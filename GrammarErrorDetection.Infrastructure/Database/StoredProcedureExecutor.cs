using System.Data;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
using Npgsql;
using NpgsqlTypes;

namespace GrammarErrorDetection.Infrastructure.Database
{
    public sealed class StoredProcedureExecutor : IStoredProcedureExecutor, IStoredProcedurePageExecutor, IStoredProcedureMultiResultExecutor
    {
        private readonly NpgsqlDataSource dataSource;

        public StoredProcedureExecutor(NpgsqlDataSource dataSource)
        {
            ArgumentNullException.ThrowIfNull(dataSource);
            this.dataSource = dataSource;
        }

        public async Task<ResourceResponse<DataTable>> ExecuteStoredProcedureAsync(string schema, string procedureName,IReadOnlyList<StoredProcedureParameter> parameters, StoredProcedureResult? result = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(schema);
            ArgumentException.ThrowIfNullOrWhiteSpace(procedureName);
            ArgumentNullException.ThrowIfNull(parameters);
            result ??= StoredProcedureResult.Rows;
            ValidateParameters(parameters);

            if (result.Kind != StoredProcedureResultKind.Rows &&
                !parameters.Any(p => p.Name == result.OutputParameter && p.Direction != ParameterDirection.Input))
            {
                throw new ArgumentException("El resultado debe identificar un parámetro OUT o INOUT.", nameof(result));
            }

            DataTable? table = null;
            try
            {
                await using var connection = await dataSource.OpenConnectionAsync();
                await using var transaction = await connection.BeginTransactionAsync();
                await using var call = BuildCall(connection, transaction, schema, procedureName, parameters);
                call.AllResultTypesAreUnknown = result.Kind == StoredProcedureResultKind.Composite;

                if (result.Kind == StoredProcedureResultKind.Rows)
                {
                    await using var reader = await call.ExecuteReaderAsync();
                    table = await ReadTable(reader);
                }
                else
                {
                    string? output;
                    await using (var reader = await call.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                            throw new DataException("El procedimiento no devolvió una fila de salida.");

                        var ordinal = reader.GetOrdinal(result.OutputParameter!);
                        output = reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
                    }

                    if (result.Kind == StoredProcedureResultKind.Cursor)
                    {
                        if (string.IsNullOrWhiteSpace(output))
                            throw new DataException("El procedimiento no devolvió un cursor válido.");

                        await using var fetch = new NpgsqlCommand($"FETCH ALL FROM {QuoteIdentifier(output)};", connection, transaction);
                        await using var reader = await fetch.ExecuteReaderAsync();
                        table = await ReadTable(reader);
                    }
                    else
                    {
                        if (output is null)
                            throw new KeyNotFoundException("El procedimiento no devolvió una entidad.");

                        var type = $"{QuoteIdentifier(result.Schema!)}.{QuoteIdentifier(result.TypeName!)}";
                        await using var expand = new NpgsqlCommand(
                            $"SELECT ($1::{type}).* WHERE NOT ($1::{type} IS NULL);", connection, transaction);
                        expand.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, Value = output });
                        await using var reader = await expand.ExecuteReaderAsync();
                        table = await ReadTable(reader);
                        if (table.Rows.Count == 0)
                            throw new KeyNotFoundException("El procedimiento no devolvió una entidad.");
                    }
                }

                await transaction.CommitAsync();
                return ResourceResponse<DataTable>.Ok(table);
            }
            catch (Exception exception) when (
                exception is NpgsqlException or TimeoutException or DataException or KeyNotFoundException or OperationCanceledException)
            {
                table?.Dispose();
                var code = exception switch
                {
                    PostgresException postgres => postgres.SqlState,
                    KeyNotFoundException => "NOT_FOUND",
                    DataException => "INVALID_DATABASE_RESULT",
                    TimeoutException or OperationCanceledException => "DATABASE_TIMEOUT",
                    _ => "DATABASE_ERROR"
                };
                return ResourceResponse<DataTable>.Failure(code, exception.Message, exception);
            }
        }

        /// <summary>
        /// SP de listado con dos cursores INOUT: la página filtrada y el total de registros que cumplen los filtros.
        /// Ambos cursores se leen en la misma conexión y transacción en que se abrieron.
        /// </summary>
        public async Task<ResourceResponse<StoredProcedurePage>> ExecutePagedStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, string rowsCursorParameter = "Results", string totalCursorParameter = "Total")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rowsCursorParameter);
            ArgumentException.ThrowIfNullOrWhiteSpace(totalCursorParameter);
            if (rowsCursorParameter == totalCursorParameter)
                throw new ArgumentException("Los cursores de datos y total deben ser distintos.", nameof(totalCursorParameter));

            var response = await ExecuteMultiResultStoredProcedureAsync(schema, procedureName, parameters,
                [rowsCursorParameter, totalCursorParameter]);
            if (response.Error is not null)
                return ResourceResponse<StoredProcedurePage>.Failure(response.Error.Code, response.Error.Message, response.Exception);

            var tables = response.Data!;
            var page = new StoredProcedurePage(tables[rowsCursorParameter], tables[totalCursorParameter]);
            try
            {
                _ = page.TotalCount; // Valida el formato del segundo DataTable.
                return ResourceResponse<StoredProcedurePage>.Ok(page);
            }
            catch (DataException exception)
            {
                page.Dispose();
                return ResourceResponse<StoredProcedurePage>.Failure("INVALID_DATABASE_RESULT", exception.Message, exception);
            }
        }

        /// <summary>
        /// SP con varios cursores INOUT con nombre. Todos se leen en la misma conexión y transacción en que se abrieron.
        /// </summary>
        public async Task<ResourceResponse<StoredProcedureResultSet>> ExecuteMultiResultStoredProcedureAsync(string schema,
            string procedureName, IReadOnlyList<StoredProcedureParameter> parameters, IReadOnlyList<string> cursorParameters)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(schema);
            ArgumentException.ThrowIfNullOrWhiteSpace(procedureName);
            ArgumentNullException.ThrowIfNull(parameters);
            ArgumentNullException.ThrowIfNull(cursorParameters);
            ValidateParameters(parameters);
            if (cursorParameters.Count == 0)
                throw new ArgumentException("Indica al menos un cursor.", nameof(cursorParameters));
            if (cursorParameters.Distinct(StringComparer.Ordinal).Count() != cursorParameters.Count)
                throw new ArgumentException("Los cursores deben ser distintos.", nameof(cursorParameters));
            foreach (var cursor in cursorParameters)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(cursor);
                if (!parameters.Any(p => p.Name == cursor && p.Direction != ParameterDirection.Input && p.Type == NpgsqlDbType.Refcursor))
                    throw new ArgumentException($"El cursor {cursor} debe ser un parámetro REFCURSOR de salida.", nameof(parameters));
            }

            var tables = new Dictionary<string, DataTable>(StringComparer.Ordinal);
            try
            {
                await using var connection = await dataSource.OpenConnectionAsync();
                await using var transaction = await connection.BeginTransactionAsync();
                await using var call = BuildCall(connection, transaction, schema, procedureName, parameters);

                var names = new List<(string Parameter, string Cursor)>();
                await using (var reader = await call.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                        throw new DataException("El procedimiento no devolvió una fila de salida.");
                    foreach (var parameter in cursorParameters)
                        names.Add((parameter, ReadCursorName(reader, parameter)));
                }

                foreach (var (parameter, cursor) in names)
                    tables[parameter] = await FetchCursor(connection, transaction, cursor);
                await transaction.CommitAsync();
                return ResourceResponse<StoredProcedureResultSet>.Ok(new StoredProcedureResultSet(tables));
            }
            catch (Exception exception) when (
                exception is NpgsqlException or TimeoutException or DataException or OperationCanceledException)
            {
                foreach (var table in tables.Values) table.Dispose();
                var code = exception switch
                {
                    PostgresException postgres => postgres.SqlState,
                    DataException => "INVALID_DATABASE_RESULT",
                    TimeoutException or OperationCanceledException => "DATABASE_TIMEOUT",
                    _ => "DATABASE_ERROR"
                };
                return ResourceResponse<StoredProcedureResultSet>.Failure(code, exception.Message, exception);
            }
        }

        private static void ValidateParameters(IReadOnlyList<StoredProcedureParameter> parameters)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var parameter in parameters)
            {
                ArgumentNullException.ThrowIfNull(parameter);
                ArgumentException.ThrowIfNullOrWhiteSpace(parameter.Name);
                if (!names.Add(parameter.Name))
                    throw new ArgumentException("Los nombres de parámetros deben ser únicos.", nameof(parameters));
                if (parameter.Direction is not (ParameterDirection.Input or ParameterDirection.Output or ParameterDirection.InputOutput))
                    throw new ArgumentException("Dirección de parámetro no admitida.", nameof(parameters));
            }
        }

        // CALL con argumentos con nombre; los parámetros OUT puros se pasan como NULL.
        private static NpgsqlCommand BuildCall(NpgsqlConnection connection, NpgsqlTransaction transaction, string schema,
            string procedureName, IReadOnlyList<StoredProcedureParameter> parameters)
        {
            var call = new NpgsqlCommand { Connection = connection, Transaction = transaction };
            var arguments = new List<string>();
            foreach (var parameter in parameters)
            {
                if (parameter.Direction == ParameterDirection.Output)
                {
                    arguments.Add($"{QuoteIdentifier(parameter.Name)} => NULL");
                    continue;
                }

                call.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = parameter.Type, Value = parameter.Value ?? DBNull.Value });
                arguments.Add($"{QuoteIdentifier(parameter.Name)} => ${call.Parameters.Count}");
            }

            call.CommandText = $"CALL {QuoteIdentifier(schema)}.{QuoteIdentifier(procedureName)}({string.Join(", ", arguments)});";
            return call;
        }

        private static string ReadCursorName(NpgsqlDataReader reader, string parameter)
        {
            var ordinal = reader.GetOrdinal(parameter);
            var name = reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            return string.IsNullOrWhiteSpace(name) ? throw new DataException($"El procedimiento no devolvió el cursor {parameter}.") : name;
        }

        private static async Task<DataTable> FetchCursor(NpgsqlConnection connection, NpgsqlTransaction transaction, string cursor)
        {
            await using var fetch = new NpgsqlCommand($"FETCH ALL FROM {QuoteIdentifier(cursor)};", connection, transaction);
            await using var reader = await fetch.ExecuteReaderAsync();
            return await ReadTable(reader);
        }

        private static string QuoteIdentifier(string identifier)
            => "\"" + identifier.Replace("\"", "\"\"") + "\"";

        private static async Task<DataTable> ReadTable(NpgsqlDataReader reader)
        {
            var table = new DataTable();
            for (var index = 0; index < reader.FieldCount; index++)
                table.Columns.Add(reader.GetName(index), reader.GetFieldType(index));

            while (await reader.ReadAsync())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                table.Rows.Add(values);
            }

            return table;
        }
    }
}


