using System;
using UnityEngine;
using UnityEngine.UI;
using VillageClash.Core;

namespace VillageClash.UI
{
    /// <summary>Army modal: Army (current troops), Train (queue + troop cards), Research (lab upgrades).</summary>
    public static class ArmyPanel
    {
        public static void Show(int tab = 0)
        {
            var c = GameRoot.I.Controller;
            var m = UIKit.ShowModal("Army", new Vector2(1400, 820));
            var tabs = UIKit.Tabs(m.Content, new[] { "Army", "Train", "Research" }, new Vector2(220, 60), null);
            var tabsRt = (RectTransform)m.Content.Find("tabs");
            UIKit.Place(tabsRt, UIKit.TL, new Vector2(10, -4), new Vector2(700, 60));
            var body = UIKit.Node("body", m.Content);
            UIKit.Stretch(body, 0, 76, 0, 0);
            UIKit.Panel(body, Palette.CreamDark, true, false);
            int sel = tab;
            Action rebuild = null;
            tabs.OnSelect = i => { sel = i; rebuild(); };
            rebuild = () =>
            {
                foreach (Transform t in body) if (t.name != "panel") UnityEngine.Object.Destroy(t.gameObject);
                switch (sel)
                {
                    case 0: ArmyTab(body, c); break;
                    case 1: TrainTab(body, c); break;
                    default: ResearchTab(body, c); break;
                }
            };
            tabs.Select(tab);
            Action onState = () => rebuild();
            GameEvents.StateChanged += onState;
            // timers
            var ticker = body.gameObject.AddComponent<Ticker>();
            ticker.OnTick = () => { if (sel != 0) rebuild(); };
            m.OnClosed = () => GameEvents.StateChanged -= onState;
        }

        sealed class Ticker : MonoBehaviour
        {
            public Action OnTick; float t;
            void Update() { t += Time.unscaledDeltaTime; if (t >= 1f) { t = 0; OnTick?.Invoke(); } }
        }

        static void ArmyTab(RectTransform body, GameController c)
        {
            var s = c.State;
            int cap = Training.CampCapacity(s), used = Training.ArmyHousing(s);
            var bar = UIKit.Bar(body, new Vector2(600, 40), Palette.Green, null, 24);
            UIKit.Place(bar.Rt, UIKit.TC, new Vector2(0, -16), new Vector2(600, 40));
            bar.Set(cap > 0 ? used / (float)cap : 0, cap > 0 ? $"Army camps: {used} / {cap}" : "Build an Army Camp to house troops");
            var grid = UIKit.Node("grid", body);
            UIKit.Stretch(grid, 20, 70, 20, 20);
            UIKit.Grid(grid, new Vector2(240, 300), 16, 10);
            bool any = false;
            foreach (var def in GameData.AllTroops)
            {
                int n = s.Army(def.Type);
                if (n == 0) continue;
                any = true;
                var card = UIKit.Frame(grid, Palette.Cream, 3);
                var name = UIKit.Dark(card, $"{def.Name}  Lv{s.TroopLevel(def.Type)}", 24, TextAnchor.MiddleCenter);
                UIKit.Place((RectTransform)name.transform, UIKit.TC, new Vector2(0, -8), new Vector2(230, 32));
                var pic = UIKit.Sprite(card, SpriteFactory.Troop(def.Type), 120, 150);
                UIKit.Place((RectTransform)pic.transform, UIKit.MC, new Vector2(0, 0), new Vector2(120, 150));
                var cnt = UIKit.Label(card, $"x{n}", 34, Palette.Gold);
                UIKit.Place((RectTransform)cnt.transform, UIKit.BC, new Vector2(0, 10), new Vector2(200, 40));
            }
            if (!any)
            {
                var empty = UIKit.Dark(body, "Your army is empty.\nOpen the Train tab to train troops.", 30, TextAnchor.MiddleCenter);
                UIKit.Stretch((RectTransform)empty.transform);
            }
        }

        static void TrainTab(RectTransform body, GameController c)
        {
            var s = c.State; long now = c.Now;
            // queue strip
            var q = UIKit.Node("queue", body);
            UIKit.Place(q, UIKit.TL, new Vector2(16, -12), new Vector2(1330, 120));
            UIKit.Panel(q, Palette.Header, true, false);
            if (Training.BarracksLevel(s) == 0)
            {
                var t = UIKit.Label(q, "Build a Barracks to train troops!", 28);
                UIKit.Stretch((RectTransform)t.transform);
            }
            else if (s.TrainQueue.Count == 0)
            {
                var t = UIKit.Label(q, "Training queue is empty. Tap a troop below to train it.", 26);
                UIKit.Stretch((RectTransform)t.transform);
            }
            else
            {
                var row = UIKit.Node("row", q);
                UIKit.Place(row, UIKit.ML, new Vector2(12, 0), new Vector2(900, 110));
                UIKit.HList(row, 10, 0, TextAnchor.MiddleLeft);
                for (int i = 0; i < s.TrainQueue.Count; i++)
                {
                    var job = s.TrainQueue[i]; int idx = i;
                    var slot = UIKit.Node("slot", row);
                    UIKit.Size(slot, 100, 104);
                    UIKit.Panel(slot, Palette.CreamDark, true, false);
                    var pic = UIKit.Sprite(slot, SpriteFactory.Troop(job.Type), 60, 75);
                    UIKit.Place((RectTransform)pic.transform, UIKit.MC, new Vector2(0, -6), new Vector2(60, 75));
                    var n = UIKit.Label(slot, $"x{job.Count}", 22);
                    UIKit.Place((RectTransform)n.transform, UIKit.BC, new Vector2(0, 2), new Vector2(90, 26));
                    var x = UIKit.IconBtn(slot, "close", Palette.Red, 28, () => c.CancelTrain(idx), true);
                    UIKit.Place(x.Rt, UIKit.TR, new Vector2(2, 2), new Vector2(28, 28));
                    if (i == 0)
                    {
                        var tm = UIKit.Label(slot, Fmt.Time(Training.NextFinishIn(s, now)), 20, Palette.Gold);
                        UIKit.Place((RectTransform)tm.transform, UIKit.TC, new Vector2(0, -2), new Vector2(90, 24));
                    }
                }
                var total = UIKit.Label(q, $"Total: {Fmt.Time(Training.TotalRemaining(s, now))}", 24, Color.white, TextAnchor.MiddleRight);
                UIKit.Place((RectTransform)total.transform, UIKit.MR, new Vector2(-230, 0), new Vector2(300, 40));
                int gems = Training.GemFinishCost(s, now);
                var fin = UIKit.Btn(q, $"Finish {gems}", Palette.Green, new Vector2(200, 64), () => { if (c.FinishTrainingWithGems()) Sfx.Play(SfxId.Upgrade); }, 24, "gem");
                UIKit.Place(fin.Rt, UIKit.MR, new Vector2(-12, 0), new Vector2(200, 64));
            }
            // housing bar
            int cap = Training.CampCapacity(s), used = Training.ArmyHousing(s) + Training.QueuedHousing(s);
            var bar = UIKit.Bar(body, new Vector2(500, 36), Palette.Green, null, 22);
            UIKit.Place(bar.Rt, UIKit.TC, new Vector2(0, -142), new Vector2(500, 36));
            bar.Set(cap > 0 ? used / (float)cap : 0, $"Housing (army + queue): {used} / {cap}");
            // troop cards
            var grid = UIKit.Node("grid", body);
            UIKit.Stretch(grid, 16, 190, 16, 10);
            UIKit.Grid(grid, new Vector2(250, 330), 16, 8);
            foreach (var def in GameData.AllTroops)
            {
                var card = UIKit.Frame(grid, Palette.Cream, 3);
                bool unlocked = Training.Unlocked(s, def.Type);
                var name = UIKit.Dark(card, $"{def.Name}  Lv{s.TroopLevel(def.Type)}", 24, TextAnchor.MiddleCenter);
                UIKit.Place((RectTransform)name.transform, UIKit.TC, new Vector2(0, -8), new Vector2(240, 32));
                var info = UIKit.IconBtn(card, "info", Palette.Blue, 32, () => TroopInfo(def, s), true);
                UIKit.Place(info.Rt, UIKit.TR, new Vector2(-6, -6), new Vector2(32, 32));
                var pic = UIKit.Sprite(card, SpriteFactory.Troop(def.Type), 110, 140);
                UIKit.Place((RectTransform)pic.transform, UIKit.TC, new Vector2(0, -44), new Vector2(110, 140));
                var meta = UIKit.Dark(card, $"Housing {def.Housing}   Time {Fmt.Time(def.TrainSeconds)}", 20, TextAnchor.MiddleCenter, false);
                UIKit.Place((RectTransform)meta.transform, UIKit.TC, new Vector2(0, -190), new Vector2(240, 28));
                int cost = Training.Cost(s, def.Type);
                string err = Training.EnqueueError(s, def.Type);
                var btn = UIKit.Btn(card, Fmt.Num(cost), err == null ? Palette.Green : Palette.Grey, new Vector2(200, 60), () =>
                {
                    var e = Training.EnqueueError(s, def.Type);
                    if (e == Msg.NotEnoughElixir) { ConfirmDialog.OfferGemFill(c, 0, Training.Cost(s, def.Type), () => { if (c.Train(def.Type)) Sfx.Play(SfxId.Coin); }); return; }
                    if (c.Train(def.Type)) Sfx.Play(SfxId.Coin); else Sfx.Play(SfxId.Error);
                }, 26, "elixir");
                UIKit.Place(btn.Rt, UIKit.BC, new Vector2(0, 10), new Vector2(200, 60));
                if (!unlocked)
                {
                    var lockP = UIKit.Panel(card, new Color(0.3f, 0.3f, 0.3f, 0.82f));
                    UIKit.Stretch((RectTransform)lockP.transform, 3, 3, 3, 3);
                    var li = UIKit.Icon(lockP.transform, "lock", 56);
                    UIKit.Place((RectTransform)li.transform, UIKit.MC, new Vector2(0, 30), new Vector2(56, 56));
                    var lt = UIKit.Label(lockP.transform, $"Barracks level {def.BarracksLevel} required", 22);
                    UIKit.Place((RectTransform)lt.transform, UIKit.MC, new Vector2(0, -30), new Vector2(220, 60));
                }
            }
        }

        static void ResearchTab(RectTransform body, GameController c)
        {
            var s = c.State; long now = c.Now;
            var lab = Laboratory.Lab(s);
            var head = UIKit.Node("head", body);
            UIKit.Place(head, UIKit.TL, new Vector2(16, -12), new Vector2(1330, 70));
            UIKit.Panel(head, Palette.Header, true, false);
            string headText = lab == null ? "Build a Laboratory to research troop upgrades!"
                : Laboratory.Busy(s) ? $"Researching {GameData.Troop((TroopType)s.LabTroop).Name}: {Fmt.Time(Math.Max(0, s.LabFinishUnix - now))} left"
                : $"Laboratory level {lab.Level} - idle";
            var ht = UIKit.Label(head, headText, 26, Color.white, TextAnchor.MiddleLeft);
            UIKit.Stretch((RectTransform)ht.transform, 20, 0, 260, 0);
            if (Laboratory.Busy(s))
            {
                int gems = Builders.GemFinishCost(Math.Max(0, s.LabFinishUnix - now));
                var fin = UIKit.Btn(head, $"Finish {gems}", Palette.Green, new Vector2(220, 56), () => c.FinishResearchWithGems(), 24, "gem");
                UIKit.Place(fin.Rt, UIKit.MR, new Vector2(-10, 0), new Vector2(220, 56));
            }
            var grid = UIKit.Node("grid", body);
            UIKit.Stretch(grid, 16, 92, 16, 10);
            UIKit.Grid(grid, new Vector2(250, 330), 16, 8);
            foreach (var def in GameData.AllTroops)
            {
                var card = UIKit.Frame(grid, Palette.Cream, 3);
                int lvl = s.TroopLevel(def.Type);
                var name = UIKit.Dark(card, $"{def.Name}  Lv{lvl}", 24, TextAnchor.MiddleCenter);
                UIKit.Place((RectTransform)name.transform, UIKit.TC, new Vector2(0, -8), new Vector2(240, 32));
                var pic = UIKit.Sprite(card, SpriteFactory.Troop(def.Type), 110, 140);
                UIKit.Place((RectTransform)pic.transform, UIKit.TC, new Vector2(0, -44), new Vector2(110, 140));
                if (lvl >= def.MaxLevel)
                {
                    var mx = UIKit.Dark(card, "MAX LEVEL", 24, TextAnchor.MiddleCenter);
                    UIKit.Place((RectTransform)mx.transform, UIKit.BC, new Vector2(0, 20), new Vector2(240, 40));
                    continue;
                }
                int cost = def.UpgradeCostElixir[lvl - 1];
                var meta = UIKit.Dark(card, $"Lv{lvl + 1}: HP {def.Hp[lvl]}  DPS {def.Dps[lvl]}\nTime {Fmt.Time(def.UpgradeSeconds[lvl - 1])}  Lab {def.LabLevelRequired[lvl - 1]}", 19, TextAnchor.MiddleCenter, false);
                UIKit.Place((RectTransform)meta.transform, UIKit.TC, new Vector2(0, -186), new Vector2(240, 50));
                string err = Laboratory.StartError(s, def.Type);
                var btn = UIKit.Btn(card, Fmt.Num(cost), err == null ? Palette.Elixir : Palette.Grey, new Vector2(200, 60), () =>
                {
                    var e = Laboratory.StartError(s, def.Type);
                    if (e == Msg.NotEnoughElixir) { ConfirmDialog.OfferGemFill(c, 0, cost, () => c.Research(def.Type)); return; }
                    if (c.Research(def.Type)) Sfx.Play(SfxId.Upgrade); else Sfx.Play(SfxId.Error);
                }, 26, "elixir");
                UIKit.Place(btn.Rt, UIKit.BC, new Vector2(0, 10), new Vector2(200, 60));
            }
        }

        public static void TroopInfo(TroopDef def, GameState s)
        {
            var m = UIKit.ShowModal(def.Name, new Vector2(800, 520));
            var pic = UIKit.Sprite(m.Content, SpriteFactory.Troop(def.Type), 160, 200);
            UIKit.Place((RectTransform)pic.transform, UIKit.TL, new Vector2(30, -20), new Vector2(160, 200));
            var desc = UIKit.Dark(m.Content, def.Description, 24, TextAnchor.UpperLeft, false);
            UIKit.Place((RectTransform)desc.transform, UIKit.TL, new Vector2(220, -20), new Vector2(540, 200));
            string rows = $"Favorite target: {def.Preference}\nRange: {def.Range}   Speed: {def.Speed}   Housing: {def.Housing}\n";
            for (int l = 1; l <= def.MaxLevel; l++) rows += $"\nLevel {l}:  HP {def.Hp[l - 1]}   DPS {def.Dps[l - 1]}   Cost {def.CostElixir[l - 1]} elixir" + (l == s.TroopLevel(def.Type) ? "   (current)" : "");
            var t = UIKit.Dark(m.Content, rows, 22, TextAnchor.UpperLeft, false);
            UIKit.Place((RectTransform)t.transform, UIKit.TL, new Vector2(30, -230), new Vector2(740, 220));
        }
    }
}
