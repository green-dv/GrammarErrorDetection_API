using GrammarErrorDetection.Models.Grammar.Vocabulary;

namespace GrammarErrorDetection.Services.Interfaces;

public interface IVocabularyLookupClient
{
    /// <summary>Verifica una palabra en la API externa y, si no es válida, devuelve alternativas con definiciones.</summary>
    /// <exception cref="GrammarErrorDetection.Services.Exceptions.VocabularyLookupException">La API externa no pudo responder.</exception>
    Task<VocabularyLookupResult> Lookup(string word, CancellationToken cancellationToken = default);
}
