using System;
using System.Collections;
using System.IO;
using UnityEngine;
using VillageClash.Core;

namespace VillageClash.UI
{
    /// <summary>
    /// Smoke-test/screenshot driver. Launch the player with "-shots &lt;dir&gt;" and it walks through the
    /// main screens, saving a PNG of each to that directory, then quits. Used for headless visual checks.
    /// </summary>
    public sealed class AutoShots : MonoBehaviour
    {
        string dir;

        public static void TryStart()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-shots")
                {
                    var go = new GameObject("AutoShots");
                    var a = go.AddComponent<AutoShots>();
                    a.dir = args[i + 1];
                    Directory.CreateDirectory(a.dir);
                    a.StartCoroutine(a.Run());
                    return;
                }
        }

        static int fails;
        static void Check(bool ok, string what)
        {
            if (!ok) fails++;
            Debug.Log("[Smoke] " + (ok ? "OK   " : "FAIL ") + what);
        }

        IEnumerator Shot(string name)
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
            yield return null; yield return null;
            Debug.Log("[AutoShots] " + name);
        }

        IEnumerator Run()
        {
            var root = GameRoot.I;
            var c = root.Controller;
            yield return new WaitForSeconds(1.5f);
            UIKit.CloseAllModals();
            yield return Shot("01_village");

            // tap-equivalent: open a building popup
            var th = c.State.Village.TownHall;
            if (th != null) { Screens.Popup.ShowBuilding(th.Id); yield return new WaitForSeconds(0.3f); yield return Shot("02_building_popup"); Screens.Popup.Hide(); }

            ShopPanel.Show(t => Screens.BuildMode.BeginPlace(t), 2);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("03_shop_defenses");
            UIKit.CloseAllModals();

            // build mode with a cannon ghost
            c.State.Gold = Math.Max(c.State.Gold, 2000);
            Screens.BuildMode.BeginPlace(BuildingType.Cannon);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("04_build_mode");
            Screens.BuildMode.Cancel();

            // place a cannon on the first free cell
            int cannons = c.State.Village.Count(BuildingType.Cannon);
            bool placed = false;
            for (int y = 2; y < GameData.GridSize - 2 && !placed; y++)
                for (int x = 2; x < GameData.GridSize - 2 && !placed; x++)
                    if (c.State.Village.CanPlace(BuildingType.Cannon, new Int2(x, y)))
                        placed = c.ConfirmPlace(BuildingType.Cannon, new Int2(x, y));
            Check(placed && c.State.Village.Count(BuildingType.Cannon) == cannons + 1, "place cannon");
            yield return new WaitForSeconds(0.3f);

            // collect from a mine (may legitimately have nothing yet)
            var mine = c.State.Village.FindFirst(BuildingType.GoldMine);
            Debug.Log("[Smoke] collect gold mine -> " + (mine != null && c.Collect(mine.Id)));

            // train one barbarian
            int queued = c.State.TrainQueue.Count;
            Check(c.Train(TroopType.Barbarian) && c.State.TrainQueue.Count == queued + 1, "train barbarian");

            // army
            c.State.Elixir = Math.Max(c.State.Elixir, 3000);
            if (c.State.ArmySize == 0) { c.State.SetArmy(TroopType.Barbarian, 8); c.State.SetArmy(TroopType.Archer, 6); c.State.SetArmy(TroopType.Giant, 1); }
            ArmyPanel.Show(0);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("05_army");
            UIKit.CloseAllModals();

            AttackPanel.Show(sim => Screens.Battle.Begin(sim));
            yield return new WaitForSeconds(0.3f);
            yield return Shot("06_attack_menu");
            UIKit.CloseAllModals();

            // battle
            var battle = c.StartPractice(0);
            Check(battle != null, "start practice battle");
            BattleResult result = null;
            Screens.Battle.Finished += r => result = r;
            if (battle != null)
            {
                Screens.Battle.Begin(battle);
                yield return new WaitForSeconds(0.6f);
                yield return Shot("07_battle_start");
                if (battle.NearestDeployCell(new Int2(3, battle.Size / 2), out var cell))
                {
                    for (int i = 0; i < 6; i++) battle.TryDeploy(TroopType.Barbarian, cell.X + 0.5f, cell.Y + 0.5f + (i % 3) * 0.3f);
                    for (int i = 0; i < 4; i++) battle.TryDeploy(TroopType.Archer, cell.X - 0.5f, cell.Y + 0.5f + i * 0.3f);
                    battle.TryDeploy(TroopType.Giant, cell.X + 0.5f, cell.Y - 0.5f);
                }
                yield return new WaitForSeconds(4f);
                yield return Shot("08_battle_fighting");
                float t = 0;
                while (!battle.Ended && t < 40f) { t += Time.deltaTime; yield return null; }
                if (!battle.Ended) Screens.Battle.EndBattle();
                yield return new WaitForSeconds(2.5f);
                Check(result != null, "battle result reported");
                if (result != null) Debug.Log($"[Smoke] result stars={result.Stars} percent={result.Percent} gold={result.Gold} elixir={result.Elixir} practice={result.Practice}");
                Check(c.ActiveBattle == null, "battle finished in controller");
                yield return Shot("09_result");
                UIKit.CloseAllModals();
                Screens.Battle.ReturnHome();
                yield return new WaitForSeconds(0.5f);
            }

            LeaguePanel.Show(0); yield return new WaitForSeconds(0.3f); yield return Shot("10_league"); UIKit.CloseAllModals();
            ClanPanel.Show(1); yield return new WaitForSeconds(0.3f); yield return Shot("11_clans"); UIKit.CloseAllModals();
            SettingsPanel.Show(); yield return new WaitForSeconds(0.3f); yield return Shot("12_settings"); UIKit.CloseAllModals();
            DefenseLogToast.ShowLog(); yield return new WaitForSeconds(0.3f); yield return Shot("13_mail"); UIKit.CloseAllModals();

            c.Save();
            var json = File.ReadAllText(root.SavePath);
            Check(SaveSystem.TryDeserialize(json, out var reloaded) && reloaded.Gold == c.State.Gold
                  && reloaded.Village.Count(BuildingType.Cannon) == c.State.Village.Count(BuildingType.Cannon), "save and reload");
            Debug.Log("[Smoke] " + (fails == 0 ? "ALL CHECKS PASSED" : fails + " CHECK(S) FAILED"));
            Debug.Log("[AutoShots] done");
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }
    }
}
