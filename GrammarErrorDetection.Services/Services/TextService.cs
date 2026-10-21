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

namespace GrammarErrorDetection.Services.Services;

public sealed class TextService(ITextRepository texts, ISentenceRepository sentences,
    ISentenceErrorRepository errors, IErrorClassificationRepository classifications,
    INlpService nlp, ILogger<TextService> logger) : ITextService
{
    public Task<List<TextListItemDto>> List(Guid userId, bool canViewAll, PaginationDto<TextList> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (userId == Guid.Empty) throw new ValidationException("El identificador de usuario no puede estar vacío.");
        request.Filter ??= new TextList();
        if (!canViewAll) request.Filter.UserId = userId;
        return texts.List(request);
    }

    public async Task<TextListItemDto> Upload(Guid userId, TextUploadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (userId == Guid.Empty) throw new ValidationException("El identificador de usuario no puede estar vacío.");
        Validator.ValidateObject(request, new ValidationContext(request), true);
        var batch = ParagraphAnalysis.Split(request.OriginalContent);
        if (batch.Count == 0) throw new ValidationException("El párrafo debe contener al menos una oración.");
        var response = await nlp.Analyze(batch, cancellationToken);
        var byId = ParagraphAnalysis.Validate(batch, response);
        var plans = await Prepare(batch, byId);
        cancellationToken.ThrowIfCancellationRequested();

        // La validación NLP y de categorías termina antes de la primera escritura.
        var text = await texts.Insert(new TextInsert
        {
            UserId = userId, OriginalContent = request.OriginalContent,
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
            text = await texts.Update(new TextUpdate { Id = text.Id, Status = "Procesado" });
            return new TextListItemDto
            {
                Id = text.Id, UserId = text.UserId, SubmissionDate = text.SubmissionDate,
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
