using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Texts;
using GrammarErrorDetection.Models.Grammar.Vocabulary;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Repositories;
using GrammarErrorDetection.Services.Configuration;
using GrammarErrorDetection.Services.Exceptions;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GrammarErrorDetection.Services.Services;

public sealed class VocabularyService(ITextRepository texts, IVocabularyWordRepository words, IVocabularyLookupClient lookup,
    VocabularyOptions options, ILogger<VocabularyService> logger) : IVocabularyService
{
    // Fallos aislados (tiempos de espera, respuestas raras) que se toleran antes de dejar de consultar.
    private const int MaxIsolatedFailures = 5;

    public async Task<VocabularyAnalysisDto> Analyze(Guid userId, bool canViewAll, Guid textId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ValidationException("El identificador de usuario no puede estar vacío.");
        if (textId == Guid.Empty) throw new ValidationException("El identificador del texto no puede estar vacío.");

        var matches = await texts.List(new PaginationDto<TextList>
        {
            PageNumber = 1, PageSize = 1,
            Filter = new TextList { Id = textId, UserId = canViewAll ? null : userId, Active = true }
        });
        var text = matches.FirstOrDefault(item => item.Id == textId) ?? throw new KeyNotFoundException("No se encontró el texto.");

        var perSentence = text.Sentences.Where(sentence => sentence.Active != false).OrderBy(sentence => sentence.SentenceIndex)
            .Select(sentence => (Sentence: sentence, Tokens: VocabularyTokenizer.Extract(sentence.OriginalSentence))).ToList();
        var distinct = perSentence.SelectMany(item => item.Tokens).Select(token => token.Word).Distinct(StringComparer.Ordinal).ToList();

        var known = new Dictionary<string, VocabularyLookupResult>(StringComparer.Ordinal);
        foreach (var chunk in distinct.Chunk(VocabularyWordRepository.MaxWordsPerQuery))
            foreach (var cached in await words.GetMany(chunk, options.CacheDays))
                known[cached.Word] = new VocabularyLookupResult(cached.IsValid, cached.Suggestions);

        var missing = distinct.Where(word => !known.ContainsKey(word)).Take(options.MaxNewLookupsPerRequest).ToList();
        if (missing.Count > 0)
        {
            var fetched = await Verify(missing, cancellationToken);
            foreach (var (word, result) in fetched)
            {
                known[word] = result;
                try
                {
                    await words.Upsert(new VocabularyWordUpsert
                    {
                        Word = word, IsValid = result.IsValid, Suggestions = result.IsValid ? [] : result.Alternatives.ToList()
                    });
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // El resultado sirve para esta respuesta aunque no se haya podido guardar en la caché.
                    logger.LogWarning(exception, "No se pudo guardar una palabra verificada en la caché de vocabulario.");
                }
            }
        }

        var analysis = new VocabularyAnalysisDto { TextId = text.Id };
        foreach (var (sentence, tokens) in perSentence)
        {
            var item = new VocabularySentenceDto
            {
                SentenceId = sentence.Id, SentenceIndex = sentence.SentenceIndex, OriginalSentence = sentence.OriginalSentence
            };
            foreach (var token in tokens)
            {
                if (!known.TryGetValue(token.Word, out var result)) item.UncheckedCount++;
                else if (!result.IsValid)
                    item.Issues.Add(new VocabularyIssue
                    {
                        Token = token.Token, StartIndex = token.StartIndex, EndIndex = token.EndIndex,
                        Alternatives = result.Alternatives.ToList()
                    });
            }
            analysis.Sentences.Add(item);
        }
        analysis.UncheckedCount = distinct.Count(word => !known.ContainsKey(word));
        logger.LogInformation("Vocabulario del texto {TextId}: {Words} palabras, {Fetched} consultadas, {Unchecked} sin verificar",
            text.Id, distinct.Count, missing.Count, analysis.UncheckedCount);
        return analysis;
    }

    // Consulta las palabras con concurrencia limitada. Ante un fallo sistémico (cuota, clave) deja de consultar;
    // las palabras que no se verificaron no se guardan, así la siguiente consulta las reintenta.
    private async Task<IReadOnlyDictionary<string, VocabularyLookupResult>> Verify(IReadOnlyList<string> pending, CancellationToken cancellationToken)
    {
        var results = new ConcurrentDictionary<string, VocabularyLookupResult>(StringComparer.Ordinal);
        var stopped = 0;
        var failures = 0;
        await Parallel.ForEachAsync(pending,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, options.MaxConcurrency), CancellationToken = cancellationToken },
            async (word, token) =>
            {
                if (Volatile.Read(ref stopped) == 1) return;
                try { results[word] = await lookup.Lookup(word, token); }
                catch (VocabularyLookupException exception)
                {
                    var count = Interlocked.Increment(ref failures);
                    if (count == 1)
                        logger.LogWarning(exception, "La API de vocabulario no pudo verificar una palabra (estado {Status}).", exception.StatusCode);
                    if (exception.IsSystemic || count >= MaxIsolatedFailures) Interlocked.Exchange(ref stopped, 1);
                }
            });
        return results;
    }
}

public sealed record VocabularyToken(string Word, string Token, int StartIndex, int EndIndex);

/// <summary>Extrae de una oración las palabras que conviene verificar, con su posición.</summary>
public static class VocabularyTokenizer
{
    public const int MaxWordLength = 45;

    private static readonly Regex WordPattern = new(@"(?<![\p{L}\p{N}_])\p{L}+(?:['’]\p{L}+)*(?![\p{L}\p{N}_])",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Se omiten las palabras que no se pueden juzgar sin contexto: contracciones y posesivos, una sola letra,
    /// siglas, marcas (iPhone) y nombres propios (con mayúscula fuera de inicio de oración). El resto se normaliza a minúsculas.
    /// </summary>
    public static List<VocabularyToken> Extract(string sentence)
    {
        var result = new List<VocabularyToken>();
        if (string.IsNullOrWhiteSpace(sentence)) return result;
        foreach (Match match in WordPattern.Matches(sentence))
        {
            var token = match.Value;
            if (token.Contains('\'') || token.Contains('’')) continue;
            if (token.Length < 2 || token.Length > MaxWordLength) continue;
            if (token.All(char.IsUpper) || token.Skip(1).Any(char.IsUpper)) continue;
            if (char.IsUpper(token[0]) && !StartsSentence(sentence, match.Index)) continue;
            result.Add(new VocabularyToken(token.ToLowerInvariant(), token, match.Index, match.Index + match.Length));
        }
        return result;
    }

    // Inicio de la oración, o tras . ! ? (ignorando espacios y comillas o paréntesis de apertura).
    private static bool StartsSentence(string sentence, int index)
    {
        for (var position = index - 1; position >= 0; position--)
        {
            var previous = sentence[position];
            if (char.IsWhiteSpace(previous) || previous is '"' or '“' or '‘' or '(' or '¿' or '¡') continue;
            return previous is '.' or '!' or '?';
        }
        return true;
    }
}
