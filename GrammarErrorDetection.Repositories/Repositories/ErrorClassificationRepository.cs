using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.Grammar.ErrorClassifications;
using GrammarErrorDetection.Repositories.Interfaces;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class ErrorClassificationRepository(IStoredProcedureExecutor executor) : IErrorClassificationRepository
{
    public async Task<List<ErrorClassificationEntity>> FindByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var matches = new List<ErrorClassificationEntity>();
        const int pageSize = 200;
        for (var page = 1; ; page++)
        {
            var response = await executor.ExecuteStoredProcedureAsync("grammar", "ErrorClassification_List",
                [
                    new("Id", NpgsqlDbType.Uuid, null),
                    new("Name", NpgsqlDbType.Varchar, name.Trim()),
                    new("Description", NpgsqlDbType.Text, null),
                    new("Active", NpgsqlDbType.Boolean, true),
                    new("CreatedAt", NpgsqlDbType.Date, null),
                    new("PageNumber", NpgsqlDbType.Integer, page),
                    new("PageSize", NpgsqlDbType.Integer, pageSize),
                    new("Results", NpgsqlDbType.Refcursor, "errorclassification_list", ParameterDirection.InputOutput)
                ], StoredProcedureResult.Cursor("Results"));
            using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
            foreach (var row in table.AsEnumerable())
            {
                var classification = new ErrorClassificationEntity
                {
                    Id = row.Field<Guid>("Id"), Name = row.Field<string>("Name") ?? string.Empty,
                    Active = row.Field<bool>("Active")
                };
                if (classification.Active && string.Equals(classification.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                    matches.Add(classification);
            }
            if (table.Rows.Count < pageSize) return matches;
        }
    }
}
