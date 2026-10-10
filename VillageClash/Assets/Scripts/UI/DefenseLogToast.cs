using UnityEngine;
using VillageClash.Core;

namespace VillageClash.UI
{
    /// <summary>Shows the "your village was attacked" report on login and the defense log from the mail button.</summary>
    public static class DefenseLogToast
    {
        public static void ShowPending()
        {
            var c = GameRoot.I.Controller;
            var r = c.PendingDefenseReport;
            if (r == null) return;
            var m = UIKit.ShowModal("Your village was attacked!", new Vector2(760, 460), () => c.ClearPendingDefenseReport());
            Report(m.Content, r, c.Now, 0, 700);
            var ok = UIKit.Btn(m.Content, "Okay", Palette.Green, new Vector2(240, 70), () => m.Close(), 26);
            UIKit.Place(ok.Rt, UIKit.BC, new Vector2(0, 16), new Vector2(240, 70));
            Sfx.Play(r.Stars >= 2 ? SfxId.Lose : SfxId.Hit);
        }

        public static void ShowLog()
        {
            var c = GameRoot.I.Controller;
            var m = UIKit.ShowModal("Defense Log", new Vector2(900, 760), () => { foreach (var r in c.State.DefenseLog) r.Seen = true; c.ClearPendingDefenseReport(); c.Save(); GameEvents.RaiseStateChanged(); });
            if (c.State.DefenseLog.Count == 0)
            {
                var l = UIKit.Dark(m.Content, "No defense reports yet.\n\nWhile you are away, other chiefs may raid your village.\nBuild defenses and keep a shield up to protect your loot!", 26, TextAnchor.MiddleCenter, false);
                UIKit.Stretch((RectTransform)l.transform, 40, 40, 40, 40);
                return;
            }
            var list = UIKit.Scroll(m.Content, out _);
            UIKit.VList(list, 12, 6);
            UIKit.FitContent(list);
            foreach (var r in c.State.DefenseLog)
            {
                var row = UIKit.Node("report", list);
                UIKit.Size(row, 820, 150);
                UIKit.Panel(row, r.Seen ? Palette.CreamDark : Palette.Hex("f5e3a0"), true, false);
                Report(row, r, c.Now, 0, 820);
            }
        }

        static void Report(RectTransform parent, DefenseReport r, long now, float y, float w)
        {
            string ago = Fmt.Time((int)System.Math.Max(0, now - r.AtUnix)) + " ago";
            var title = UIKit.Dark(parent, $"{r.Attacker} attacked you  -  {ago}", 26, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)title.transform, UIKit.TL, new Vector2(20, -8 + y), new Vector2(w - 40, 40));
            for (int i = 0; i < 3; i++)
            {
                var st = UIKit.Icon(parent, i < r.Stars ? "star" : "star_empty", 40);
                UIKit.Place((RectTransform)st.transform, UIKit.TL, new Vector2(20 + i * 44, -52 + y), new Vector2(40, 40));
            }
            var pct = UIKit.Dark(parent, $"{r.Percent}% destroyed", 24, TextAnchor.MiddleLeft, false);
            UIKit.Place((RectTransform)pct.transform, UIKit.TL, new Vector2(170, -52 + y), new Vector2(300, 40));
            var g = UIKit.Icon(parent, "gold", 32); UIKit.Place((RectTransform)g.transform, UIKit.TL, new Vector2(20, -100 + y), new Vector2(32, 32));
            var gt = UIKit.Dark(parent, $"-{Fmt.Num(r.GoldLost)}", 24, TextAnchor.MiddleLeft, false); UIKit.Place((RectTransform)gt.transform, UIKit.TL, new Vector2(60, -100 + y), new Vector2(160, 36));
            var e = UIKit.Icon(parent, "elixir", 32); UIKit.Place((RectTransform)e.transform, UIKit.TL, new Vector2(230, -100 + y), new Vector2(32, 32));
            var et = UIKit.Dark(parent, $"-{Fmt.Num(r.ElixirLost)}", 24, TextAnchor.MiddleLeft, false); UIKit.Place((RectTransform)et.transform, UIKit.TL, new Vector2(270, -100 + y), new Vector2(160, 36));
            var t = UIKit.Icon(parent, "trophy", 32); UIKit.Place((RectTransform)t.transform, UIKit.TL, new Vector2(440, -100 + y), new Vector2(32, 32));
            var tt = UIKit.Dark(parent, Fmt.Signed(r.TrophyDelta), 24, TextAnchor.MiddleLeft, false); UIKit.Place((RectTransform)tt.transform, UIKit.TL, new Vector2(480, -100 + y), new Vector2(160, 36));
            if (r.Percent >= 30)
            {
                var sh = UIKit.Dark(parent, "Shield granted", 22, TextAnchor.MiddleRight, false);
                UIKit.Place((RectTransform)sh.transform, UIKit.TR, new Vector2(-20, -100 + y), new Vector2(240, 36));
            }
        }
    }
}
