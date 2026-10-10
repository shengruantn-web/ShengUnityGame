using UnityEngine;
using VillageClash.Core;

namespace VillageClash.UI
{
    /// <summary>Connects HUD buttons and world taps to panels and controllers.</summary>
    public static class Screens
    {
        public static BuildModeController BuildMode { get; private set; }
        public static BuildingPopup Popup { get; private set; }
        public static BattleController Battle { get; private set; }
        public static BattleHud BattleHud { get; private set; }

        public static void Wire(GameRoot root, Hud hud)
        {
            var c = root.Controller;
            BuildMode = root.gameObject.AddComponent<BuildModeController>();
            Popup = BuildingPopup.Create();
            Popup.OnMove = id => BuildMode.BeginMove(id);
            Popup.OnTrain = () => { Popup.Hide(); ArmyPanel.Show(1); };
            Popup.OnResearch = () => { Popup.Hide(); ArmyPanel.Show(2); };
            Battle = root.gameObject.AddComponent<BattleController>();
            BattleHud = BattleHud.Create(Battle);
            Battle.Finished += result => { BattleHud.Hide(); ResultPanel.Show(result, () => Battle.ReturnHome()); };

            root.Router.Tap += (cell, world) =>
            {
                if (root.Mode != GameMode.Village || UIKit.AnyModalOpen) return;
                var v = c.State.Village;
                var b = v.BuildingAt(cell);
                if (b != null) { Popup.ShowBuilding(b.Id); return; }
                var o = v.ObstacleAt(cell);
                if (o != null) { Popup.ShowObstacle(o.Id); return; }
                Popup.Hide();
            };
            root.Router.Held += (cell, world, dur) =>
            {
                // long-press on a building starts moving it
                if (root.Mode != GameMode.Village || UIKit.AnyModalOpen) return;
                var b = c.State.Village.BuildingAt(cell);
                if (b != null && !BuildMode.Active) { Popup.Hide(); BuildMode.BeginMove(b.Id, cell); }
            };
            root.ModeChanged += m =>
            {
                if (m != GameMode.Village) Popup.Hide();
                if (m == GameMode.Battle) BattleHud.Show();
            };

            hud.OnShop = () => { Popup.Hide(); ShopPanel.Show(t => BuildMode.BeginPlace(t)); };
            hud.OnBuyBuilder = () =>
            {
                Popup.Hide();
                var st = c.Shop(BuildingType.BuilderHut);
                if (!st.CanBuildMore) { UIKit.Toast("You already have all builders!"); return; }
                ConfirmDialog.Show("Hire a Builder", $"Hire another builder for {st.CostGems} gems?\nA second builder lets you build two things at once.", $"{st.CostGems}", () =>
                {
                    if (c.State.Gems < st.CostGems) { GameEvents.RaiseToast(Msg.NotEnoughGems); return; }
                    BuildMode.BeginPlace(BuildingType.BuilderHut);
                }, "Cancel", Palette.Green, "gem");
            };
            hud.OnBuyShield = () =>
            {
                if (c.State.ShieldActive(c.Now)) { UIKit.Toast("Your shield is already active."); return; }
                ConfirmDialog.Show("Buy Shield", $"Protect your village from attacks for 12 hours for {GameData.ShieldGemCost} gems?", $"{GameData.ShieldGemCost}", () =>
                {
                    if (c.BuyShield()) Sfx.Play(SfxId.Upgrade);
                }, "Cancel", Palette.Blue, "gem");
            };
            hud.OnGems = () => UIKit.Toast("Gems are earned by clearing obstacles and winning battles.");
            hud.OnNews = () => UIKit.Toast("No news today. Go raid something!");
            hud.OnProfile = () => { Popup.Hide(); ClanPanel.Show(0); };
            hud.OnAttack = () => { Popup.Hide(); AttackPanel.Show(sim => Battle.Begin(sim)); };
            hud.OnArmy = () => { Popup.Hide(); ArmyPanel.Show(0); };
            hud.OnCastle = () => hud.OnClans?.Invoke();
            hud.OnSettings = () => { Popup.Hide(); SettingsPanel.Show(); };
            hud.OnClans = () => { Popup.Hide(); ClanPanel.Show(1); };
            hud.OnLeague = () => { Popup.Hide(); LeaguePanel.Show(); };
            hud.OnMail = () => { Popup.Hide(); DefenseLogToast.ShowLog(); };
        }
    }
}
