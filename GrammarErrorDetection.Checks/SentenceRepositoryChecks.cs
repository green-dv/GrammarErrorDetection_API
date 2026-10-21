using System.ComponentModel.DataAnnotations;
using System.Data;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Infrastructure.Responses;
using GrammarErrorDetection.Models.Grammar.Sentences;
using GrammarErrorDetection.Models.Grammar.SentenceErrors;
using GrammarErrorDetection.Repositories.Repositories;
using NpgsqlTypes;

internal static class SentenceRepositoryChecks
{
    public static async Task Run()
    {
        var executor = new InsertExecutor();
        var sentences = new SentenceRepository(executor);
        var errors = new SentenceErrorRepository(executor);
        var textId = Guid.NewGuid();
        var sentence = await sentences.Insert(new SentenceInsert
        {
            TextId = textId, SentenceIndex = 0, OriginalSentence = "She go."
        });
        Check(executor.Procedure == "Sentence_Insert" && executor.Parameters.Select(p => p.Name).SequenceEqual(
            ["TextId", "SentenceIndex", "OriginalSentence", "Sentence", "CorrectedSentence", "Active"])
            && executor.Parameters.Single(p => p.Name == "Sentence").Direction == ParameterDirection.Output,
            "Sentence_Insert usa su firma y su parámetro OUT.");
        Check(sentence.Id == executor.Id && sentence.TextId == textId && sentence.SentenceIndex == 0
            && sentence.CorrectedSentence is null && sentence.Active && sentence.CreatedAt == executor.Timestamp,
            "Mapea la oración completa y conserva índice cero, corrección null y activación por defecto.");
        var classificationId = Guid.NewGuid();
        var error = await errors.Insert(new SentenceErrorInsert
        {
            SentenceId = sentence.Id, ErrorClassificationId = classificationId,
            ErrorTokenStartIndex = 4, ErrorTokenEndIndex = 6,
            ErrorToken = "go", SuggestedCorrection = "goes", ConfidenceScore = 95.25m, Active = false
        });
        Check(executor.Procedure == "SentenceError_Insert" && executor.Parameters.Select(p => p.Name).SequenceEqual(
            ["SentenceId", "ErrorClassificationId", "ErrorTokenStartIndex", "ErrorTokenEndIndex", "SentenceError",
                "ErrorToken", "SuggestedCorrection", "ConfidenceScore", "Active"])
            && executor.Parameters.Single(p => p.Name == "SentenceError").Direction == ParameterDirection.Output
            && executor.Parameters.Single(p => p.Name == "ConfidenceScore").Type == NpgsqlDbType.Numeric,
            "SentenceError_Insert usa el OUT compuesto y NUMERIC para la precisión.");
        Check(error.SentenceId == sentence.Id && error.ErrorClassificationId == classificationId
            && error.ErrorToken == "go" && error.SuggestedCorrection == "goes"
            && error.ConfidenceScore == 95.25m && !error.Active,
            "Mapea el error sin exigir nombre de categoría y conserva decimales y Active=false.");
        error = await errors.Insert(new SentenceErrorInsert
        {
            SentenceId = sentence.Id, ErrorClassificationId = classificationId,
            ErrorTokenStartIndex = 0, ErrorTokenEndIndex = 0
        });
        Check(error.ErrorToken is null && error.SuggestedCorrection is null && error.ConfidenceScore is null && error.Active,
            "Conserva campos opcionales null y Active=true por defecto.");
        var previousCalls = executor.Calls;
        try
        {
            await sentences.Insert(new() { TextId = textId, OriginalSentence = "Hello." });
            throw new Exception("Se aceptó SentenceIndex ausente.");
        }
        catch (ValidationException) { }
        Check(executor.Calls == previousCalls, "Valida campos obligatorios antes de ejecutar el SP.");
        executor.ReturnNoRows = true;
        try
        {
            await errors.Insert(new() { SentenceId = sentence.Id, ErrorClassificationId = classificationId,
                ErrorTokenStartIndex = 0, ErrorTokenEndIndex = 1 });
            throw new Exception("Se aceptó una salida vacía.");
        }
        catch (DataException) { }
        Console.WriteLine("7 comprobaciones de inserción de oraciones y errores correctas.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class InsertExecutor : IStoredProcedureExecutor
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime Timestamp { get; } = new(2026, 10, 20, 12, 0, 0);
        public int Calls { get; private set; }
        public bool ReturnNoRows { get; set; }
        public string? Procedure { get; private set; }
        public IReadOnlyList<StoredProcedureParameter> Parameters { get; private set; } = [];

        public Task<ResourceResponse<DataTable>> ExecuteStoredProcedureAsync(string schema, string procedureName,
            IReadOnlyList<StoredProcedureParameter> parameters, StoredProcedureResult? result = null)
        {
            Check(schema == "grammar" && result is not null, "Usa el esquema grammar y resultado compuesto.");
            Calls++;
            Procedure = procedureName;
            Parameters = parameters;
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("CreatedAt", typeof(DateTime));
            table.Columns.Add("UpdatedAt", typeof(DateTime));
            foreach (var parameter in parameters.Where(p => p.Direction == ParameterDirection.Input))
                table.Columns.Add(parameter.Name, parameter.Type switch
                {
                    NpgsqlDbType.Uuid => typeof(Guid), NpgsqlDbType.Integer => typeof(int),
                    NpgsqlDbType.Numeric => typeof(decimal), NpgsqlDbType.Boolean => typeof(bool),
                    _ => typeof(string)
                });
            if (!ReturnNoRows)
            {
                var row = table.NewRow();
                row["Id"] = Id;
                row["CreatedAt"] = Timestamp;
                row["UpdatedAt"] = Timestamp;
                foreach (var parameter in parameters.Where(p => p.Direction == ParameterDirection.Input))
                    row[parameter.Name] = parameter.Value ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return Task.FromResult(ResourceResponse<DataTable>.Ok(table));
        }
    }
}
