using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VillageClash.Core;

namespace VillageClash.UI
{
    /// <summary>In-battle overlay: loot available, timer, damage/stars, troop bar, end battle.</summary>
    public sealed class BattleHud : MonoBehaviour
    {
        BattleController battle;
        RectTransform root;
        Text enemyName, timer, gold, elixir, trophies, percent, hint;
        Image[] stars = new Image[3];
        BtnView endBtn;
        RectTransform troopRow;
        readonly List<(TroopType type, BtnView btn, Text count)> slots = new List<(TroopType, BtnView, Text)>();
        RectTransform lootRow;

        public static BattleHud Create(BattleController battle)
        {
            var rt = UIKit.Node("BattleHud", UIKit.HudLayer);
            UIKit.Stretch(rt);
            var h = rt.gameObject.AddComponent<BattleHud>();
            h.battle = battle; h.root = rt;
            h.Build();
            rt.gameObject.SetActive(false);
            battle.Changed += h.RebuildTroops;
            return h;
        }

        void Build()
        {
            // top-left: enemy + loot
            var loot = UIKit.Node("loot", root);
            UIKit.Place(loot, UIKit.TL, new Vector2(20, -20), new Vector2(360, 190));
            UIKit.Panel(loot, Palette.Alpha(Palette.Black, 0.45f), true, false);
            enemyName = UIKit.Label(loot, "", 26, Color.white, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)enemyName.transform, UIKit.TL, new Vector2(14, -6), new Vector2(340, 36));
            var sub = UIKit.Label(loot, "Available loot:", 20, Palette.Grey, TextAnchor.MiddleLeft, false, false);
            UIKit.Place((RectTransform)sub.transform, UIKit.TL, new Vector2(14, -42), new Vector2(300, 26));
            gold = Row(loot, "gold", -70);
            elixir = Row(loot, "elixir", -110);
            trophies = Row(loot, "trophy", -150);

            // top-center timer
            var tc = UIKit.Node("timer", root);
            UIKit.Place(tc, UIKit.TC, new Vector2(0, -20), new Vector2(520, 60));
            UIKit.Panel(tc, Palette.Alpha(Palette.Black, 0.45f), true, false);
            timer = UIKit.Label(tc, "", 30, Color.white);
            UIKit.Stretch((RectTransform)timer.transform);

            // top-right: overall damage
            var dmg = UIKit.Node("damage", root);
            UIKit.Place(dmg, UIKit.TR, new Vector2(-20, -20), new Vector2(360, 130));
            UIKit.Panel(dmg, Palette.Alpha(Palette.Black, 0.45f), true, false);
            var dl = UIKit.Label(dmg, "Overall Damage", 22, Palette.Grey, TextAnchor.MiddleCenter, false, false);
            UIKit.Place((RectTransform)dl.transform, UIKit.TC, new Vector2(0, -6), new Vector2(340, 28));
            percent = UIKit.Label(dmg, "0%", 40, Color.white);
            UIKit.Place((RectTransform)percent.transform, UIKit.TC, new Vector2(-90, -50), new Vector2(160, 60));
            for (int i = 0; i < 3; i++)
            {
                stars[i] = UIKit.Icon(dmg, "star_empty", 52);
                UIKit.Place((RectTransform)stars[i].transform, UIKit.TC, new Vector2(30 + i * 56, -50 + (i == 1 ? 6 : 0)), new Vector2(52, 52));
            }

            // hint
            hint = UIKit.Label(root, "", 24, Color.white);
            UIKit.Place((RectTransform)hint.transform, UIKit.BC, new Vector2(0, 170), new Vector2(900, 36));

            // bottom troop bar
            var bar = UIKit.Node("troopbar", root);
            UIKit.Place(bar, UIKit.BC, new Vector2(0, 16), new Vector2(1500, 150));
            UIKit.Panel(bar, Palette.Alpha(Palette.Black, 0.5f), true, false);
            troopRow = UIKit.Node("row", bar);
            UIKit.Place(troopRow, UIKit.ML, new Vector2(16, 0), new Vector2(1180, 140));
            UIKit.HList(troopRow, 12, 0, TextAnchor.MiddleLeft);
            endBtn = UIKit.Btn(bar, "End Battle", Palette.Red, new Vector2(240, 80), () => battle.EndBattle(), 28);
            UIKit.Place(endBtn.Rt, UIKit.MR, new Vector2(-20, 0), new Vector2(240, 80));
        }

        Text Row(RectTransform parent, string icon, float y)
        {
            var ic = UIKit.Icon(parent, icon, 34);
            UIKit.Place((RectTransform)ic.transform, UIKit.TL, new Vector2(16, y), new Vector2(34, 34));
            var t = UIKit.Label(parent, "", 26, Color.white, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)t.transform, UIKit.TL, new Vector2(60, y), new Vector2(280, 36));
            return t;
        }

        public void Show()
        {
            root.gameObject.SetActive(true);
            RebuildTroops();
            Refresh();
        }

        public void Hide() => root.gameObject.SetActive(false);

        void RebuildTroops()
        {
            if (!root.gameObject.activeSelf || battle.Sim == null) return;
            foreach (Transform t in troopRow) Destroy(t.gameObject);
            slots.Clear();
            var sim = battle.Sim;
            int filled = 0;
            for (int i = 0; i < sim.Remaining.Length; i++)
            {
                if (sim.Remaining[i] + sim.Deployed[i] == 0) continue;
                var type = (TroopType)i;
                var def = GameData.Troop(type);
                bool left = sim.Remaining[i] > 0;
                bool selected = type == battle.SelectedTroop && left;
                var card = UIKit.Btn(troopRow, "", selected ? Palette.Green : left ? Palette.GreyDark : Palette.Alpha(Palette.GreyDark, 0.45f), new Vector2(130, 130), () => battle.Select(type), 20);
                UIKit.Size(card.Rt, 130, 130);
                var pic = UIKit.Sprite(card.Rt, SpriteFactory.Troop(type), 70, 88);
                UIKit.Place((RectTransform)pic.transform, UIKit.MC, new Vector2(0, 6), new Vector2(70, 88));
                if (!left) pic.color = new Color(1, 1, 1, 0.35f);
                var nm = UIKit.Label(card.Rt, def.Name, 18, Color.white, TextAnchor.MiddleCenter, true, false);
                UIKit.Place((RectTransform)nm.transform, UIKit.BC, new Vector2(0, 2), new Vector2(126, 24));
                var cnt = UIKit.Label(card.Rt, $"x{sim.Remaining[i]}", 26, left ? Palette.Gold : Palette.Grey, TextAnchor.MiddleRight);
                UIKit.Place((RectTransform)cnt.transform, UIKit.TR, new Vector2(-6, -2), new Vector2(80, 30));
                slots.Add((type, card, cnt));
                filled++;
            }
            // dashed empty slots, like the reference troop bar
            for (int i = filled; i < 5; i++)
            {
                var slot = UIKit.Node("empty", troopRow);
                UIKit.Size(slot, 130, 130);
                var f = UIKit.Frame(slot, Palette.Alpha(Palette.Black, 0.2f), 3);
                UIKit.Stretch(f);
                var dash = UIKit.Label(slot, "- - -", 24, Palette.Alpha(Color.white, 0.35f), TextAnchor.MiddleCenter, false, false);
                UIKit.Stretch((RectTransform)dash.transform);
            }
        }

        void Update() { if (root.gameObject.activeSelf) Refresh(); }

        void Refresh()
        {
            var sim = battle.Sim;
            if (sim == null) return;
            var e = sim.Enemy;
            enemyName.text = e.Practice ? $"{e.Name}  (Practice)" : $"{e.Name}  -  TH {e.TownHallLevel}";
            gold.text = Fmt.Num(sim.AvailableGold - sim.LootGold) + (sim.LootGold > 0 ? $"   (+{Fmt.Num(sim.LootGold)})" : "");
            elixir.text = Fmt.Num(sim.AvailableElixir - sim.LootElixir) + (sim.LootElixir > 0 ? $"   (+{Fmt.Num(sim.LootElixir)})" : "");
            trophies.text = e.Practice ? "No trophies (practice)" : $"+{Progression.TrophyDelta(3)} / {Progression.TrophyDelta(0)}";
            if (sim.Ended) timer.text = sim.Surrendered ? "Battle over" : sim.Percent >= 100 ? "Total destruction!" : "Battle ended";
            else if (!sim.Started) timer.text = "Tap the field to deploy troops";
            else timer.text = Fmt.Clock(sim.TimeLeft);
            percent.text = sim.Percent + "%";
            int st = sim.Stars;
            for (int i = 0; i < 3; i++) stars[i].sprite = SpriteFactory.Icon(i < st ? "star" : "star_empty");
            endBtn.SetText(sim.Ended ? "..." : !sim.Started ? (e.Practice ? "Exit Practice" : "Leave") : "Surrender");
            endBtn.SetEnabled(!sim.Ended);
            hint.text = sim.Ended ? "" : !sim.Started ? "Red tiles are too close to buildings. Hold to deploy many." : sim.TroopsLeft == 0 ? "All troops deployed" : $"Deploying: {GameData.Troop(battle.SelectedTroop).Name}";
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                int n = sim.Remaining[(int)s.type];
                s.count.text = "x" + n;
                bool sel = s.type == battle.SelectedTroop && n > 0;
                s.btn.SetColor(sel ? Palette.Green : n > 0 ? Palette.GreyDark : Palette.Alpha(Palette.GreyDark, 0.45f));
            }
        }
    }
}
