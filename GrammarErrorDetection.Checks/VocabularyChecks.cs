using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Net;
using System.Text;
using System.Text.Json;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Texts;
using GrammarErrorDetection.Models.Grammar.Vocabulary;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Repositories;
using GrammarErrorDetection.Services.Configuration;
using GrammarErrorDetection.Services.Exceptions;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NpgsqlTypes;

internal static class VocabularyChecks
{
    public static async Task Run()
    {
        var count = 0;
        Tokenizer();
        await Client();
        await Repository();
        await Service();
        Console.WriteLine($"{count} comprobaciones de vocabulario correctas.");

        void Check(bool condition, string message) { if (!condition) throw new Exception(message); count++; }
        static string Words(IEnumerable<VocabularyToken> tokens) => string.Join(",", tokens.Select(t => t.Word));

        // ---- Tokenizador: qué palabras se verifican y cuáles se omiten por no poder juzgarse sin contexto.
        void Tokenizer()
        {
            var tokens = VocabularyTokenizer.Extract("She recieve a USA letter from John in Paris.");
            Check(Words(tokens) == "she,recieve,letter,from,in",
                "Omite letras sueltas, siglas y nombres propios, y conserva la primera palabra de la oración.");
            var recieve = tokens.Single(t => t.Word == "recieve");
            Check(recieve.StartIndex == 4 && recieve.EndIndex == 11 && recieve.Token == "recieve",
                "Informa la posición UTF-16 de la palabra (fin exclusivo).");
            Check(Words(VocabularyTokenizer.Extract("I don't know Anna's phone.")) == "know,phone",
                "Omite contracciones y posesivos.");
            Check(Words(VocabularyTokenizer.Extract("What? Hello there")) == "what,hello,there",
                "Una mayúscula tras ? o ! inicia oración y se verifica.");
            Check(Words(VocabularyTokenizer.Extract("\"Hello there\"")) == "hello,there",
                "Las comillas de apertura no impiden reconocer el inicio de oración.");
            Check(Words(VocabularyTokenizer.Extract("My iPhone is new")) == "my,is,new", "Omite marcas con mayúscula interna.");
            Check(Words(VocabularyTokenizer.Extract("abc123 test _x y_z")) == "test",
                "No parte palabras pegadas a números o guiones bajos.");
            Check(Words(VocabularyTokenizer.Extract("The café is nice")) == "the,café,is,nice", "Conserva letras con tilde.");
            Check(Words(VocabularyTokenizer.Extract(new string('a', 46) + " ok")) == "ok", "Omite palabras de más de 45 letras.");
            Check(VocabularyTokenizer.Extract("   ").Count == 0 && VocabularyTokenizer.Extract("123 !!").Count == 0,
                "Una oración sin palabras no produce tokens.");
        }

        // ---- Cliente Datamuse: reglas de validez y alternativas con respuestas reales capturadas de la API.
        async Task Client()
        {
            var handler = new Handler();
            var options = new VocabularyOptions();
            var datamuse = new DatamuseLookupClient(new HttpClient(handler), options);

            handler.Routes["words?sp=happy&md=df&max=10"] = """
                [{"word":"happy","score":165077,"tags":["f:49.009288"],"defs":["adj\tHaving a feeling arising from a consciousness of well-being."]},
                 {"word":"sappy","score":19033,"tags":["f:0.082668"],"defs":["adj\t(US) Excessively sweet."]}]
                """;
            var valid = await datamuse.Lookup("Happy ");
            Check(valid.IsValid && valid.Alternatives.Count == 0 && handler.Requests.Count == 1
                && handler.Requests[0] == "https://api.datamuse.com/words?sp=happy&md=df&max=10",
                "Una palabra con entrada propia es válida y se consulta una sola vez, normalizada.");

            handler.Requests.Clear();
            handler.Routes["words?sp=recieve&md=df&max=10"] = """
                [{"word":"recieve","score":148042,"tags":["f:0.033083"],"defs":["v\tMisspelling of receive. [(transitive) To be given, sent, or paid something.]"]},
                 {"word":"relieve","score":18044,"tags":["f:6.876253"],"defs":["v\t(transitive) To ease (a person) from mental distress."]},
                 {"word":"decieve","score":3002,"tags":["f:0.003116"],"defs":["v\tMisspelling of deceive. [(transitive) To trick or mislead.]"]}]
                """;
            handler.Routes["words?sp=receive&md=df&max=3"] = """
                [{"word":"receive","score":1,"tags":["f:100.5"],"defs":["v\t(transitive) To be given, sent, or paid something.",
                  "v\t(transitive) To take, as something offered.","n\tA third sense.","v\tA fourth sense."]}]
                """;
            var misspelled = await datamuse.Lookup("recieve");
            Check(!misspelled.IsValid && misspelled.Alternatives.Count == 1 && misspelled.Alternatives[0].Word == "receive",
                "Un 'Misspelling of X' es inválido y propone solo X, no palabras parecidas como relieve.");
            Check(misspelled.Alternatives[0].Definitions.Count == 3
                && misspelled.Alternatives[0].Definitions[0].PartOfSpeech == "verb"
                && misspelled.Alternatives[0].Definitions[2].PartOfSpeech == "noun"
                && misspelled.Alternatives[0].Definitions[0].Text == "(transitive) To be given, sent, or paid something.",
                "Las definiciones de la alternativa se limitan a tres e informan la categoría gramatical.");
            Check(handler.Requests.Count == 2, "La alternativa se consulta con una llamada adicional.");

            handler.Routes["words?sp=lead&md=df&max=10"] = """
                [{"word":"lead","score":1,"tags":["f:50"],"defs":["n\tA soft metal.","n\tMisspelling of led."]}]
                """;
            Check((await datamuse.Lookup("lead")).IsValid, "Si alguna entrada es un sentido real, la palabra es válida.");
            handler.Routes["words?sp=went&md=df&max=10"] = """[{"word":"went","score":7035,"tags":["f:203.9"]}]""";
            Check((await datamuse.Lookup("went")).IsValid, "Una entrada sin definiciones sigue siendo válida.");

            handler.Routes["words?sp=tomorow&md=df&max=10"] = """
                [{"word":"tomorrow","score":9,"tags":["f:19.29"],"defs":["n\tThe day after today."]},
                 {"word":"tomorrows","score":8,"tags":["f:0.5"],"defs":["n\tPlural."]},
                 {"word":"tomorrow is another day","score":7,"tags":["f:30"],"defs":["n\tA phrase."]}]
                """;
            var unknown = await datamuse.Lookup("tomorow");
            Check(!unknown.IsValid && unknown.Alternatives.Select(a => a.Word).SequenceEqual(["tomorrow"]),
                "Sin entrada propia propone las parecidas, descartando las raras (frecuencia < 1) y las frases.");

            handler.Requests.Clear();
            handler.Routes["words?sp=tomorrow&md=df&max=3"] = "[]";
            handler.Routes["words?sp=asdfgh&md=df&max=10"] = "[]";
            handler.Routes["sug?s=asdfgh&max=5"] = "[]";
            var nothing = await datamuse.Lookup("asdfgh");
            Check(!nothing.IsValid && nothing.Alternatives.Count == 0 && handler.Requests.Count == 2,
                "Una palabra sin ninguna entrada es inválida aunque no haya alternativas.");

            handler.Routes["words?sp=tomorow2&md=df&max=10"] = "[]";
            handler.Routes["sug?s=tomorow2&max=5"] = """[{"word":"tomorrow","score":1033},{"word":"komorowski","score":7}]""";
            handler.Routes["words?sp=tomorrow&md=df&max=1"] = """[{"word":"tomorrow","score":1,"tags":["f:19.29"],"defs":["n\tThe day after today."]}]""";
            handler.Routes["words?sp=komorowski&md=df&max=1"] = "[]";
            var viaSuggestions = await datamuse.Lookup("tomorow2");
            Check(viaSuggestions.Alternatives.Select(a => a.Word).SequenceEqual(["tomorrow"])
                && viaSuggestions.Alternatives[0].Definitions.Single().Text == "The day after today.",
                "Como último recurso usa /sug y descarta las candidatas sin entrada.");

            handler.Status = HttpStatusCode.TooManyRequests;
            var limited = await Throws(() => datamuse.Lookup("limited"));
            Check(limited.StatusCode == 429 && limited.IsSystemic, "El límite de cuota (429) es un fallo sistémico.");
            handler.Status = HttpStatusCode.InternalServerError;
            var broken = await Throws(() => datamuse.Lookup("broken"));
            Check(broken.StatusCode == 500 && !broken.IsSystemic, "Un 500 es un fallo del servicio, no de la cuota.");
            handler.Status = HttpStatusCode.Forbidden;
            Check((await Throws(() => datamuse.Lookup("key"))).IsSystemic, "Si la API exige clave (403) se trata como sistémico.");
            handler.Status = HttpStatusCode.OK;
            handler.Routes["words?sp=object&md=df&max=10"] = """{"detail":[]}""";
            await Throws(() => datamuse.Lookup("object"));
            handler.Routes["words?sp=garbage&md=df&max=10"] = "not json";
            await Throws(() => datamuse.Lookup("garbage"));
            handler.Throw = new HttpRequestException("sin red");
            Check((await Throws(() => datamuse.Lookup("offline"))).StatusCode is null, "Un fallo de red se informa como excepción de vocabulario.");
            handler.Throw = null;
            count += 2;

            var misconfigured = new DatamuseLookupClient(new HttpClient(handler), new VocabularyOptions { BaseUrl = "ftp://x" });
            try { await misconfigured.Lookup("word"); throw new Exception("Se aceptó una URL no HTTP."); }
            catch (InvalidOperationException) { count++; }
            var custom = new DatamuseLookupClient(new HttpClient(handler), new VocabularyOptions { BaseUrl = "https://example.test/dm" });
            handler.Requests.Clear();
            handler.Routes["words?sp=happy&md=df&max=10"] = """[{"word":"happy","score":1,"tags":["f:49"]}]""";
            await custom.Lookup("happy");
            Check(handler.Requests[0] == "https://example.test/dm/words?sp=happy&md=df&max=10", "Respeta una URL base configurada sin barra final.");
        }

        // ---- Repositorio: nombres y tipos de parámetros de los SP, y mapeo de la caché.
        async Task Repository()
        {
            var executor = new WordExecutor();
            var repository = new VocabularyWordRepository(executor);
            var rows = await repository.GetMany(["recieve", "happy"], 90);
            Check(executor.Procedure == "VocabularyWord_GetMany"
                && executor.Parameters.Select(p => p.Name).SequenceEqual(["Words", "Results", "MaxAgeDays"]),
                "GetMany usa exactamente los parámetros del SP.");
            Check(executor.Parameters[0].Type == (NpgsqlDbType.Array | NpgsqlDbType.Text)
                && executor.Parameters[0].Value is string[] sent && sent.SequenceEqual(["recieve", "happy"])
                && executor.Parameters[1].Direction == ParameterDirection.InputOutput
                && executor.Parameters[2].Type == NpgsqlDbType.Integer && Equals(executor.Parameters[2].Value, 90),
                "Envía TEXT[], cursor INOUT y MaxAgeDays entero.");
            Check(rows.Count == 1 && rows[0].Word == "recieve" && !rows[0].IsValid && rows[0].Suggestions.Count == 1
                && rows[0].Suggestions[0].Word == "receive" && rows[0].Suggestions[0].Definitions[0].PartOfSpeech == "verb"
                && rows[0].Suggestions[0].Definitions[0].Text == "To be given something.",
                "Mapea la fila y el JSON de sugerencias.");
            executor.Procedure = null;
            Check((await repository.GetMany([])).Count == 0 && executor.Procedure is null, "Una lista vacía no llama a la base de datos.");
            await Reject(() => repository.GetMany(["a"], 0));
            await Reject(() => repository.GetMany(Enumerable.Range(0, 501).Select(n => "w" + n).ToList()));

            var saved = await repository.Upsert(new VocabularyWordUpsert
            {
                Word = "recieve", IsValid = false,
                Suggestions = [new() { Word = "receive", Definitions = [new() { PartOfSpeech = "verb", Text = "To be given something." }] }]
            });
            Check(executor.Procedure == "VocabularyWord_Upsert"
                && executor.Parameters.Select(p => p.Name).SequenceEqual(["Word", "IsValid", "VocabularyWord", "Suggestions"])
                && executor.Parameters[2].Direction == ParameterDirection.Output
                && executor.Parameters[3].Type == NpgsqlDbType.Jsonb
                && Equals(executor.Parameters[3].Value,
                    """[{"word":"receive","definitions":[{"partOfSpeech":"verb","text":"To be given something."}]}]"""),
                "Upsert usa el compuesto OUT y escribe las sugerencias como JSONB camelCase.");
            Check(saved.Word == "recieve" && saved.Suggestions.Count == 1, "Upsert mapea la entidad guardada.");
            await repository.Upsert(new VocabularyWordUpsert { Word = "happy", IsValid = true });
            Check(Equals(executor.Parameters[3].Value, "[]"), "Una palabra válida se guarda con un arreglo vacío.");
            await Reject(() => repository.Upsert(new VocabularyWordUpsert { Word = " ", IsValid = true }));
            await Reject(() => repository.Upsert(new VocabularyWordUpsert { Word = new string('a', 65), IsValid = true }));

            async Task Reject(Func<Task> action)
            {
                try { await action(); }
                catch (ValidationException) { count++; return; }
                throw new Exception("Se aceptaron datos inválidos.");
            }
        }

        // ---- Servicio: caché, tolerancia a fallos y control de acceso.
        async Task Service()
        {
            var owner = Guid.NewGuid();
            var text = Text(owner,
                ("She recieve a letter from John.", true),
                ("Hello there, I am happy.", true),
                ("Ignored recieve here.", false));
            var texts = new TextStore(text);
            var cache = new WordCache();
            var lookup = new LookupFake();
            lookup.Results["recieve"] = new(false, [new() { Word = "receive", Definitions = [new() { PartOfSpeech = "verb", Text = "To get." }] }]);
            var options = new VocabularyOptions { MaxConcurrency = 1 };
            var service = new VocabularyService(texts, cache, lookup, options, NullLogger<VocabularyService>.Instance);

            var first = await service.Analyze(owner, false, text.Id);
            Check(texts.LastRequest!.Filter!.Id == text.Id && texts.LastRequest.Filter.UserId == owner && texts.LastRequest.Filter.Active == true,
                "Un usuario sin permiso global filtra por sus propios textos y solo textos activos.");
            Check(first.Sentences.Count == 2 && first.Sentences[0].Issues.Count == 1 && first.Sentences[1].Issues.Count == 0
                && first.IssueCount == 1 && first.UncheckedCount == 0 && first.Source == "Datamuse",
                "Ignora oraciones inactivas; una oración sin errores de vocabulario queda con la lista vacía.");
            var issue = first.Sentences[0].Issues[0];
            Check(issue.Token == "recieve" && issue.StartIndex == 4 && issue.EndIndex == 11
                && issue.Alternatives.Single().Word == "receive" && issue.Alternatives[0].Definitions[0].Text == "To get.",
                "La palabra con error lleva su posición, alternativa y definiciones.");
            Check(lookup.Calls.Count == 8 && lookup.Calls.Distinct().Count() == 8,
                "Cada palabra distinta se consulta una sola vez, aunque se repita (she, recieve, letter, from, hello, there, am, happy).");
            Check(cache.Stored.Count == 8 && !cache.Stored["recieve"].IsValid && cache.Stored["recieve"].Suggestions.Count == 1
                && cache.Stored["happy"].IsValid && cache.Stored["happy"].Suggestions.Count == 0,
                "Guarda todas las palabras: las inválidas con sugerencias y las válidas sin ellas.");

            lookup.Calls.Clear();
            var second = await service.Analyze(owner, false, text.Id);
            Check(lookup.Calls.Count == 0 && second.IssueCount == 1 && second.Sentences[0].Issues[0].Alternatives.Single().Word == "receive",
                "Una segunda consulta sale de la caché: cero llamadas a la API externa.");

            var json = JsonSerializer.SerializeToElement(first, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Check(json.GetProperty("issueCount").GetInt32() == 1 && json.GetProperty("source").GetString() == "Datamuse"
                && json.GetProperty("sentences")[0].GetProperty("issues")[0].GetProperty("alternatives")[0]
                    .GetProperty("definitions")[0].GetProperty("partOfSpeech").GetString() == "verb",
                "Serializa el contrato esperado por la WEB.");

            // Fallo sistémico a mitad de camino: lo verificado se conserva, lo demás queda sin verificar y no se cachea.
            var freshCache = new WordCache();
            var failing = new LookupFake { FailOn = "recieve", FailWith = new VocabularyLookupException("cuota", 429) };
            failing.Results["recieve"] = lookup.Results["recieve"];
            var degraded = await new VocabularyService(texts, freshCache, failing, options, NullLogger<VocabularyService>.Instance)
                .Analyze(owner, false, text.Id);
            Check(degraded.UncheckedCount == 7 && degraded.Sentences[0].UncheckedCount == 3 && degraded.IssueCount == 0
                && freshCache.Stored.Keys.SequenceEqual(["she"]) && failing.Calls.SequenceEqual(["she", "recieve"]),
                "Ante un 429 deja de consultar, informa las palabras sin verificar y no guarda las fallidas.");

            // Tope de palabras nuevas por consulta.
            var capped = new LookupFake();
            var cappedService = new VocabularyService(texts, new WordCache(), capped,
                new VocabularyOptions { MaxConcurrency = 1, MaxNewLookupsPerRequest = 2 }, NullLogger<VocabularyService>.Instance);
            var partial = await cappedService.Analyze(owner, false, text.Id);
            Check(capped.Calls.Count == 2 && partial.UncheckedCount == 6, "Respeta el tope de palabras nuevas por consulta.");

            // Si no se puede guardar en la caché, la respuesta igual se entrega.
            var unsaved = new VocabularyService(texts, new WordCache { FailUpsert = true }, new LookupFake { Results = { ["recieve"] = lookup.Results["recieve"] } },
                options, NullLogger<VocabularyService>.Instance);
            Check((await unsaved.Analyze(owner, false, text.Id)).IssueCount == 1, "Un fallo al guardar en la caché no impide devolver el análisis.");

            // Mayúscula inicial: se verifica en minúsculas y se muestra tal como se escribió.
            var capital = Text(owner, ("Recieve it now.", true));
            var capitalResult = await new VocabularyService(new TextStore(capital), cache, lookup, options, NullLogger<VocabularyService>.Instance)
                .Analyze(owner, false, capital.Id);
            Check(capitalResult.Sentences[0].Issues.Single().Token == "Recieve" && capitalResult.Sentences[0].Issues[0].StartIndex == 0,
                "Una palabra con mayúscula inicial de oración se muestra como se escribió.");

            // Control de acceso.
            await service.Analyze(Guid.NewGuid(), true, text.Id);
            Check(texts.LastRequest!.Filter!.UserId is null, "Docentes y coordinadores consultan cualquier texto (sin filtrar por usuario).");
            try { await service.Analyze(Guid.NewGuid(), false, text.Id); throw new Exception("Se mostró un texto ajeno."); }
            catch (KeyNotFoundException) { count++; }
            try { await service.Analyze(owner, false, Guid.NewGuid()); throw new Exception("Se aceptó un texto inexistente."); }
            catch (KeyNotFoundException) { count++; }
            try { await service.Analyze(Guid.Empty, false, text.Id); throw new Exception("Se aceptó un usuario vacío."); }
            catch (ValidationException) { count++; }
            try { await service.Analyze(owner, false, Guid.Empty); throw new Exception("Se aceptó un texto vacío."); }
            catch (ValidationException) { count++; }
        }
    }

    private static async Task<VocabularyLookupException> Throws(Func<Task> action)
    {
        try { await action(); }
        catch (VocabularyLookupException exception) { return exception; }
        throw new Exception("Se esperaba una excepción de vocabulario.");
    }

    private static TextListItemDto Text(Guid userId, params (string Sentence, bool Active)[] sentences)
    {
        var text = new TextListItemDto { Id = Guid.NewGuid(), UserId = userId, OriginalContent = string.Join(" ", sentences.Select(s => s.Sentence)), Active = true };
        for (var index = 0; index < sentences.Length; index++)
            text.Sentences.Add(new SentenceErrorDto
            {
                Id = Guid.NewGuid(), TextId = text.Id, SentenceIndex = index + 1,
                OriginalSentence = sentences[index].Sentence, Active = sentences[index].Active
            });
        return text;
    }

    private sealed class Handler : HttpMessageHandler
    {
        public Dictionary<string, string> Routes { get; } = [];
        public List<string> Requests { get; } = [];
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public Exception? Throw { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!.ToString());
            if (Throw is not null) throw Throw;
            var key = request.RequestUri!.PathAndQuery.TrimStart('/');
            // Una ruta con el prefijo de una base con ruta (example.test/dm/...) se resuelve por su final.
            var route = Routes.FirstOrDefault(r => key == r.Key || key.EndsWith("/" + r.Key, StringComparison.Ordinal));
            var body = route.Key is null ? "[]" : route.Value;
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    private sealed class LookupFake : IVocabularyLookupClient
    {
        public Dictionary<string, VocabularyLookupResult> Results { get; } = [];
        public List<string> Calls { get; } = [];
        public string? FailOn { get; set; }
        public VocabularyLookupException? FailWith { get; set; }

        public Task<VocabularyLookupResult> Lookup(string word, CancellationToken cancellationToken = default)
        {
            Calls.Add(word);
            if (word == FailOn && FailWith is not null) throw FailWith;
            return Task.FromResult(Results.TryGetValue(word, out var result) ? result : new VocabularyLookupResult(true, []));
        }
    }

    private sealed class WordCache : IVocabularyWordRepository
    {
        public Dictionary<string, VocabularyWordEntity> Stored { get; } = [];
        public bool FailUpsert { get; set; }

        public Task<List<VocabularyWordEntity>> GetMany(IReadOnlyCollection<string> words, int maxAgeDays = 180)
            => Task.FromResult(words.Where(Stored.ContainsKey).Select(word => Stored[word]).ToList());

        public Task<VocabularyWordEntity> Upsert(VocabularyWordUpsert request)
        {
            if (FailUpsert) throw new DataException("Base de datos no disponible.");
            var entity = new VocabularyWordEntity
            {
                Id = Guid.NewGuid(), Word = request.Word, IsValid = request.IsValid, Suggestions = request.Suggestions, Active = true
            };
            Stored[request.Word] = entity;
            return Task.FromResult(entity);
        }
    }

    private sealed class TextStore(TextListItemDto text) : ITextRepository
    {
        public PaginationDto<TextList>? LastRequest { get; private set; }

        public Task<List<TextListItemDto>> List(PaginationDto<TextList> request)
        {
            LastRequest = request;
            var filter = request.Filter ?? new TextList();
            var matches = new[] { text }.Where(item => (filter.Id is null || item.Id == filter.Id)
                && (filter.UserId is null || item.UserId == filter.UserId) && (filter.Active is null || item.Active == filter.Active)).ToList();
            return Task.FromResult(matches);
        }

        public Task<TextEntity> Insert(TextInsert request) => throw new NotSupportedException();
        public Task<TextEntity> CompleteAnalysis(Guid id, Guid? registrationId) => throw new NotSupportedException();
        public Task<TextEntity> Update(TextUpdate request) => throw new NotSupportedException();
    }

    private sealed class WordExecutor : IStoredProcedureExecutor
    {
        public string? Procedure { get; set; }
        public IReadOnlyList<StoredProcedureParameter> Parameters { get; private set; } = [];

        public Task<ResourceResponse<DataTable>> ExecuteStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, StoredProcedureResult? result = null)
        {
            if (schema != "grammar") throw new Exception("Esquema incorrecto.");
            Procedure = procedureName;
            Parameters = parameters;
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("Word", typeof(string));
            table.Columns.Add("IsValid", typeof(bool));
            table.Columns.Add("Suggestions", typeof(string));
            table.Columns.Add("FetchedAt", typeof(DateTime));
            table.Columns.Add("CreatedAt", typeof(DateTime));
            table.Columns.Add("UpdatedAt", typeof(DateTime));
            table.Columns.Add("Active", typeof(bool));
            var row = table.NewRow();
            row["Id"] = Guid.NewGuid();
            row["Word"] = "recieve";
            row["IsValid"] = false;
            row["Suggestions"] = """[{"word":"receive","definitions":[{"partOfSpeech":"verb","text":"To be given something."}]}]""";
            row["FetchedAt"] = row["CreatedAt"] = row["UpdatedAt"] = new DateTime(2026, 11, 18);
            row["Active"] = true;
            table.Rows.Add(row);
            return Task.FromResult(ResourceResponse<DataTable>.Ok(table));
        }
    }
}
