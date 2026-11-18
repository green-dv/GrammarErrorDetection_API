using GrammarErrorDetection.Models.Grammar.Vocabulary;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IVocabularyService
{
    /// <summary>Verifica el vocabulario de las oraciones de un texto. Un usuario sin permiso de lectura global solo accede a sus textos.</summary>
    /// <exception cref="KeyNotFoundException">El texto no existe o no es del usuario.</exception>
    Task<VocabularyAnalysisDto> Analyze(Guid userId, bool canViewAll, Guid textId, CancellationToken cancellationToken = default);
}
