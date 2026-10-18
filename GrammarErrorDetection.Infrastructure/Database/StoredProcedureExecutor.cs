using System.Data;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
using Npgsql;
using NpgsqlTypes;

namespace GrammarErrorDetection.Infrastructure.Database
{
    public sealed class StoredProcedureExecutor : IStoredProcedureExecutor
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
                await using var call = new NpgsqlCommand { Connection = connection, Transaction = transaction };
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


