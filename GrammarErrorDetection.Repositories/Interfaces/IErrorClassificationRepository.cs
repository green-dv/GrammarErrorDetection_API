using GrammarErrorDetection.Models.Grammar.ErrorClassifications;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IErrorClassificationRepository
{
    Task<List<ErrorClassificationEntity>> FindByName(string name);
}
