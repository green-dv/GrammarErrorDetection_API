using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace GrammarErrorDetection.API.Controllers;

public sealed class AchievementFormRequest
{
    [Required]
    public string Data { get; set; } = string.Empty;
    public IFormFile? File { get; set; }

    public T Parse<T>() where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(Data, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new ValidationException("El campo data debe contener el objeto JSON del logro.");
        }
        catch (JsonException)
        {
            throw new ValidationException("El campo data debe contener un JSON válido con los datos del logro.");
        }
    }
}
