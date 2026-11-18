using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using GrammarErrorDetection.Models.Grammar.Vocabulary;
using GrammarErrorDetection.Services.Configuration;
using GrammarErrorDetection.Services.Exceptions;
using GrammarErrorDetection.Services.Interfaces;

namespace GrammarErrorDetection.Services.Services;

/// <summary>
/// Verifica palabras con la API gratuita Datamuse (https://www.datamuse.com/api/).
/// Una palabra es inválida si la API no tiene ninguna entrada para ella, o si todas sus entradas la definen
/// como "Misspelling of X" (p. ej. recieve → receive). No intenta detectar palabras reales usadas fuera de contexto.
/// </summary>
public sealed class DatamuseLookupClient(HttpClient client, VocabularyOptions options) : IVocabularyLookupClient
{
    private const int MaxDefinitionLength = 240;

    // Cortesía con el servicio externo: nunca más de este número de llamadas a la vez en todo el proceso.
    private static readonly SemaphoreSlim Gate = new(6);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // "v\tMisspelling of receive. [...]", "n\t(common) misspelling of ...". Se ignoran dialectos a propósito ("eye dialect").
    private static readonly Regex Misspelling = new(
        @"^[^\t]*\t(?:\([^)]*\)\s*)?(?:(?:common|frequent|nonstandard|dated|obsolete)\s+)?misspelling\s+of\s+(?<target>[\p{L}'-]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public async Task<VocabularyLookupResult> Lookup(string word, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(word);
        var normalized = word.Trim().ToLowerInvariant();
        var candidates = await Query($"words?sp={Uri.EscapeDataString(normalized)}&md=df&max=10", cancellationToken);
        var exact = candidates.FirstOrDefault(entry => Same(entry.Word, normalized));
        if (exact is not null && !IsMisspellingEntry(exact)) return new VocabularyLookupResult(true, []);

        var alternatives = new List<VocabularyAlternative>();
        if (exact is not null)
        {
            // La propia entrada dice cuál es la palabra correcta.
            var targets = (exact.Defs ?? []).Select(def => Misspelling.Match(def)).Where(match => match.Success)
                .Select(match => match.Groups["target"].Value.ToLowerInvariant()).Distinct().Take(options.MaxAlternatives);
            foreach (var target in targets)
                alternatives.Add(await ForTarget(target, candidates, cancellationToken));
        }
        else
        {
            foreach (var candidate in candidates)
            {
                if (alternatives.Count >= options.MaxAlternatives) break;
                if (IsUsable(candidate, normalized)) alternatives.Add(ToAlternative(candidate));
            }
            if (alternatives.Count == 0) await AddSuggestions(normalized, alternatives, cancellationToken);
        }
        return new VocabularyLookupResult(false, alternatives);
    }

    private async Task<VocabularyAlternative> ForTarget(string target, List<Entry> candidates, CancellationToken cancellationToken)
    {
        var entry = candidates.FirstOrDefault(candidate => Same(candidate.Word, target));
        if (entry?.Defs is not { Count: > 0 })
        {
            var found = await Query($"words?sp={Uri.EscapeDataString(target)}&md=df&max=3", cancellationToken);
            entry = found.FirstOrDefault(candidate => Same(candidate.Word, target)) ?? entry;
        }
        return new VocabularyAlternative { Word = target, Definitions = Definitions(entry?.Defs) };
    }

    // Último recurso para palabras sin ninguna entrada: el endpoint /sug completa y corrige el texto.
    private async Task AddSuggestions(string word, List<VocabularyAlternative> alternatives, CancellationToken cancellationToken)
    {
        var suggestions = await Query($"sug?s={Uri.EscapeDataString(word)}&max=5", cancellationToken);
        foreach (var suggestion in suggestions.Where(s => !Same(s.Word, word) && !s.Word.Contains(' ')).Take(3))
        {
            if (alternatives.Count >= options.MaxAlternatives) break;
            var found = await Query($"words?sp={Uri.EscapeDataString(suggestion.Word)}&md=df&max=1", cancellationToken);
            var entry = found.FirstOrDefault(candidate => Same(candidate.Word, suggestion.Word));
            if (entry is not null && IsUsable(entry, word)) alternatives.Add(ToAlternative(entry));
        }
    }

    private bool IsUsable(Entry candidate, string original)
        => !Same(candidate.Word, original) && !candidate.Word.Contains(' ') && !IsMisspellingEntry(candidate)
           && Frequency(candidate) >= options.MinAlternativeFrequency;

    private VocabularyAlternative ToAlternative(Entry entry)
        => new() { Word = entry.Word.ToLowerInvariant(), Definitions = Definitions(entry.Defs) };

    private static bool IsMisspellingEntry(Entry entry)
        => entry.Defs is { Count: > 0 } defs && defs.All(def => Misspelling.IsMatch(def));

    private static bool Same(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static double Frequency(Entry entry)
    {
        foreach (var tag in entry.Tags ?? [])
            if (tag.StartsWith("f:", StringComparison.Ordinal)
                && double.TryParse(tag.AsSpan(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return value;
        return 0;
    }

    private List<VocabularyDefinition> Definitions(IEnumerable<string>? defs)
    {
        var result = new List<VocabularyDefinition>();
        foreach (var raw in defs ?? [])
        {
            if (Misspelling.IsMatch(raw)) continue;
            var parts = raw.Split('\t', 2);
            var text = (parts.Length == 2 ? parts[1] : parts[0]).Trim();
            if (text.Length == 0) continue;
            if (text.Length > MaxDefinitionLength)
            {
                var cut = char.IsHighSurrogate(text[MaxDefinitionLength - 2]) ? MaxDefinitionLength - 2 : MaxDefinitionLength - 1;
                text = text[..cut].TrimEnd() + "…";
            }
            result.Add(new VocabularyDefinition { PartOfSpeech = parts.Length == 2 ? PartOfSpeech(parts[0]) : null, Text = text });
            if (result.Count >= options.MaxDefinitions) break;
        }
        return result;
    }

    private static string? PartOfSpeech(string code) => code switch
    {
        "n" => "noun",
        "v" => "verb",
        "adj" => "adjective",
        "adv" => "adverb",
        _ => null
    };

    private Uri BaseUri()
        => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? new Uri(uri.AbsoluteUri.TrimEnd('/') + "/")
            : throw new InvalidOperationException("Configura Vocabulary:BaseUrl con la URL HTTP/HTTPS de la API de vocabulario.");

    private async Task<List<Entry>> Query(string relativeUrl, CancellationToken cancellationToken)
    {
        var uri = new Uri(BaseUri(), relativeUrl);
        await Gate.WaitAsync(cancellationToken);
        try
        {
            using var response = await client.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new VocabularyLookupException($"La API de vocabulario respondió {(int)response.StatusCode}.", (int)response.StatusCode);
            return await response.Content.ReadFromJsonAsync<List<Entry>>(Json, cancellationToken)
                ?? throw new VocabularyLookupException("La API de vocabulario devolvió una respuesta vacía.");
        }
        catch (HttpRequestException exception)
        {
            throw new VocabularyLookupException("No se pudo conectar con la API de vocabulario.", null, exception);
        }
        catch (JsonException exception)
        {
            throw new VocabularyLookupException("La API de vocabulario devolvió un JSON incompatible.", null, exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new VocabularyLookupException("Se agotó el tiempo de espera de la API de vocabulario.", null, exception);
        }
        finally { Gate.Release(); }
    }

    private sealed class Entry
    {
        public string Word { get; set; } = string.Empty;
        public List<string>? Tags { get; set; }
        public List<string>? Defs { get; set; }
    }
}
