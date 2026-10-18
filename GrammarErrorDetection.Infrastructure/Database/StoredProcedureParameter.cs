using System.Data;
using NpgsqlTypes;

namespace GrammarErrorDetection.Infrastructure.Database
{
    public sealed record StoredProcedureParameter(
        string Name,
        NpgsqlDbType Type,
        object? Value = null,
        ParameterDirection Direction = ParameterDirection.Input);
}
