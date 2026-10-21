using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.Texts;
using GrammarErrorDetection.Repositories.Repositories;
using NpgsqlTypes;

internal static class TextRepositoryChecks
{
    public static async Task Run()
    {
        var count = 0;
        var executor = new TextExecutor();
        var repository = new TextRepository(executor);
        var date = new DateOnly(2026, 10, 20);
        var rows = await repository.List(new PaginationDto<TextList>
        {
            PageNumber = 2, PageSize = 10,
            Filter = new() { Id = executor.TextId, UserId = executor.UserId, SubmissionDate = date,
                OriginalContent = "Hello.", Status = "Pendiente", Active = false, CreatedAt = date }
        });
        Check(executor.Procedure == "Text_Detail_List" && executor.Parameters.Select(p => p.Name).SequenceEqual(
            ["Id", "UserId", "SubmissionDate", "OriginalContent", "Status", "Active", "CreatedAt", "PageNumber", "PageSize", "Results"]),
            "Usa exactamente los parámetros de Text_Detail_List.");
        Check(executor.Parameters[2].Type == NpgsqlDbType.Date && Equals(executor.Parameters[2].Value, date)
            && executor.Parameters[3].Type == NpgsqlDbType.Text && Equals(executor.Parameters[5].Value, false)
            && executor.Parameters[9].Direction == ParameterDirection.InputOutput,
            "Conserva fechas DATE, texto, Active=false y cursor INOUT.");
        Check(rows.Single().Id == executor.TextId && rows[0].Sentences.Count == 3
            && rows[0].Sentences[0].OriginalSentence == "She go home."
            && rows[0].Sentences[0].Errors.Single().ErrorClassificationName == "Concordancia sujeto-verbo"
            && rows[0].Sentences[0].Errors[0].ErrorToken == "go"
            && rows[0].Sentences[0].Errors[0].ConfidenceScore == 95.25m && !rows[0].Active,
            "Mapea el JSON real de Text_Detail_List sin exigir un campo User inexistente.");
        Check(rows[0].ErrorCount == 3 && rows[0].Sentences.Select(s => s.ErrorCount).SequenceEqual([1, 2, 0]),
            "Cuenta tres errores del párrafo, aunque dos compartan categoría, y cuenta cada oración.");
        var json = JsonSerializer.SerializeToElement(rows[0], new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Check(json.GetProperty("errorCount").GetInt32() == 3
            && json.GetProperty("sentences")[1].GetProperty("errorCount").GetInt32() == 2
            && !json.TryGetProperty("user", out _), "Serializa los conteos y no inventa datos de usuario.");
        rows[0].Sentences[2].Errors.Add(new SentenceErrorDetailDto { ErrorClassificationName = "Prueba" });
        Check(rows[0].ErrorCount == 4, "El conteo calculado se actualiza al cambiar los errores.");
        await repository.List(new() { PageNumber = 1, PageSize = 20 });
        Check(executor.Parameters.Take(7).All(p => p.Value is null), "El filtro vacío no añade fechas, estados ni UUID por defecto.");
        executor.WithoutSentences = true;
        var empty = (await repository.List(new() { PageNumber = 1, PageSize = 20 })).Single();
        Check(empty.Sentences.Count == 0 && empty.ErrorCount == 0,
            "Un texto sin oraciones devuelve un array vacío y cero errores.");
        var inserted = await repository.Insert(new() { UserId = executor.UserId, OriginalContent = "Hello." });
        Check(executor.Procedure == "Text_Insert" && executor.Parameters.Single(p => p.Name == "Text").Direction == ParameterDirection.Output
            && executor.Parameters.Single(p => p.Name == "SubmissionDate").Value is null
            && Equals(executor.Parameters.Single(p => p.Name == "Status").Value, "Pendiente")
            && Equals(executor.Parameters.Single(p => p.Name == "Active").Value, true),
            "Insert usa el compuesto OUT y conserva los valores opcionales del contrato.");
        Check(inserted.Id == executor.TextId && !Json(inserted).TryGetProperty("sentences", out _)
            && !Json(inserted).TryGetProperty("user", out _), "Insert mapea la entidad sin exigir columnas exclusivas del listado.");
        var utcDate = new DateTime(2026, 10, 20, 12, 30, 0, DateTimeKind.Utc);
        await repository.Update(new() { Id = executor.TextId, SubmissionDate = utcDate, Active = false });
        Check(executor.Procedure == "Text_Update"
            && Equals(executor.Parameters.Single(p => p.Name == "SubmissionDate").Value, new DateTime(2026, 10, 20, 8, 30, 0))
            && executor.Parameters.Single(p => p.Name == "UserId").Value is null
            && executor.Parameters.Single(p => p.Name == "Status").Value is null
            && Equals(executor.Parameters.Single(p => p.Name == "Active").Value, false),
            "Update conserva null y false y normaliza TIMESTAMP a hora de Bolivia.");
        var updated = await repository.Update(new() { Id = executor.TextId });
        Check(executor.Parameters.Where(p => p.Direction == ParameterDirection.Input && p.Name != "Id").All(p => p.Value is null)
            && updated.Id == executor.TextId, "Update con solo Id no sobrescribe los datos omitidos con valores por defecto.");
        await Reject(() => repository.Insert(new() { OriginalContent = "Hello." }));
        await Reject(() => repository.Insert(new() { UserId = executor.UserId, OriginalContent = " " }));
        await Reject(() => repository.Update(new()));
        await Reject(() => repository.List(new() { PageNumber = 1, PageSize = 20, Filter = new() { UserId = Guid.Empty } }));
        Check(typeof(TextList).GetProperty("Sentences") is null && typeof(TextList).GetProperty("User") is null
            && typeof(TextList).GetProperty("UpdatedAt") is null, "El modelo de filtro no contiene campos de la respuesta.");
        Console.WriteLine($"{count} comprobaciones del repository de textos correctas.");

        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            count++;
        }
        async Task Reject(Func<Task> action)
        {
            try { await action(); }
            catch (ValidationException) { count++; return; }
            throw new Exception("Se aceptaron datos inválidos.");
        }
    }

    private static JsonElement Json(TextEntity text) => JsonSerializer.SerializeToElement(text, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private sealed class TextExecutor : IStoredProcedureExecutor
    {
        public Guid TextId { get; } = Guid.NewGuid();
        public Guid UserId { get; } = Guid.NewGuid();
        public bool WithoutSentences { get; set; }
        public string? Procedure { get; private set; }
        public IReadOnlyList<StoredProcedureParameter> Parameters { get; private set; } = [];
        public Task<ResourceResponse<DataTable>> ExecuteStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, StoredProcedureResult? result = null)
        {
            if (schema != "grammar") throw new Exception("Esquema incorrecto.");
            Procedure = procedureName;
            Parameters = parameters;
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("UserId", typeof(Guid));
            table.Columns.Add("SubmissionDate", typeof(DateTime));
            table.Columns.Add("OriginalContent", typeof(string));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("CreatedAt", typeof(DateTime));
            table.Columns.Add("UpdatedAt", typeof(DateTime));
            table.Columns.Add("Active", typeof(bool));
            if (procedureName == "Text_Detail_List")
            {
                table.Columns.Add("Sentences", typeof(string));
            }
            var row = table.NewRow();
            row["Id"] = TextId;
            row["UserId"] = UserId;
            row["SubmissionDate"] = new DateTime(2026, 10, 20);
            row["OriginalContent"] = "Hello.";
            row["Status"] = "Pendiente";
            row["CreatedAt"] = new DateTime(2026, 10, 20);
            row["UpdatedAt"] = new DateTime(2026, 10, 20);
            row["Active"] = false;
            if (procedureName == "Text_Detail_List")
            {
                row["Sentences"] = WithoutSentences ? DBNull.Value : """
                    [
                      {
                        "SentenceIndex": 1,
                        "OriginalSentence": "She go home.",
                        "CorrectedSentence": "She goes home.",
                        "Errors": [
                          {
                            "ErrorToken": "go",
                            "ErrorTokenStartIndex": 4,
                            "ErrorTokenEndIndex": 6,
                            "ErrorClassificationName": "Concordancia sujeto-verbo",
                            "ConfidenceScore": 95.25
                          }
                        ]
                      },
                      {
                        "SentenceIndex": 2,
                        "OriginalSentence": "He have a apple.",
                        "CorrectedSentence": "He has an apple.",
                        "Errors": [
                          { "ErrorToken": "have", "ErrorTokenStartIndex": 3, "ErrorTokenEndIndex": 7,
                            "ErrorClassificationName": "Concordancia sujeto-verbo" },
                          { "ErrorToken": "a", "ErrorTokenStartIndex": 8, "ErrorTokenEndIndex": 9,
                            "ErrorClassificationName": "Uso de artículos" }
                        ]
                      },
                      { "SentenceIndex": 3, "OriginalSentence": "Hello!", "CorrectedSentence": null, "Errors": [] }
                    ]
                    """;
            }
            table.Rows.Add(row);
            return Task.FromResult(ResourceResponse<DataTable>.Ok(table));
        }
    }
}
