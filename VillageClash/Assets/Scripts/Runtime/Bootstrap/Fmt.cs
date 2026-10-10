using System.Globalization;

namespace VillageClash
{
    public static class Fmt
    {
        public static string Num(int n) => n.ToString("N0", CultureInfo.InvariantCulture);

        public static string Time(long s)
        {
            if (s <= 0) return "0s";
            long d = s / 86400, h = s % 86400 / 3600, m = s % 3600 / 60, sec = s % 60;
            if (d > 0) return h > 0 ? $"{d}d {h}h" : $"{d}d";
            if (h > 0) return m > 0 ? $"{h}h {m}m" : $"{h}h";
            if (m > 0) return sec > 0 ? $"{m}m {sec}s" : $"{m}m";
            return $"{sec}s";
        }

        public static string Clock(float seconds)
        {
            int s = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.CeilToInt(seconds));
            return $"{s / 60}:{s % 60:00}";
        }

        public static string Signed(int n) => n >= 0 ? "+" + Num(n) : "-" + Num(-n);
    }
}
