namespace GrammarErrorDetection.Models.Grammar.Vocabulary;

/// <summary>Una definición de una palabra (la API externa la entrega en inglés).</summary>
public sealed class VocabularyDefinition
{
    /// <summary>noun, verb, adjective o adverb; null si la fuente no lo indica.</summary>
    public string? PartOfSpeech { get; set; }

    public string Text { get; set; } = string.Empty;
}

/// <summary>Palabra sugerida en lugar de la que tiene el error, con sus definiciones.</summary>
public sealed class VocabularyAlternative
{
    public string Word { get; set; } = string.Empty;
    public List<VocabularyDefinition> Definitions { get; set; } = [];
}

/// <summary>Palabra de la oración que no es válida. Los índices son posiciones UTF-16 de la oración; el final es exclusivo.</summary>
public sealed class VocabularyIssue
{
    public string Token { get; set; } = string.Empty;
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }

    /// <summary>Vacía cuando no hay ninguna palabra parecida que sugerir.</summary>
    public List<VocabularyAlternative> Alternatives { get; set; } = [];
}

public sealed class VocabularySentenceDto
{
    public Guid SentenceId { get; set; }
    public int SentenceIndex { get; set; }
    public string OriginalSentence { get; set; } = string.Empty;
    public List<VocabularyIssue> Issues { get; set; } = [];

    /// <summary>Palabras de la oración que no se pudieron verificar (servicio externo no disponible o con límite alcanzado).</summary>
    public int UncheckedCount { get; set; }
}

public sealed class VocabularyAnalysisDto
{
    public Guid TextId { get; set; }
    public List<VocabularySentenceDto> Sentences { get; set; } = [];
    public int IssueCount => Sentences.Sum(sentence => sentence.Issues.Count);

    /// <summary>Palabras distintas sin verificar. Si es mayor que cero, volver a consultar completa la verificación.</summary>
    public int UncheckedCount { get; set; }

    /// <summary>Origen de las alternativas y definiciones (se muestra al usuario).</summary>
    public string Source => "Datamuse";
}

/// <summary>Resultado de verificar una palabra en la API externa.</summary>
public sealed record VocabularyLookupResult(bool IsValid, IReadOnlyList<VocabularyAlternative> Alternatives);
