using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using GrammarErrorDetection.Models.APIRequest;
using GrammarErrorDetection.Models.Grammar.VerificationCodes;
using GrammarErrorDetection.Repositories.Interfaces;
using GrammarErrorDetection.Repositories.Utils;
using GrammarErrorDetection.Services.Configuration;
using GrammarErrorDetection.Services.Services;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

var utc = new DateTime(2026, 10, 17, 2, 0, 0, DateTimeKind.Utc);
var bolivia = new DateTime(2026, 10, 16, 22, 0, 0, DateTimeKind.Unspecified);
var timestamp = DatabaseDateTime.ToTimestamp(utc);
Check(timestamp == bolivia && timestamp.Value.Kind == DateTimeKind.Unspecified,
    "UTC must convert to Bolivia time, including a change of date.");
Check(DatabaseDateTime.ToTimestamp(null) is null, "NULL must remain NULL.");
Check(DatabaseDateTime.ToTimestamp(bolivia) == bolivia,
    "Unspecified database timestamps must retain their value.");
Check(DatabaseDateTime.ToTimestamp(utc.ToLocalTime()) == bolivia,
    "Local DateTime must convert to the same instant in Bolivia.");

var request = JsonSerializer.Deserialize<VerificationCodeInsert>(
    """{"CodeHash":"test-hash","Purpose":"test","ExpirationTime":"2000-01-01T00:00:00Z"}""")!;
Check(request.ExpirationTime is null, "Clients must not control insertion expiration.");
Validator.ValidateObject(request, new ValidationContext(request), true);

var repository = new CaptureRepository();
var service = new VerificationCodeService(repository, new VerificationCodeOptions(23), new FixedClock(utc));
await service.Insert(request);
Check(repository.Inserted?.ExpirationTime == utc.AddMinutes(23),
    "Expiration must use the configured duration and clock.");
Check(repository.Inserted?.ExpirationTime?.Kind == DateTimeKind.Utc,
    "The service must calculate expiration in UTC before repository conversion.");

var update = new VerificationCodeUpdate { Id = Guid.NewGuid(), ConsumedAt = utc };
await service.Update(update);
Check(ReferenceEquals(repository.Updated, update) && update.ExpirationTime is null,
    "Updating ConsumedAt must not regenerate expiration.");

foreach (var invalid in new[] { 0, -1 })
{
    try
    {
        _ = new VerificationCodeOptions(invalid);
        throw new Exception("Nonpositive duration was accepted.");
    }
    catch (ArgumentOutOfRangeException) { }
}

Console.WriteLine("Verification code checks passed: UTC/local/unspecified/null conversion, configured expiration, JSON validation, partial update, invalid duration.");

sealed class FixedClock(DateTime utc) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(utc);
}

sealed class CaptureRepository : IVerificationCodeRepository
{
    public VerificationCodeInsert? Inserted { get; private set; }
    public VerificationCodeUpdate? Updated { get; private set; }

    public Task<List<VerificationCodeEntity>> List(PaginationDto<VerificationCodeList> request)
        => Task.FromResult(new List<VerificationCodeEntity>());

    public Task<VerificationCodeEntity> Insert(VerificationCodeInsert request)
    {
        Inserted = request;
        return Task.FromResult(new VerificationCodeEntity { ExpirationTime = request.ExpirationTime!.Value });
    }

    public Task<VerificationCodeEntity> Update(VerificationCodeUpdate request)
    {
        Updated = request;
        return Task.FromResult(new VerificationCodeEntity { Id = request.Id });
    }
}
