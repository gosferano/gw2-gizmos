namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>Pacing a CPU-bound job to about half a core: a pause as long as each slice of work.</summary>
public sealed class CpuPacerTests
{
    [Fact]
    public void Pauses_as_long_as_each_whole_slice_of_work_took()
    {
        var clock = new SteppedClock();
        var waits = new List<TimeSpan>();
        var pacer = new CpuPacer(clock, (wait, _) =>
        {
            waits.Add(wait);
            clock.Advance(wait);
        });

        clock.Advance(TimeSpan.FromMilliseconds(30));
        pacer.PauseIfDue(CancellationToken.None); // not a whole slice yet
        clock.Advance(TimeSpan.FromMilliseconds(40));
        pacer.PauseIfDue(CancellationToken.None); // 70 ms of work
        clock.Advance(TimeSpan.FromMilliseconds(20));
        pacer.PauseIfDue(CancellationToken.None); // a new slice, counted from after the pause

        Assert.Equal([TimeSpan.FromMilliseconds(70)], waits);
    }

    // A clock that only moves when told to.
    private sealed class SteppedClock : TimeProvider
    {
        private long _ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _ticks;

        public void Advance(TimeSpan by) => _ticks += by.Ticks;
    }
}
