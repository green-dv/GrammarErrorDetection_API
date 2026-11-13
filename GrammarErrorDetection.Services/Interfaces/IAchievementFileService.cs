using GrammarErrorDetection.Models.Grammar.Gamification;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IAchievementFileService
{
    Task<AchievementIconResult> Upload(Stream content, CancellationToken cancellationToken = default);
}
