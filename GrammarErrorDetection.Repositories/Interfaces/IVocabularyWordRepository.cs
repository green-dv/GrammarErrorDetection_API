using GrammarErrorDetection.Models.Grammar.Vocabulary;

namespace GrammarErrorDetection.Repositories.Interfaces;

public interface IVocabularyWordRepository
{
    /// <summary>Palabras ya verificadas y vigentes (consultadas hace menos de <paramref name="maxAgeDays"/> días). Las ausentes no aparecen.</summary>
    Task<List<VocabularyWordEntity>> GetMany(IReadOnlyCollection<string> words, int maxAgeDays = 180);

    /// <summary>Guarda o renueva el resultado de verificar una palabra.</summary>
    Task<VocabularyWordEntity> Upsert(VocabularyWordUpsert request);
}
