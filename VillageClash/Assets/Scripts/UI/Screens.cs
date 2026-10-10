using VillageClash.Core;

namespace VillageClash.UI
{
    /// <summary>Connects HUD buttons to panels. Panels are added task by task; unwired buttons toast.</summary>
    public static class Screens
    {
        public static void Wire(GameRoot root, Hud hud)
        {
            var c = root.Controller;
            hud.OnBuyBuilder = () =>
            {
                UIKit.Toast($"Buy a second builder for {GameData.SecondBuilderGemCost} gems from the Shop");
            };
            hud.OnBuyShield = () =>
            {
                if (c.BuyShield()) { UIKit.Toast("Shield activated for 12 hours"); Sfx.Play(SfxId.Upgrade); }
            };
            hud.OnGems = () => UIKit.Toast("Gems are earned by clearing obstacles and winning battles");
            hud.OnNews = () => UIKit.Toast("No news today. Go raid something!");
            hud.OnProfile = () => UIKit.Toast($"{c.State.PlayerName} - Level {c.State.Level} - {c.State.BattlesWon}/{c.State.BattlesPlayed} battles won");
            hud.OnAttack = () => UIKit.Toast("Attack menu coming soon");
            hud.OnArmy = () => UIKit.Toast("Army panel coming soon");
            hud.OnCastle = () => hud.OnClans?.Invoke();
            hud.OnShop = () => UIKit.Toast("Shop coming soon");
            hud.OnSettings = () => UIKit.Toast("Settings coming soon");
            hud.OnClans = () => UIKit.Toast("Clans coming soon");
            hud.OnLeague = () => UIKit.Toast("League coming soon");
            hud.OnMail = () => UIKit.Toast("No defense reports yet");
        }
    }
}
