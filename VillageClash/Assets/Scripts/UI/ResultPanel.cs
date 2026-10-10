using System;
using UnityEngine;
using VillageClash.Core;

namespace VillageClash.UI
{
    public static class ResultPanel
    {
        public static void Show(BattleResult r, Action onHome)
        {
            string title = r == null ? "Battle over" : r.Practice ? (r.Won ? "Practice complete!" : "Practice over") : r.Won ? "Victory!" : "Defeat";
            var m = UIKit.ShowModal(title, new Vector2(820, 640), null, false, false);
            var c = m.Content;
            if (r == null) { Done(m, onHome); return; }

            for (int i = 0; i < 3; i++)
            {
                var ic = UIKit.Icon(c, i < r.Stars ? "star" : "star_empty", i == 1 ? 120 : 96);
                UIKit.Place((RectTransform)ic.transform, UIKit.TC, new Vector2((i - 1) * 130, i == 1 ? -10 : -24), new Vector2(i == 1 ? 120 : 96, i == 1 ? 120 : 96));
            }
            var pct = UIKit.Dark(c, $"{r.Percent}% destruction" + (r.TownHallDown ? "  -  Town Hall destroyed" : ""), 28, TextAnchor.MiddleCenter);
            UIKit.Place((RectTransform)pct.transform, UIKit.TC, new Vector2(0, -140), new Vector2(760, 40));
            var who = UIKit.Dark(c, $"vs {r.EnemyName}   {Fmt.Clock(r.Duration)}" + (r.Surrendered ? "   (surrendered)" : ""), 22, TextAnchor.MiddleCenter, false);
            UIKit.Place((RectTransform)who.transform, UIKit.TC, new Vector2(0, -180), new Vector2(760, 30));

            var box = UIKit.Node("loot", c);
            UIKit.Place(box, UIKit.TC, new Vector2(0, -220), new Vector2(620, 190));
            UIKit.Panel(box, Palette.CreamDark, true, false);
            Row(box, "gold", $"+{Fmt.Num(r.Gold)} gold", 16);
            Row(box, "elixir", $"+{Fmt.Num(r.Elixir)} elixir", 72);
            Row(box, "trophy", r.Practice ? "No trophies (practice)" : $"{Fmt.Signed(r.TrophyDelta)} trophies", 128);

            string used = "Troops used: ";
            bool any = false;
            for (int i = 0; i < r.TroopsUsed.Length; i++) if (r.TroopsUsed[i] > 0) { used += $"{r.TroopsUsed[i]} {GameData.Troop((TroopType)i).Name}  "; any = true; }
            var u = UIKit.Dark(c, any ? used : "No troops were deployed.", 22, TextAnchor.MiddleCenter, false);
            UIKit.Place((RectTransform)u.transform, UIKit.TC, new Vector2(0, -424), new Vector2(760, 30));
            Done(m, onHome);
        }

        static void Row(RectTransform parent, string icon, string text, float y)
        {
            var ic = UIKit.Icon(parent, icon, 40);
            UIKit.Place((RectTransform)ic.transform, UIKit.TL, new Vector2(30, -y), new Vector2(40, 40));
            var t = UIKit.Dark(parent, text, 26, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)t.transform, UIKit.TL, new Vector2(90, -y), new Vector2(500, 40));
        }

        static void Done(Modal m, Action onHome)
        {
            var b = UIKit.Btn(m.Content, "Return Home", Palette.Green, new Vector2(300, 74), () => { m.Close(); onHome?.Invoke(); }, 28, "castle");
            UIKit.Place(b.Rt, UIKit.BC, new Vector2(0, 14), new Vector2(300, 74));
        }
    }
}
