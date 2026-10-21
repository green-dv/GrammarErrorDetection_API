using System.Text;
using GrammarErrorDetection.Models.Nlp;
using GrammarErrorDetection.Services.Exceptions;

namespace GrammarErrorDetection.Services.Services;

internal static class ParagraphAnalysis
{
    internal static List<NlpSentenceRequest> Split(string paragraph)
    {
        var sentences = new List<NlpSentenceRequest>();
        var start = 0;
        for (var index = 0; index < paragraph.Length; index++)
        {
            if (paragraph[index] != '.') continue;
            Add(paragraph[start..(index + 1)]);
            start = index + 1;
        }
        Add(paragraph[start..]);
        return sentences;

        void Add(string value)
        {
            var text = value.Trim();
            if (text.Length == 0 || text.All(c => c == '.')) return;
            sentences.Add(new NlpSentenceRequest { SentenceId = Guid.NewGuid().ToString(), Text = text });
        }
    }

    internal static Dictionary<string, NlpSentenceResult> Validate(IReadOnlyList<NlpSentenceRequest> request, List<NlpSentenceResult> response)
    {
        var expected = request.Select(s => s.SentenceId).ToHashSet(StringComparer.Ordinal);
        var results = new Dictionary<string, NlpSentenceResult>(StringComparer.Ordinal);
        foreach (var sentence in response)
        {
            if (sentence is null || string.IsNullOrEmpty(sentence.SentenceId) || !expected.Contains(sentence.SentenceId)
                || sentence.Errors is null || !results.TryAdd(sentence.SentenceId, sentence))
                throw new NlpApiException("La API NLP devolvió identificadores de oración desconocidos, duplicados o sin errores definidos.");
        }
        if (results.Count != request.Count)
            throw new NlpApiException("La API NLP no devolvió el resultado de todas las oraciones.");
        return results;
    }

    // Los offsets NLP son posiciones Unicode (como en Python) con fin exclusivo.
    // Se traducen a índices UTF-16 únicamente para operar con strings de .NET.
    internal static int[] Boundaries(string text)
    {
        var result = new List<int> { 0 };
        var offset = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            offset += rune.Utf16SequenceLength;
            result.Add(offset);
        }
        return result.ToArray();
    }

    internal static string? Correct(string text, IReadOnlyList<NlpError> errors, int[] boundaries)
    {
        if (errors.Count == 0 || errors.Any(e => e.SuggestedCorrection is null)) return null;
        var ordered = errors.OrderBy(e => e.StartIndex).ThenBy(e => e.EndIndex).ToList();
        for (var index = 1; index < ordered.Count; index++)
            if (ordered[index].StartIndex < ordered[index - 1].EndIndex
                || ordered[index].StartIndex == ordered[index - 1].StartIndex) return null;
        var corrected = new StringBuilder(text);
        foreach (var error in ordered.AsEnumerable().Reverse())
        {
            var start = boundaries[error.StartIndex];
            corrected.Remove(start, boundaries[error.EndIndex] - start);
            corrected.Insert(start, error.SuggestedCorrection);
        }
        return corrected.ToString();
    }
}
