using GrammarErrorDetection.Models.Grammar.Sentences;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface ISentenceRepository
{
    Task<SentenceEntity> Insert(SentenceInsert request);
}
