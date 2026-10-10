using System;
using VillageClash.Core;

namespace VillageClash
{
    public sealed class SystemClock : IClock
    {
        public long NowUnix => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
