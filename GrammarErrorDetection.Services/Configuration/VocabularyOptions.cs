namespace GrammarErrorDetection.Services.Configuration;

/// <summary>Configuración de la verificación de vocabulario (sección "Vocabulary" de appsettings).</summary>
public sealed class VocabularyOptions
{
    /// <summary>API externa gratuita Datamuse. Desde el 1-feb-2027 exigirá clave; ver README.</summary>
    public string BaseUrl { get; set; } = "https://api.datamuse.com/";

    /// <summary>Alternativas máximas por palabra con error.</summary>
    public int MaxAlternatives { get; set; } = 3;

    /// <summary>Definiciones máximas por alternativa.</summary>
    public int MaxDefinitions { get; set; } = 3;

    /// <summary>Frecuencia mínima (por millón de palabras) de una alternativa: descarta palabras rarísimas.</summary>
    public double MinAlternativeFrequency { get; set; } = 1.0;

    /// <summary>Palabras nuevas que una sola consulta puede verificar; el resto queda para la siguiente. Cuida la cuota diaria.</summary>
    public int MaxNewLookupsPerRequest { get; set; } = 150;

    /// <summary>Consultas simultáneas a la API externa por solicitud.</summary>
    public int MaxConcurrency { get; set; } = 4;

    /// <summary>Días que una palabra verificada se considera vigente en la caché.</summary>
    public int CacheDays { get; set; } = 180;
}
