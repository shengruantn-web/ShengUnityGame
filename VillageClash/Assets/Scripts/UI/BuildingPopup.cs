using System;
using UnityEngine;
using UnityEngine.UI;
using VillageClash.Core;

namespace VillageClash.UI
{
    /// <summary>Bottom panel shown when a building or obstacle is tapped. Non-modal; refreshes every second.</summary>
    public sealed class BuildingPopup : MonoBehaviour
    {
        public static BuildingPopup I { get; private set; }
        public int BuildingId = -1, ObstacleId = -1;
        public Action<int> OnMove; public Action OnTrain, OnResearch;

        RectTransform root, actions; Text title, sub, stats; float timer;

        public static BuildingPopup Create()
        {
            var rt = UIKit.Node("BuildingPopup", UIKit.PanelLayer);
            UIKit.Place(rt, UIKit.BC, new Vector2(0, 150), new Vector2(1180, 230));
            I = rt.gameObject.AddComponent<BuildingPopup>();
            I.root = rt;
            var frame = UIKit.Frame(rt, Palette.Cream, 4);
            UIKit.Stretch(frame);
            var header = UIKit.Node("header", rt);
            header.anchorMin = new Vector2(0, 1); header.anchorMax = new Vector2(1, 1); header.pivot = new Vector2(0.5f, 1);
            header.offsetMin = new Vector2(4, -56); header.offsetMax = new Vector2(-4, -4);
            UIKit.Panel(header, Palette.Header, true, false);
            I.title = UIKit.Label(header, "", 30, Color.white, TextAnchor.MiddleLeft);
            UIKit.Stretch((RectTransform)I.title.transform, 20, 0, 300, 0);
            I.sub = UIKit.Label(header, "", 24, Palette.Gold, TextAnchor.MiddleRight);
            UIKit.Stretch((RectTransform)I.sub.transform, 300, 0, 70, 0);
            var x = UIKit.IconBtn(header, "close", Palette.Red, 44, () => I.Hide(), true);
            UIKit.Place(x.Rt, UIKit.MR, new Vector2(-6, 0), new Vector2(44, 44));
            I.stats = UIKit.Dark(rt, "", 22, TextAnchor.UpperLeft, false);
            UIKit.Place((RectTransform)I.stats.transform, UIKit.TL, new Vector2(20, -66), new Vector2(380, 150));
            I.actions = UIKit.Node("actions", rt);
            UIKit.Place(I.actions, UIKit.MR, new Vector2(-16, -24), new Vector2(740, 150));
            UIKit.HList(I.actions, 12, 0, TextAnchor.MiddleRight);
            rt.gameObject.SetActive(false);
            GameEvents.StateChanged += I.Refresh;
            return I;
        }

        void OnDestroy() { GameEvents.StateChanged -= Refresh; }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer > 1f) { timer = 0; Refresh(); }
        }

        public void ShowBuilding(int id)
        {
            BuildingId = id; ObstacleId = -1;
            root.gameObject.SetActive(true);
            GameRoot.I.Village.HighlightedId = id;
            Sfx.Play(SfxId.Tap);
            Refresh();
        }

        public void ShowObstacle(int id)
        {
            ObstacleId = id; BuildingId = -1;
            root.gameObject.SetActive(true);
            GameRoot.I.Village.HighlightedId = -1;
            Sfx.Play(SfxId.Tap);
            Refresh();
        }

        public void Hide()
        {
            if (!root.gameObject.activeSelf) return;
            BuildingId = -1; ObstacleId = -1;
            if (GameRoot.I != null) GameRoot.I.Village.HighlightedId = -1;
            root.gameObject.SetActive(false);
        }

        public bool Visible => root.gameObject.activeSelf;

        void ClearActions() { foreach (Transform t in actions) Destroy(t.gameObject); }

        BtnView AddAction(string text, Color color, Action onClick, string icon = null, float w = 150)
        {
            var b = UIKit.Btn(actions, text, color, new Vector2(w, 110), onClick, 22, icon);
            UIKit.Size(b.Rt, w, 110);
            if (icon != null && b.Label != null)
            {
                UIKit.Place((RectTransform)b.Label.transform, UIKit.BC, new Vector2(0, 6), new Vector2(w, 50));
                UIKit.Place((RectTransform)b.Go.transform.Find("icon:" + icon), UIKit.TC, new Vector2(0, -6), new Vector2(48, 48));
            }
            return b;
        }

        public void Refresh()
        {
            if (!Visible || GameRoot.I == null) return;
            var c = GameRoot.I.Controller; var s = c.State; long now = c.Now;
            ClearActions();
            if (ObstacleId >= 0)
            {
                var o = s.Village.FindObstacle(ObstacleId);
                if (o == null) { Hide(); return; }
                title.text = o.Kind.ToString();
                sub.text = "";
                int g = GameData.ObstacleRemoveCostGold(o.Kind), e = GameData.ObstacleRemoveCostElixir(o.Kind);
                stats.text = o.Removing
                    ? $"Being removed... {Fmt.Time(Math.Max(0, o.FinishAtUnix - now))} left"
                    : $"Clear this {o.Kind.ToString().ToLower()} to free space.\nCost: {(g > 0 ? g + " gold" : e + " elixir")}   Time: {Fmt.Time(GameData.ObstacleRemoveSeconds(o.Kind))}\nReward: a few gems";
                if (!o.Removing)
                {
                    int id = o.Id;
                    AddAction($"Remove\n{(g > 0 ? g + " gold" : e + " elixir")}", Palette.Green, () =>
                    {
                        if (c.RemoveObstacle(id)) { Sfx.Play(SfxId.Build); Hide(); }
                        else if (Builders.Free(s) > 0 && !Economy.CanAfford(s, g, e)) ConfirmDialog.OfferGemFill(c, g, e, () => { if (c.RemoveObstacle(id)) Hide(); });
                    }, "hammer", 180);
                }
                return;
            }
            var b = s.Village.Find(BuildingId);
            if (b == null) { Hide(); return; }
            var def = b.Def; var st = b.Stats;
            title.text = $"{def.Name}";
            sub.text = $"Level {b.Level}" + (b.Level >= def.MaxLevel ? " (MAX)" : "");
            string line = $"HP: {st.Hp}";
            if (def.IsDefense) line += $"\nDamage: {st.Damage} every {st.AttackCooldown:0.#}s\nRange: {st.Range}" + (st.MinRange > 0 ? $" (min {st.MinRange})" : "") + (st.SplashRadius > 0 ? $"\nSplash: {st.SplashRadius}" : "");
            else if (Economy.IsProducer(b.Type)) line += $"\nProduces {st.ProductionPerHour}/hour\nStored: {(int)b.Stored} / {Fmt.Num(st.Capacity)}";
            else if (b.Type == BuildingType.GoldStorage || b.Type == BuildingType.ElixirStorage || b.Type == BuildingType.ArmyCamp) line += $"\nCapacity: {Fmt.Num(st.Capacity)}";
            else if (b.Type == BuildingType.TownHall) line += $"\nStores {Fmt.Num(st.Capacity)} gold and elixir\nBuildings unlocked up to TH {b.Level}";
            else if (b.Type == BuildingType.Barracks) line += $"\nTrains troops. Queue: {Training.QueuedCount(s)}";
            else if (b.Type == BuildingType.Laboratory) line += Laboratory.Busy(s) ? $"\nResearching {GameData.Troop((TroopType)s.LabTroop).Name}: {Fmt.Time(Math.Max(0, s.LabFinishUnix - now))}" : "\nIdle";
            else if (b.Type == BuildingType.BuilderHut) line += $"\nBuilders: {Builders.Free(s)} / {Builders.Total(s)} free";
            else if (def.IsTrap) line += b.Triggered ? "\nTriggered! Re-arms after the battle." : "\nHidden from attackers until triggered.";
            if (b.Constructing) line += $"\n{(b.IsNewBuild ? "Building" : "Upgrading")}: {Fmt.Time(Builders.Remaining(b, now))} left";
            stats.text = line;

            int id2 = b.Id;
            AddAction("Info", Palette.Blue, () => ShopPanel.InfoModal(def), "info", 120);
            if (b.Constructing)
            {
                int gems = Builders.GemFinishCost(Builders.Remaining(b, now));
                AddAction($"Finish now\n{gems} gems", Palette.Green, () => { if (c.FinishWithGems(id2)) Sfx.Play(SfxId.Upgrade); }, "gem", 170);
            }
            else
            {
                if (Economy.IsProducer(b.Type))
                    AddAction("Collect", Palette.Gold, () => { if (c.Collect(id2)) Sfx.Play(SfxId.Collect); }, b.Type == BuildingType.GoldMine ? "gold" : "elixir", 140);
                if (b.Type == BuildingType.Barracks) AddAction("Train", Palette.Green, () => OnTrain?.Invoke(), "sword", 140);
                if (b.Type == BuildingType.ArmyCamp) AddAction("Army", Palette.Blue, () => OnTrain?.Invoke(), "fist", 140);
                if (b.Type == BuildingType.Laboratory) AddAction("Research", Palette.Elixir, () => OnResearch?.Invoke(), "xp", 150);
                if (b.Level < def.MaxLevel)
                {
                    var next = def.Stats(b.Level + 1);
                    string gate = ShopRules.UpgradeGateError(s, b);
                    string costText = next.CostGems > 0 ? $"{next.CostGems} gems" : next.CostElixir > 0 ? $"{Fmt.Num(next.CostElixir)} elixir" : $"{Fmt.Num(next.CostGold)} gold";
                    bool afford = Economy.CanAfford(s, next.CostGold, next.CostElixir, next.CostGems);
                    var up = AddAction($"Upgrade\n{costText}", gate == null && afford ? Palette.Green : Palette.Grey, () =>
                    {
                        if (gate != null) { GameEvents.RaiseToast(gate); Sfx.Play(SfxId.Error); return; }
                        if (Builders.Free(s) == 0) { GameEvents.RaiseToast(Msg.BuildersBusy); Sfx.Play(SfxId.Error); return; }
                        if (!afford && next.CostGems == 0) { ConfirmDialog.OfferGemFill(c, next.CostGold, next.CostElixir, () => { if (c.Upgrade(id2)) Sfx.Play(SfxId.Upgrade); }); return; }
                        if (c.Upgrade(id2)) Sfx.Play(SfxId.Upgrade);
                    }, "arrow", 180);
                    if (gate != null) { up.SetText("Upgrade\n" + (gate == Msg.TownHallRequired ? "TH needed" : "locked")); }
                }
            }
            if (!def.IsWall || true) AddAction("Move", Palette.GreyDark, () => { Hide(); OnMove?.Invoke(id2); }, "arrow", 120);
        }
    }
}
