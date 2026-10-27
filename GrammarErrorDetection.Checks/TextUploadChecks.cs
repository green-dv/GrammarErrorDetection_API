using System.ComponentModel.DataAnnotations;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.ErrorClassifications;
using GrammarErrorDetection.Models.Grammar.SentenceErrors;
using GrammarErrorDetection.Models.Grammar.Sentences;
using GrammarErrorDetection.Models.Grammar.Texts;
using GrammarErrorDetection.Models.Nlp;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Services.Exceptions;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services;
using Microsoft.Extensions.Logging.Abstractions;

internal static class TextUploadChecks
{
    public static async Task Run()
    {
        var checks = 0;
        var userId = Guid.NewGuid();
        var db = new Memory();
        var nlp = new Analyzer();
        var service = Service(db, nlp);
        nlp.Response = batch =>
        {
            Check(db.Texts.Count == 0, "NLP se consulta antes de escribir en la BD.");
            return batch.Select((s, i) => new NlpSentenceResult
            {
                SentenceId = s.SentenceId,
                Errors = i == 0 ? [Error(4, 6)] : []
            }).Reverse().ToList();
        };
        var result = await service.Upload(userId, new() { OriginalContent = " She go home. .. Hello! " });
        Check(nlp.Batch.Select(s => s.Text).SequenceEqual(["She go home.", "Hello!"]),
            "Divide por puntos, conserva puntuación, ignora segmentos vacíos e incluye el final sin punto.");
        Check(result.UserId == userId && result.OriginalContent == " She go home. .. Hello! "
            && result.Status == "Procesado" && result.ErrorCount == 1 && result.Sentences.Count == 2,
            "Guarda el párrafo original para el usuario autenticado y devuelve el resultado completo.");
        Check(result.Sentences[0].SentenceIndex == 1 && result.Sentences[1].SentenceIndex == 2
            && result.Sentences[0].CorrectedSentence == "She goes home." && result.Sentences[1].CorrectedSentence is null,
            "Relaciona por ID aunque NLP devuelva otro orden y aplica la corrección a su oración.");
        var error = db.Errors.Single();
        Check(error.SentenceId == db.Sentences[0].Id && error.ErrorClassificationId == db.CategoryId
            && error.ErrorToken == "go" && error.SuggestedCorrection == "goes" && error.ConfidenceScore == 0.95m,
            "Guarda IDs reales y extrae el token usando los offsets de la oración.");
        Check(result.Sentences.All(s => nlp.Batch.All(b => b.SentenceId != s.Id.ToString()))
            && result.Sentences[0].Errors[0].SentenceId == result.Sentences[0].Id,
            "Los correladores NLP no sustituyen los IDs generados por los SPs.");

        var cleanDb = new Memory();
        var clean = await Service(cleanDb, new Analyzer()).Upload(userId, new() { OriginalContent = "Hello." });
        Check(clean.ErrorCount == 0 && cleanDb.CategoryCalls == 0 && clean.Status == "Procesado",
            "Un análisis sin errores se guarda sin buscar categorías.");

        await Rejected(batch => [], "Una respuesta incompleta no escribe nada.");
        await Rejected(batch => [new() { SentenceId = "unknown", Errors = [] }], "Un ID desconocido no escribe nada.");
        await Rejected(batch => [new() { SentenceId = batch[0].SentenceId }, new() { SentenceId = batch[0].SentenceId }],
            "Los resultados duplicados no escriben nada.");
        await Rejected(batch => [new() { SentenceId = batch[0].SentenceId, Errors = [Error(0, 999)] }],
            "Los índices fuera de rango no escriben nada.");
        await Rejected(batch => [new() { SentenceId = batch[0].SentenceId, Errors = [new() { StartIndex = 0, EndIndex = 1 }] }],
            "Una categoría vacía no escribe nada.");
        var unknownDb = new Memory { MissingCategory = true };
        var withError = new Analyzer { Response = batch => [new() { SentenceId = batch[0].SentenceId, Errors = [Error(4, 6)] }] };
        try { await Service(unknownDb, withError).Upload(userId, new() { OriginalContent = "She go." }); throw new Exception("Se aceptó categoría inexistente."); }
        catch (NlpApiException) { Check(unknownDb.Texts.Count == 0, "Resuelve las categorías antes de guardar."); }

        var unicodeDb = new Memory();
        var unicodeNlp = new Analyzer { Response = batch => [new() { SentenceId = batch[0].SentenceId, Errors = [Error(6, 8)] }] };
        var unicode = await Service(unicodeDb, unicodeNlp).Upload(userId, new() { OriginalContent = "😀 She go." });
        Check(unicodeDb.Errors[0].ErrorToken == "go" && unicode.Sentences[0].CorrectedSentence == "😀 She goes.",
            "Convierte offsets Unicode a UTF-16 sin cortar caracteres suplementarios.");

        var brokenDb = new Memory { FailErrorInsert = true };
        try { await Service(brokenDb, withError).Upload(userId, new() { OriginalContent = "She go." }); throw new Exception("Se ocultó el fallo de BD."); }
        catch (InvalidOperationException e) when (e.Message == "Insert falló")
        {
            Check(brokenDb.Texts.Single().Status == "Error" && brokenDb.Sentences.Count == 1,
                "Un fallo de guardado parcial conserva el error original y marca el texto como Error.");
        }
        try { await service.Upload(userId, new() { OriginalContent = " ... " }); throw new Exception("Se aceptó un párrafo sin oraciones."); }
        catch (ValidationException) { checks++; }
        try { await service.Upload(Guid.Empty, new() { OriginalContent = "Hello." }); throw new Exception("Se aceptó usuario vacío."); }
        catch (ValidationException) { checks++; }
        var otherUser = Guid.NewGuid();
        await db.Insert(new TextInsert { UserId = otherUser, OriginalContent = "Other student's paragraph." });
        var own = await service.List(userId, false, new()
        {
            PageNumber = 1, PageSize = 20, Filter = new() { UserId = otherUser }
        });
        Check(own.Count == 1 && own.All(t => t.UserId == userId),
            "Un estudiante no puede listar textos ajenos alterando Filter.UserId.");
        var all = await service.List(userId, true, new() { PageNumber = 1, PageSize = 20 });
        Check(all.Count == 2, "El acceso de Teacher/Coordinator permite el listado general.");
        var filtered = await service.List(userId, true, new()
        {
            PageNumber = 1, PageSize = 20, Filter = new() { UserId = otherUser }
        });
        Check(filtered.Single().UserId == otherUser, "El acceso general conserva el filtro del estudiante solicitado.");
        Console.WriteLine($"{checks} comprobaciones del flujo de carga de párrafos correctas.");

        TextService Service(Memory memory, Analyzer analyzer) => new(memory, memory, memory, memory, analyzer, NullLogger<TextService>.Instance);
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
        async Task Rejected(Func<IReadOnlyList<NlpSentenceRequest>, List<NlpSentenceResult>> response, string message)
        {
            var memory = new Memory();
            try { await Service(memory, new Analyzer { Response = response }).Upload(userId, new() { OriginalContent = "She go." }); }
            catch (NlpApiException) { Check(memory.Texts.Count == 0, message); return; }
            throw new Exception(message);
        }
    }

    private static NlpError Error(int start, int end) => new()
    {
        StartIndex = start, EndIndex = end, ClassificationName = "Concordancia",
        SuggestedCorrection = "goes", ConfidenceScore = 0.95m
    };

    private sealed class Analyzer : INlpService
    {
        public IReadOnlyList<NlpSentenceRequest> Batch { get; private set; } = [];
        public Func<IReadOnlyList<NlpSentenceRequest>, List<NlpSentenceResult>> Response { get; set; } =
            batch => batch.Select(s => new NlpSentenceResult { SentenceId = s.SentenceId }).ToList();
        public Task<List<NlpSentenceResult>> Analyze(IReadOnlyList<NlpSentenceRequest> sentences, CancellationToken cancellationToken = default)
        {
            Batch = sentences;
            return Task.FromResult(Response(sentences));
        }
    }

    private sealed class Memory : ITextRepository, ISentenceRepository, ISentenceErrorRepository, IErrorClassificationRepository
    {
        public Guid CategoryId { get; } = Guid.NewGuid();
        public int CategoryCalls { get; private set; }
        public bool MissingCategory { get; set; }
        public bool FailErrorInsert { get; set; }
        public List<TextEntity> Texts { get; } = [];
        public List<SentenceEntity> Sentences { get; } = [];
        public List<SentenceErrorEntity> Errors { get; } = [];
        public Task<List<ErrorClassificationEntity>> List(PaginationDto<ErrorClassificationList> request)
            => throw new NotSupportedException("Este doble solo comprueba la resolución de categorías por nombre.");
        public Task<List<ErrorClassificationEntity>> FindByName(string name)
        {
            CategoryCalls++;
            return Task.FromResult<List<ErrorClassificationEntity>>(MissingCategory ? [] : [new() { Id = CategoryId, Name = name }]);
        }
        public Task<TextEntity> Insert(TextInsert request)
        {
            var text = new TextEntity { Id = Guid.NewGuid(), UserId = request.UserId, OriginalContent = request.OriginalContent,
                SubmissionDate = DateTime.UtcNow, Status = request.Status, Active = request.Active };
            Texts.Add(text);
            return Task.FromResult(text);
        }
        public Task<TextEntity> Update(TextUpdate request)
        {
            var text = Texts.Single(t => t.Id == request.Id);
            text.Status = request.Status ?? text.Status;
            return Task.FromResult(text);
        }
        public Task<SentenceEntity> Insert(SentenceInsert request)
        {
            var sentence = new SentenceEntity { Id = Guid.NewGuid(), TextId = request.TextId, SentenceIndex = request.SentenceIndex!.Value,
                OriginalSentence = request.OriginalSentence, CorrectedSentence = request.CorrectedSentence, Active = request.Active };
            Sentences.Add(sentence);
            return Task.FromResult(sentence);
        }
        public Task<SentenceErrorEntity> Insert(SentenceErrorInsert request)
        {
            if (FailErrorInsert) throw new InvalidOperationException("Insert falló");
            var error = new SentenceErrorEntity { Id = Guid.NewGuid(), SentenceId = request.SentenceId,
                ErrorClassificationId = request.ErrorClassificationId, ErrorToken = request.ErrorToken,
                ErrorTokenStartIndex = request.ErrorTokenStartIndex!.Value, ErrorTokenEndIndex = request.ErrorTokenEndIndex!.Value,
                ConfidenceScore = request.ConfidenceScore, SuggestedCorrection = request.SuggestedCorrection, Active = request.Active };
            Errors.Add(error);
            return Task.FromResult(error);
        }
        public Task<List<TextListItemDto>> List(PaginationDto<TextList> request) => Task.FromResult(
            Texts.Where(t => request.Filter?.UserId is null || t.UserId == request.Filter.UserId)
                .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
                .Select(t => new TextListItemDto { Id = t.Id, UserId = t.UserId, OriginalContent = t.OriginalContent }).ToList());
    }
}
