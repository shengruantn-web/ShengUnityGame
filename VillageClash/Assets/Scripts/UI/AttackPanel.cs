using System;
using UnityEngine;
using VillageClash.Core;

namespace VillageClash.UI
{
    /// <summary>Attack menu: Find a Match (random generated village, costs gold) or Practice maps.</summary>
    public static class AttackPanel
    {
        public static void Show(Action<BattleSim> onStart)
        {
            var c = GameRoot.I.Controller; var s = c.State;
            var m = UIKit.ShowModal("Attack!", new Vector2(1200, 700));
            // left: find a match
            var left = UIKit.Frame(m.Content, Palette.Cream, 3);
            UIKit.Place(left, UIKit.TL, new Vector2(16, -10), new Vector2(540, 560));
            var t1 = UIKit.Dark(left, "Find a Match", 34, TextAnchor.MiddleCenter);
            UIKit.Place((RectTransform)t1.transform, UIKit.TC, new Vector2(0, -12), new Vector2(500, 44));
            var ic = UIKit.Icon(left, "sword", 160);
            UIKit.Place((RectTransform)ic.transform, UIKit.TC, new Vector2(0, -70), new Vector2(160, 160));
            var league = Leagues.ForTrophies(s.Trophies);
            var d1 = UIKit.Dark(left, $"Raid a random village for loot and trophies.\n\nYour trophies: {s.Trophies}\nLeague: {league.Name}\nEnemy Town Hall: {Mathf.Clamp(s.TownHallLevel, 1, GameData.MaxTownHall)} (about your level)\n\nAttacking removes your shield.", 22, TextAnchor.UpperCenter, false);
            UIKit.Place((RectTransform)d1.transform, UIKit.TC, new Vector2(0, -240), new Vector2(480, 220));
            var army = UIKit.Dark(left, $"Army: {s.ArmySize} troops ready", 22, TextAnchor.MiddleCenter);
            UIKit.Place((RectTransform)army.transform, UIKit.BC, new Vector2(0, 94), new Vector2(480, 30));
            string err = c.AttackError();
            var find = UIKit.Btn(left, $"Find a Match   {GameData.MatchCostGold}", err == null ? Palette.Red : Palette.Grey, new Vector2(400, 72), () =>
            {
                var sim = c.StartMatch(Environment.TickCount ^ (int)DateTime.UtcNow.Ticks);
                if (sim == null) { Sfx.Play(SfxId.Error); return; }
                m.Close();
                onStart(sim);
            }, 26, "gold");
            UIKit.Place(find.Rt, UIKit.BC, new Vector2(0, 14), new Vector2(400, 72));

            // right: practice
            var right = UIKit.Frame(m.Content, Palette.Cream, 3);
            UIKit.Place(right, UIKit.TR, new Vector2(-16, -10), new Vector2(590, 560));
            var t2 = UIKit.Dark(right, "Practice", 34, TextAnchor.MiddleCenter);
            UIKit.Place((RectTransform)t2.transform, UIKit.TC, new Vector2(0, -12), new Vector2(500, 44));
            var d2 = UIKit.Dark(right, "Goblin villages for practice. Loot counts, no trophies, free to attack.", 22, TextAnchor.MiddleCenter, false);
            UIKit.Place((RectTransform)d2.transform, UIKit.TC, new Vector2(0, -60), new Vector2(540, 60));
            var list = UIKit.Node("list", right);
            UIKit.Place(list, UIKit.TC, new Vector2(0, -130), new Vector2(540, 400));
            UIKit.VList(list, 12);
            for (int i = 0; i < EnemyBaseGenerator.PracticeNames.Length; i++)
            {
                int id = i;
                var row = UIKit.Node("row", list);
                UIKit.Size(row, 540, 110);
                UIKit.Panel(row, Palette.CreamDark, true, false);
                var gi = UIKit.Icon(row, "goblin", 70);
                UIKit.Place((RectTransform)gi.transform, UIKit.ML, new Vector2(16, 0), new Vector2(70, 70));
                var eb = EnemyBaseGenerator.Practice(id);
                int gold = LootCalculator.Available(eb.Gold, eb.TownHallLevel);
                int elixir = LootCalculator.Available(eb.Elixir, eb.TownHallLevel);
                var name = UIKit.Dark(row, $"{eb.Name}\nTown Hall {eb.TownHallLevel}\nLoot: {Fmt.Num(gold)} gold / {Fmt.Num(elixir)} elixir", 20, TextAnchor.MiddleLeft, false);
                UIKit.Place((RectTransform)name.transform, UIKit.ML, new Vector2(100, 0), new Vector2(286, 100));
                var go = UIKit.Btn(row, "Attack", s.ArmySize > 0 ? Palette.Red : Palette.Grey, new Vector2(130, 64), () =>
                {
                    var sim = c.StartPractice(id);
                    if (sim == null) { Sfx.Play(SfxId.Error); return; }
                    m.Close();
                    onStart(sim);
                }, 24);
                UIKit.Place(go.Rt, UIKit.MR, new Vector2(-12, 0), new Vector2(130, 64));
            }
        }
    }
}
