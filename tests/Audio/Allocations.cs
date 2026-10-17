namespace Micser.Audio.Tests;

internal static class Allocations
{
    /// <summary>
    /// Returns what <paramref name="run"/> allocates, by default on the current thread. A GC during the run (e.g. one that tests running
    /// in parallel trigger) can make the counters show a few KB that weren't allocated, so a run with a GC that seemed to allocate is
    /// repeated, up to 5 times; a real allocation shows in the runs without a GC too.
    /// </summary>
    public static long Measure(Action run, Func<long>? counter = null)
    {
        counter ??= GC.GetAllocatedBytesForCurrentThread;
        long allocated = 0;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var collections = GC.CollectionCount(0);
            var before = counter();
            run();
            allocated = counter() - before;
            if (allocated == 0 || GC.CollectionCount(0) == collections)
            {
                break;
            }
        }

        return allocated;
    }
}
