namespace VillageClash.Core
{
    public interface IClock { long NowUnix { get; } }

    /// <summary>Test clock whose time is set explicitly.</summary>
    public sealed class FixedClock : IClock
    {
        public long NowUnix { get; set; }
        public FixedClock(long now = 1_700_000_000) { NowUnix = now; }
        public void Advance(long seconds) { NowUnix += seconds; }
    }
}
