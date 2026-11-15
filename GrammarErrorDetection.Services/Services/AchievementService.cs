using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Gamification;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

public sealed class AchievementService(IAchievementRepository repository, IAchievementFileService files) : IAchievementService
{
    public Task<List<AchievementListItem>> List(PaginationDto<AchievementList> request) => repository.List(request);
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
