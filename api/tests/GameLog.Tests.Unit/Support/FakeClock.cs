namespace GameLog.Tests.Unit.Support;

/// <summary>Deterministic time for lockout, timestamps and "today" checks.</summary>
public sealed class FakeClock(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset now = start;

    public override DateTimeOffset GetUtcNow() => now;

    public void Advance(TimeSpan by) => now += by;
}
