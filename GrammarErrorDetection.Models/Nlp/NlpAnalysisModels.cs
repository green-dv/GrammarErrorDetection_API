using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrammarErrorDetection.Models.Nlp;

public sealed class NlpSentenceRequest
{
    [JsonPropertyName("sentence_id")]
    public string SentenceId { get; set; } = string.Empty;
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public sealed class NlpSentenceResult
{
    [JsonRequired, JsonPropertyName("sentence_id")]
    public string SentenceId { get; set; } = string.Empty;
    [JsonRequired, JsonPropertyName("errors")]
    public List<NlpError> Errors { get; set; } = [];
}

public sealed class NlpError
{
    [JsonRequired, JsonPropertyName("error_token_start_index")]
    public int StartIndex { get; set; }
    [JsonRequired, JsonPropertyName("error_token_end_index")]
    public int EndIndex { get; set; }
    [JsonPropertyName("suggested_correction")]
    public string? SuggestedCorrection { get; set; }
    [JsonPropertyName("confidence_score")]
    public decimal? ConfidenceScore { get; set; }
    [JsonRequired, JsonPropertyName("error_classification_name")]
    public string ClassificationName { get; set; } = string.Empty;
}

public sealed class NlpValidationResponse
{
    [JsonPropertyName("detail")]
    public List<NlpValidationDetail>? Detail { get; set; }
}

public sealed class NlpValidationDetail
{
    [JsonPropertyName("loc")]
    public List<JsonElement> Location { get; set; } = [];
    [JsonPropertyName("msg")]
    public string Message { get; set; } = string.Empty;
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
