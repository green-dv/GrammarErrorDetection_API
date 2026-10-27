using System.ComponentModel.DataAnnotations;
using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Models.Grammar.Users;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using NpgsqlTypes;

namespace GrammarErrorDetection.Repositories.Repositories;

public sealed class UserLeaderboardRepository(IStoredProcedureExecutor executor) : IUserLeaderboardRepository
{
    public async Task<List<UserLeaderboardItemDto>> List(Guid userId, UserLeaderboardQuery request)
    {
        if (userId == Guid.Empty)
            throw new ValidationException("UserId debe identificar al usuario actual.");
        RepositoryValidation.ValidateInput(request);
        var response = await executor.ExecuteStoredProcedureAsync("grammar", "User_Leaderboard",
            [
                new("UserId", NpgsqlDbType.Uuid, userId),
                new("Bimester", NpgsqlDbType.Integer, request.Bimester),
                new("Year", NpgsqlDbType.Integer, request.Year),
                new("ModuleNameId", NpgsqlDbType.Uuid, request.ModuleNameId),
                new("Results", NpgsqlDbType.Refcursor, "user_leaderboard", ParameterDirection.InputOutput)
            ], StoredProcedureResult.Cursor("Results"));
        using var table = response.GetDataOrThrow() ?? throw new DataException("El ejecutor no devolvió una tabla.");
        return table.AsEnumerable().Select(row => new UserLeaderboardItemDto
        {
            Position = row.Field<long?>("Position"),
            UserId = row.Field<Guid>("UserId"),
            FirstName = row.Field<string>("FirstName") ?? string.Empty,
            LastName = row.Field<string>("LastName") ?? string.Empty,
            UserName = row.Field<string>("UserName") ?? string.Empty,
            ModuleNameId = row.Field<Guid?>("ModuleNameId"),
            ModuleName = row.Field<string>("ModuleName"),
            TotalPoints = row.Field<long>("TotalPoints"),
            IsCurrentUser = row.Field<bool>("IsCurrentUser"),
            IsTop10 = row.Field<bool>("IsTop10")
        }).ToList();
    }
}
