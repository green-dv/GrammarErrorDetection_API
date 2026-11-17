using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Responses;

namespace GrammarErrorDetection.Infrastructure.Interfaces
{
    public interface IStoredProcedureExecutor
    {
        Task<ResourceResponse<DataTable>> ExecuteStoredProcedureAsync(
            string schema,
            string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters,
            StoredProcedureResult? result = null);
    }
}

