using GrammarErrorDetection.API.Middleware;
using GrammarErrorDetection.Infrastructure.Database;
using GrammarErrorDetection.Infrastructure.Interfaces;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Repositories;
using GrammarErrorDetection.Services.Interfaces;
using GrammarErrorDetection.Services.Services;
using GrammarErrorDetection.Services.Configuration;
using Npgsql;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");

if (!File.Exists(envPath))
{
    envPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));
}

if (File.Exists(envPath))
{
    foreach (var line in File.ReadLines(envPath))
    {
        var text = line.Trim();

        if (string.IsNullOrWhiteSpace(text) || text.StartsWith('#'))
            continue;

        var parts = text.Split('=', 2);

        if (parts.Length != 2)
            continue;

        var name = parts[0].Trim();
        var value = parts[1].Trim().Trim('"', '\'');

        // Conserva las variables que ya existen en el entorno.
        if (Environment.GetEnvironmentVariable(name) is null)
            Environment.SetEnvironmentVariable(name, value);
    }
}

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("LocalDevelopment", policy => policy
            .WithOrigins("http://localhost:7178", "https://localhost:7178")
            .AllowAnyHeader()
            .AllowAnyMethod());
    });
}

var jwtOptions = JwtOptions.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
});

var expirationSetting = builder.Configuration[VerificationCodeOptions.EnvironmentVariable] ?? "10";
if (!int.TryParse(expirationSetting, out var expirationMinutes) || expirationMinutes < 1)
{
    throw new InvalidOperationException(
        $"{VerificationCodeOptions.EnvironmentVariable} debe ser un entero positivo en minutos.");
}
builder.Services.AddSingleton(new VerificationCodeOptions(expirationMinutes));
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new NlpOptions { Endpoint = configuration["NLP_API_URL"] ?? configuration["Nlp:Endpoint"] ?? string.Empty };
});
builder.Services.AddHttpClient<INlpService, NlpService>(client => client.Timeout = TimeSpan.FromSeconds(60));

builder.Services.AddSingleton<NpgsqlDataSource>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();

    string Required(string name)
    {
        var value = configuration[name];

        return !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException($"Falta configurar {name} en el entorno o en .env.");
    }

    if (!int.TryParse(Required("DB_PORT"), out var port) ||
        port is < 1 or > 65535)
    {
        throw new InvalidOperationException("DB_PORT no es válido.");
    }

    var connection = new NpgsqlConnectionStringBuilder
    {
        Host = Required("DB_HOST"),
        Port = port,
        Database = Required("DB_NAME"),
        Username = Required("DB_USER"),
        Password = Required("DB_PASSWORD")
    };

    return NpgsqlDataSource.Create(connection.ConnectionString);
});

builder.Services.AddScoped<IStoredProcedureExecutor, StoredProcedureExecutor>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserLeaderboardRepository, UserLeaderboardRepository>();
builder.Services.AddScoped<IModuleNameRepository, ModuleNameRepository>();
builder.Services.AddScoped<IClassroomRepository, ClassroomRepository>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
builder.Services.AddScoped<IRegistrationRepository, RegistrationRepository>();
builder.Services.AddScoped<IEvaluationRepository, EvaluationRepository>();
builder.Services.AddScoped<ITextRepository, TextRepository>();
builder.Services.AddScoped<ISentenceRepository, SentenceRepository>();
builder.Services.AddScoped<ISentenceErrorRepository, SentenceErrorRepository>();
builder.Services.AddScoped<IErrorClassificationRepository, ErrorClassificationRepository>();
builder.Services.AddScoped<IFileTypeRepository, FileTypeRepository>();
builder.Services.AddScoped<IErrorClassificationService, ErrorClassificationService>();
builder.Services.AddScoped<IFileTypeService, FileTypeService>();
builder.Services.AddScoped<ISupportMaterialRepository, SupportMaterialRepository>();
builder.Services.AddScoped<IPointTransactionRepository, PointTransactionRepository>();
builder.Services.AddScoped<IUserLevelRepository, UserLevelRepository>();
builder.Services.AddScoped<IUserLevelService, UserLevelService>();
builder.Services.AddScoped<IPointTransactionService, PointTransactionService>();
builder.Services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
builder.Services.AddScoped<IAuthChallengeRepository, AuthChallengeRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserLeaderboardService, UserLeaderboardService>();
builder.Services.AddScoped<IModuleNameService, ModuleNameService>();
builder.Services.AddScoped<IClassroomService, ClassroomService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IEvaluationService, EvaluationService>();
builder.Services.AddScoped<ITextService, TextService>();
builder.Services.AddScoped<ISupportMaterialService, SupportMaterialService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVerificationCodeService, VerificationCodeService>();
builder.Services.AddScoped<IRoleService, RoleService>();

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = ExceptionHandlerMiddleware.CreateValidationErrorResponse;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GrammarErrorDetection API",
        Version = "v1"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Introduce solo el JWT de acceso, sin el prefijo Bearer."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
    options.OperationFilter<GrammarErrorDetection.API.Swagger.AnonymousSecurityOperationFilter>();
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlerMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("v1/swagger.json", "GrammarErrorDetection API v1");
    });
}

app.UseHttpsRedirection();

app.UseRouting();
if (app.Environment.IsDevelopment())
{
    app.UseCors("LocalDevelopment");
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();

