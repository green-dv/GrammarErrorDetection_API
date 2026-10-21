using GrammarErrorDetection.Models.Grammar.SentenceErrors;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface ISentenceErrorRepository
{
    Task<SentenceErrorEntity> Insert(SentenceErrorInsert request);
}
