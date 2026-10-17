namespace Micser.Audio.Tests;

/// <param name="Bytes">The bytes allocated in the last run.</param>
/// <param name="Collections">The GCs during the last run.</param>
internal readonly record struct Allocation(long Bytes, int Collections)
{
    public override string ToString()
    {
        return $"{Bytes} B with {Collections} GCs during the run";
    }
}

internal static class Allocations
{
    /// <summary>
    /// Returns what <paramref name="run"/> allocates, by default on the current thread. Tests that measure run alone
    /// (<c>[NotInParallel]</c>): a GC during the run, e.g. one that other tests trigger, can make the counters show a few KB that weren't
    /// allocated. Each run starts after a GC, and a run with a GC that seemed to allocate is repeated, up to 5 times; a real allocation
    /// shows in the runs without a GC too.
    /// </summary>
    public static Allocation Measure(Action run, Func<long>? counter = null)
    {
        counter ??= GC.GetAllocatedBytesForCurrentThread;
        var result = default(Allocation);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var collections = GC.CollectionCount(0);
            var before = counter();
            run();
            result = new Allocation(counter() - before, GC.CollectionCount(0) - collections);
            if (result.Bytes == 0 || result.Collections == 0)
            {
                break;
            }
        }

        return result;
    }
}
