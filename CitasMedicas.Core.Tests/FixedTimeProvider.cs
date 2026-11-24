namespace CitasMedicas.Core.Tests;

public sealed class FixedTimeProvider : TimeProvider
{
    private DateTime _now;

    public FixedTimeProvider(DateTime now) => _now = now;

    public void Set(DateTime now) => _now = now;

    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(_now, DateTimeKind.Utc));

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
