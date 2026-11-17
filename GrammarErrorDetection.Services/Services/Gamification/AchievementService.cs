using GrammarErrorDetection.Models.Grammar.Dashboards;
using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Models.Grammar.PointTransactions;
using GrammarErrorDetection.Models.Grammar.UserLevels;
using GrammarErrorDetection.Models.Grammar.Users;

namespace GrammarErrorDetection.Services.Services;

public sealed class AchievementService(IAchievementRepository repository, IAchievementFileService files) : IAchievementService
{
    public Task<List<AchievementListItem>> List(PaginationDto<AchievementList> request) => repository.List(request);

    public Task<PagedList<AchievementListItem>> ListPage(PaginationDto<AchievementList> request) => repository.ListPage(request);
    public Task<AchievementEntity> Insert(AchievementInsert request, Guid createdByUserId) => repository.Insert(request, createdByUserId);
    public Task<AchievementEntity> Update(AchievementUpdate request) => repository.Update(request);

    public async Task<AchievementEntity> InsertWithImage(AchievementInsert request, Guid createdByUserId, Stream? image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(request, new ValidationContext(request), true);
        if (createdByUserId == Guid.Empty) throw new UnauthorizedAccessException();
        cancellationToken.ThrowIfCancellationRequested();
        if (image is not null)
            request.IconLink = (await files.Upload(image, cancellationToken)).IconLink;
        cancellationToken.ThrowIfCancellationRequested();
        return await repository.Insert(request, createdByUserId);
    }

    public async Task<AchievementEntity> UpdateWithImage(AchievementUpdate request, Stream? image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(request, new ValidationContext(request), true);
        cancellationToken.ThrowIfCancellationRequested();
        if (image is not null)
        {
            var existing = await repository.List(new GamificationPage<AchievementList>
            {
                Filter = new() { Id = request.Id }, PageSize = 1
            });
            if (existing.Count == 0) throw new KeyNotFoundException("No se encontró el logro solicitado.");
            request.IconLink = (await files.Upload(image, cancellationToken)).IconLink;
        }
        cancellationToken.ThrowIfCancellationRequested();
        return await repository.Update(request);
    }
}

public sealed class AchievementCategoryService(IAchievementCategoryRepository repository) : IAchievementCategoryService
{
    public Task<List<AchievementCategoryEntity>> List(PaginationDto<AchievementCategoryList> request) => repository.List(request);

    public Task<PagedList<AchievementCategoryEntity>> ListPage(PaginationDto<AchievementCategoryList> request) => repository.ListPage(request);
    public Task<AchievementCategoryEntity> Insert(AchievementCategoryInsert request) => repository.Insert(request);
    public Task<AchievementCategoryEntity> Update(AchievementCategoryUpdate request) => repository.Update(request);
}

public sealed class AchievementCollectionService(IAchievementCollectionRepository repository) : IAchievementCollectionService
{
    public Task<List<AchievementCollectionEntity>> List(PaginationDto<AchievementCollectionList> request) => repository.List(request);

    public Task<PagedList<AchievementCollectionEntity>> ListPage(PaginationDto<AchievementCollectionList> request) => repository.ListPage(request);
    public Task<AchievementCollectionEntity> Insert(AchievementCollectionInsert request, Guid createdByUserId) => repository.Insert(request, createdByUserId);
    public Task<AchievementCollectionEntity> Update(AchievementCollectionUpdate request) => repository.Update(request);
}

public sealed class ChallengeService(IChallengeRepository repository) : IChallengeService
{
    public Task<List<ChallengeListItem>> List(PaginationDto<ChallengeList> request) => repository.List(request);

    public Task<PagedList<ChallengeListItem>> ListPage(PaginationDto<ChallengeList> request) => repository.ListPage(request);
    public Task<ChallengeEntity> Insert(ChallengeInsert request, Guid createdByUserId) => repository.Insert(request, createdByUserId);
    public Task<ChallengeEntity> Update(ChallengeUpdate request) => repository.Update(request);
    public Task<ChallengeEntity> Repeat(Guid id, ChallengeRepeat request) => repository.Repeat(id, request);
    public Task<ChallengeEntity> Duplicate(Guid id, Guid createdByUserId) => repository.Duplicate(id, createdByUserId);
}

public sealed class ConditionTypeService(IConditionTypeRepository repository) : IConditionTypeService
{
    public Task<PagedList<ConditionTypeEntity>> List(PaginationDto<ConditionTypeList> request) => repository.List(request);
    public Task<ConditionTypeEntity> Insert(ConditionTypeInsert request) => repository.Insert(request);
    public Task<ConditionTypeEntity> Update(ConditionTypeUpdate request) => repository.Update(request);
}

public sealed class UserAchievementService(IUserAchievementRepository repository) : IUserAchievementService
{
    public Task<List<UserAchievementBestItem>> ListBest(Guid userId, PaginationDto<UserAchievementBestList> request)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId es obligatorio y debe ser un UUID distinto de vacío.", nameof(userId));
        return repository.ListBest(userId, request);
    }

    public Task<PagedList<UserAchievementBestItem>> ListBestPage(Guid userId, PaginationDto<UserAchievementBestList> request)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId es obligatorio y debe ser un UUID distinto de vacío.", nameof(userId));
        return repository.ListBestPage(userId, request);
    }
}

public sealed class PointTransactionService(IPointTransactionRepository repository) : IPointTransactionService
{
    public Task<List<PointTransactionEntity>> List(PointTransactionListQuery request) => repository.List(request);
    public Task<PagedList<PointTransactionEntity>> ListPage(PointTransactionListQuery request) => repository.ListPage(request);
    public Task<PointTransactionEntity> Insert(PointTransactionInsert request) => repository.Insert(request);
    public Task<PointTransactionEntity> Update(PointTransactionUpdate request) => repository.Update(request);
    public Task<PointTransactionTotalDto> Total(PointTransactionTotalQuery request) => repository.Total(request);
}

public sealed class UserLevelService(IUserLevelRepository repository) : IUserLevelService
{
    public Task<PagedList<UserLevelListItemDto>> List(PaginationDto<UserLevelList> request) => repository.List(request);
    public Task<UserLevelListItemDto> Insert(UserLevelInsert request) => repository.Insert(request);
    public Task<UserLevelListItemDto> Update(UserLevelUpdate request) => repository.Update(request);
}

public sealed class UserLeaderboardService(IUserLeaderboardRepository repository) : IUserLeaderboardService
{
    public Task<List<UserLeaderboardItemDto>> List(Guid userId, UserLeaderboardQuery request)
        => repository.List(userId, request);
}
