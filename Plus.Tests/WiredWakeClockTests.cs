using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Xunit;

namespace Plus.Tests;

[CollectionDefinition("Wired wake timing", DisableParallelization = true)]
public class WiredWakeTimingCollection;

[Collection("Wired wake timing")]
public class WiredWakeClockTests
{
    [Fact]
    public void DelayedWakeUpsLandOnTheirOwnDueTimeNotOnTheNextRoomStep()
    {
        var time = new ManualTime();
        using var clock = new WiredWakeClock(time);
        var step = (long)RoomCycle.WiredInterval.TotalMilliseconds;
        // The due times fall between room steps, and two are 1 ms apart, so a clock that waits for a step or fires early lands on the wrong ms.
        long[] due = [10 * step + 13, step + 29, 5 * step + 41, 5 * step + 42];
        var woke = new long[due.Length];
        var order = new List<int>();
        var fired = due.Select(_ => new ManualResetEventSlim()).ToArray();

        for (var i = 0; i < due.Length; i++) {
            var slot = i;
            clock.At(due[slot], () =>
            {
                woke[slot] = time.GetTimestamp();

                lock (order) {
                    order.Add(slot);
                }

                fired[slot].Set();
            });
        }

        foreach (var slot in Enumerable.Range(0, due.Length).OrderBy(slot => due[slot])) {
            time.Set(due[slot]);
            Assert.True(fired[slot].Wait(TimeSpan.FromSeconds(10)));
        }

        Assert.Equal([1, 2, 3, 0], order);
        Assert.Equal(due, woke);
    }

    [Fact]
    public void AWakeUpThatThrowsDoesNotStopTheClock()
    {
        using var clock = new WiredWakeClock();
        using var woke = new ManualResetEventSlim();

        clock.After(5, () => throw new InvalidOperationException());
        clock.After(10, woke.Set);

        Assert.True(woke.Wait(TimeSpan.FromSeconds(2)));
    }

    private sealed class ManualTime : TimeProvider
    {
        private long _ms;

        public override long TimestampFrequency => 1000;

        public override long GetTimestamp() => Interlocked.Read(ref _ms);

        public void Set(long ms) => Interlocked.Exchange(ref _ms, ms);
    }
}
