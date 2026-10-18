using System.Text;
using Microsoft.Extensions.Configuration;

namespace GrammarErrorDetection.Services.Configuration;

public sealed class JwtOptions
{
    public string Secret { get; }
    public string Issuer { get; }
    public string Audience { get; }
    public int ExpirationMinutes { get; }

    public JwtOptions(string secret, string issuer, string audience, int expirationMinutes)
    {
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("JWT_SECRET debe contener al menos 32 bytes.");
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("JWT_ISSUER y JWT_AUDIENCE son obligatorios.");
        if (expirationMinutes <= 0)
            throw new InvalidOperationException("JWT_EXPIRATION_MINUTES debe ser un entero positivo.");

        Secret = secret;
        Issuer = issuer;
        Audience = audience;
        ExpirationMinutes = expirationMinutes;
    }

    public static JwtOptions FromConfiguration(IConfiguration configuration)
    {
        if (!int.TryParse(configuration["JWT_EXPIRATION_MINUTES"], out var minutes))
            throw new InvalidOperationException("JWT_EXPIRATION_MINUTES debe ser un entero positivo.");

        return new JwtOptions(configuration["JWT_SECRET"] ?? "",
            configuration["JWT_ISSUER"] ?? "", configuration["JWT_AUDIENCE"] ?? "", minutes);
    }
}
