using GrammarErrorDetection.Models.Grammar.Dashboards;
using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.SentenceErrors;
using GrammarErrorDetection.Models.Grammar.Sentences;
using GrammarErrorDetection.Models.Grammar.Texts;
using GrammarErrorDetection.Models.Nlp;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Exceptions;
using GrammarErrorDetection.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using GrammarErrorDetection.Models.Grammar.ErrorClassifications;

namespace GrammarErrorDetection.Services.Services;

public sealed class TextService(ITextRepository texts, ISentenceRepository sentences,
    ISentenceErrorRepository errors, IErrorClassificationRepository classifications,
    INlpService nlp, ILogger<TextService> logger, IRegistrationRepository registrations) : ITextService
{
    public Task<List<TextListItemDto>> List(Guid userId, bool canViewAll, PaginationDto<TextList> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (userId == Guid.Empty) throw new ValidationException("El identificador de usuario no puede estar vacío.");
        request.Filter ??= new TextList();
        if (!canViewAll) request.Filter.UserId = userId;
        return texts.List(request);
    }

    public Task<PagedList<TextListItemDto>> ListPage(Guid userId, bool canViewAll, PaginationDto<TextList> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (userId == Guid.Empty) throw new ValidationException("El identificador de usuario no puede estar vacío.");
        request.Filter ??= new TextList();
        if (!canViewAll) request.Filter.UserId = userId;
        return texts.ListPage(request);
    }

    public async Task<TextListItemDto> Upload(Guid userId, TextUploadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (userId == Guid.Empty) throw new ValidationException("El identificador de usuario no puede estar vacío.");
        Validator.ValidateObject(request, new ValidationContext(request), true);
        if (request.RegistrationId.HasValue)
        {
            var matches = await registrations.List(new PaginationDto<GrammarErrorDetection.Models.Grammar.Registrations.RegistrationList>
            {
                PageNumber = 1, PageSize = 2,
                Filter = new() { Id = request.RegistrationId, UserId = userId }
            });
            if (!matches.Any(r => r.Id == request.RegistrationId && r.UserId == userId && r.ClassId.HasValue
                && string.Equals(r.Status, "Approved", StringComparison.OrdinalIgnoreCase)))
                throw new ValidationException("RegistrationId debe ser una inscripción aprobada del usuario en un curso.");
        }
        var batch = ParagraphAnalysis.Split(request.OriginalContent);
        if (batch.Count == 0) throw new ValidationException("El párrafo debe contener al menos una oración.");
        var response = await nlp.Analyze(batch, cancellationToken);
        var byId = ParagraphAnalysis.Validate(batch, response);
        var plans = await Prepare(batch, byId);
        cancellationToken.ThrowIfCancellationRequested();

        // La validación NLP y de categorías termina antes de la primera escritura.
        var text = await texts.Insert(new TextInsert
        {
            UserId = userId, RegistrationId = request.RegistrationId, OriginalContent = request.OriginalContent,
            SubmissionDate = DateTime.UtcNow, Status = "Procesando", Active = true
        });
        try
        {
            var detail = new List<SentenceErrorDto>();
            for (var index = 0; index < plans.Count; index++)
            {
                var plan = plans[index];
                var sentence = await sentences.Insert(new SentenceInsert
                {
                    TextId = text.Id, SentenceIndex = index + 1, OriginalSentence = plan.Text,
                    CorrectedSentence = plan.CorrectedText, Active = true
                });
                var item = new SentenceErrorDto
                {
                    Id = sentence.Id, TextId = sentence.TextId, SentenceIndex = sentence.SentenceIndex,
                    OriginalSentence = sentence.OriginalSentence, CorrectedSentence = sentence.CorrectedSentence,
                    CreatedAt = sentence.CreatedAt, UpdatedAt = sentence.UpdatedAt, Active = sentence.Active
                };
                foreach (var plannedError in plan.Errors)
                {
                    plannedError.Insert.SentenceId = sentence.Id;
                    var saved = await errors.Insert(plannedError.Insert);
                    item.Errors.Add(new SentenceErrorDetailDto
                    {
                        Id = saved.Id, SentenceId = saved.SentenceId, ErrorClassificationId = saved.ErrorClassificationId,
                        ErrorClassificationName = plannedError.Name, ErrorToken = saved.ErrorToken,
                        ErrorTokenStartIndex = saved.ErrorTokenStartIndex, ErrorTokenEndIndex = saved.ErrorTokenEndIndex,
                        SuggestedCorrection = saved.SuggestedCorrection, ConfidenceScore = saved.ConfidenceScore,
                        CreatedAt = saved.CreatedAt, UpdatedAt = saved.UpdatedAt, Active = saved.Active
                    });
                }
                detail.Add(item);
            }
            text = await texts.CompleteAnalysis(text.Id, request.RegistrationId);
            logger.LogInformation("Texto analizado {TextId}: {Sentences} oraciones, {Errors} errores",
                text.Id, detail.Count, detail.Sum(sentence => sentence.Errors.Count));
            return new TextListItemDto
            {
                Id = text.Id, UserId = text.UserId, RegistrationId = text.RegistrationId, SubmissionDate = text.SubmissionDate,
                OriginalContent = text.OriginalContent, Status = text.Status, CreatedAt = text.CreatedAt,
                UpdatedAt = text.UpdatedAt, Active = text.Active, Sentences = detail
            };
        }
        catch
        {
            // Los SPs confirman por separado: se hace visible un guardado parcial.
            try { await texts.Update(new TextUpdate { Id = text.Id, Status = "Error" }); }
            catch (Exception markingError) { logger.LogError(markingError, "No se pudo marcar como Error el texto {TextId}.", text.Id); }
            throw;
        }
    }

    private sealed record PlannedError(SentenceErrorInsert Insert, string Name);
    private sealed record PlannedSentence(string Text, string? CorrectedText, List<PlannedError> Errors);

    private async Task<List<PlannedSentence>> Prepare(List<NlpSentenceRequest> batch, Dictionary<string, NlpSentenceResult> results)
    {
        var categoryIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var plans = new List<PlannedSentence>();
        foreach (var sentence in batch)
        {
            var findings = results[sentence.SentenceId].Errors;
            var boundaries = ParagraphAnalysis.Boundaries(sentence.Text);
            var preparedErrors = new List<PlannedError>();
            foreach (var error in findings)
            {
                if (error is null || error.StartIndex < 0 || error.EndIndex < error.StartIndex
                    || error.EndIndex >= boundaries.Length || string.IsNullOrWhiteSpace(error.ClassificationName)
                    || error.ClassificationName.Trim().Length > 100)
                    throw new NlpApiException("La API NLP devolvió índices o una categoría de error inválidos.");
                var name = error.ClassificationName.Trim();
                if (!categoryIds.TryGetValue(name, out var categoryId))
                {
                    var matches = (await classifications.FindByName(name))
                        .Where(c => c.Active && c.Id != Guid.Empty && string.Equals(c.Name.Trim(), name, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (matches.Count != 1)
                        throw new NlpApiException($"No se encontró una categoría activa e inequívoca para '{name}'.");
                    categoryIds.Add(name, categoryId = matches[0].Id);
                }
                var insert = new SentenceErrorInsert
                {
                    SentenceId = Guid.Parse(sentence.SentenceId), ErrorClassificationId = categoryId,
                    ErrorTokenStartIndex = error.StartIndex, ErrorTokenEndIndex = error.EndIndex,
                    ErrorToken = sentence.Text[boundaries[error.StartIndex]..boundaries[error.EndIndex]],
                    SuggestedCorrection = error.SuggestedCorrection, ConfidenceScore = error.ConfidenceScore, Active = true
                };
                try { Validator.ValidateObject(insert, new ValidationContext(insert), true); }
                catch (ValidationException exception)
                {
                    throw new NlpApiException("La API NLP devolvió un error que no cabe en el contrato de persistencia.", innerException: exception);
                }
                preparedErrors.Add(new PlannedError(insert, name));
            }
            plans.Add(new PlannedSentence(sentence.Text, ParagraphAnalysis.Correct(sentence.Text, findings, boundaries), preparedErrors));
        }
        return plans;
    }
}

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

public sealed class ErrorClassificationService(IErrorClassificationRepository repository) : IErrorClassificationService
{
    public Task<PagedList<ErrorClassificationEntity>> List(PaginationDto<ErrorClassificationList> request) => repository.List(request);

    public Task<ErrorClassificationEntity> Insert(ErrorClassificationInsert request) => repository.Insert(request);

    public Task<ErrorClassificationEntity> Update(ErrorClassificationUpdate request) => repository.Update(request);

    public async Task<ErrorClassificationEntity> Get(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID no puede estar vacío.", nameof(id));
        var items = await repository.List(new PaginationDto<ErrorClassificationList>
        {
            PageNumber = 1, PageSize = 1, Filter = new ErrorClassificationList { Id = id }
        });
        return items.Items.FirstOrDefault(item => item.Id == id)
            ?? throw new KeyNotFoundException("No se encontró el registro.");
    }
}
