namespace GrammarErrorDetection.Repositories.Utils;

internal static class DatabaseDateTime
{
    private static readonly TimeZoneInfo DatabaseZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");

    internal static DateTime? ToTimestamp(DateTime? value)
    {
        if (value is null)
            return null;

        // Las fechas sin zona ya representan la hora de la base.
        if (value.Value.Kind == DateTimeKind.Unspecified)
            return value;

        var utc = value.Value.ToUniversalTime();
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, DatabaseZone);
        return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
    }
}
