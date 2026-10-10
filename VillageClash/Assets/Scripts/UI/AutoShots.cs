using System;
using System.Collections;
using System.IO;
using UnityEngine;
using VillageClash.Core;

namespace VillageClash.UI
{
    /// <summary>Standalone gameplay verification with optional screenshots of all main screens.</summary>
    public sealed class AutoShots : MonoBehaviour
    {
        SmokeSession session;
        int screenshots;

        public static void TryStart()
        {
            if (SmokeSession.Current == null || SmokeSession.Current.Finished || SmokeSession.Current.Interactive) return;
            var runner = new GameObject("AutoShots").AddComponent<AutoShots>();
            runner.session = SmokeSession.Current;
            runner.StartCoroutine(runner.Run());
        }

        void Check(bool ok, string what) => session.Check(ok, what);

        IEnumerator Pause(float seconds)
        {
            if (session.ShotsDirectory != null) yield return new WaitForSecondsRealtime(seconds);
            else yield return null;
        }

        IEnumerator Shot(string name)
        {
            if (session.ShotsDirectory == null) yield break;
            yield return null;
            yield return new WaitForEndOfFrame();
            Texture2D texture = null;
            Texture2D decoded = null;
            try
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture == null || texture.width < 16 || texture.height < 16)
                {
                    Check(false, "screenshot missing: " + name);
                    yield break;
                }
                // Reject a solid/blank render even when the graphics driver returns a texture.
                var colors = texture.GetPixels32();
                bool varied = false;
                for (int i = 1; i < colors.Length; i += Math.Max(1, colors.Length / 4096))
                    if (!colors[i].Equals(colors[0])) { varied = true; break; }
                Check(varied, "screenshot contains rendered content: " + name);
                var path = Path.Combine(session.ShotsDirectory, name + ".png");
                File.WriteAllBytes(path, texture.EncodeToPNG());
                decoded = new Texture2D(2, 2);
                bool valid = decoded.LoadImage(File.ReadAllBytes(path))
                    && decoded.width == texture.width && decoded.height == texture.height;
                Check(valid, "screenshot PNG saved and decoded: " + name);
                if (valid && varied) screenshots++;
                Debug.Log("[AutoShots] " + name);
            }
            finally
            {
                if (texture != null) Destroy(texture);
                if (decoded != null) Destroy(decoded);
            }
        }

        IEnumerator Run()
        {
            var root = GameRoot.I;
            var c = root.Controller;
            yield return Pause(1.5f);
            UIKit.CloseAllModals();
            Check(c.State.Seed == 11 && c.State.ArmySize == 0 && c.State.BattlesPlayed == 0,
                "fresh deterministic village");
            yield return Shot("01_village");
            VerifyControls(root);

            var th = c.State.Village.TownHall;
            Check(th != null, "starter Town Hall exists");
            if (th != null)
            {
                var ground = root.Village.transform.Find("Ground").GetComponent<SpriteRenderer>();
                var townHallView = root.Village.View(th.Id);
                Check(townHallView != null && townHallView.enabled && townHallView.sortingOrder > ground.sortingOrder,
                    "Town Hall renders above ground");
                Screens.Popup.ShowBuilding(th.Id);
                yield return Pause(0.3f);
                yield return Shot("02_building_popup");
                Screens.Popup.Hide();
            }

            ShopPanel.Show(t => Screens.BuildMode.BeginPlace(t), 2);
            yield return Pause(0.3f);
            yield return Shot("03_shop_defenses");
            UIKit.CloseAllModals();
            Screens.BuildMode.BeginPlace(BuildingType.Cannon);
            yield return Pause(0.4f);
            yield return Shot("04_build_mode");
            Screens.BuildMode.Cancel();

            int cannons = c.State.Village.Count(BuildingType.Cannon);
            int goldBeforeBuild = c.State.Gold;
            bool attempted = false, placed = false;
            for (int y = 2; y < GameData.GridSize - 2 && !attempted; y++)
                for (int x = 2; x < GameData.GridSize - 2 && !attempted; x++)
                    if (c.State.Village.CanPlace(BuildingType.Cannon, new Int2(x, y)))
                    {
                        attempted = true;
                        placed = c.ConfirmPlace(BuildingType.Cannon, new Int2(x, y));
                    }
            Check(placed && c.State.Village.Count(BuildingType.Cannon) == cannons + 1, "place cannon");
            Check(c.State.Gold == goldBeforeBuild - GameData.Building(BuildingType.Cannon).Stats(1).CostGold,
                "placement spends gold");
            Check(Builders.Busy(c.State) == 1, "construction occupies builder");

            // Advance the test clock, not wall time, and let the real controller finish timers.
            session.Clock.Advance(3600);
            c.Tick();
            Check(Builders.Busy(c.State) == 0, "construction completes and releases builder");
            var mine = c.State.Village.FindFirst(BuildingType.GoldMine);
            int goldBeforeCollect = c.State.Gold;
            double storedBefore = mine != null ? mine.Stored : 0;
            int expectedCollection = Math.Min((int)storedBefore, Economy.GoldCap(c.State) - goldBeforeCollect);
            bool collected = mine != null && c.Collect(mine.Id);
            Check(collected && expectedCollection > 0 && c.State.Gold == goldBeforeCollect + expectedCollection
                && Math.Abs(mine.Stored - (storedBefore - expectedCollection)) < 0.001,
                "collect produced gold into storage");

            const int armyCount = 20;
            int elixirBefore = c.State.Elixir;
            bool queued = true;
            for (int i = 0; i < armyCount; i++) queued &= c.Train(TroopType.Barbarian);
            Check(queued && Training.QueuedCount(c.State) == armyCount
                && c.State.Elixir == elixirBefore - armyCount * Training.Cost(c.State, TroopType.Barbarian),
                "queue army and pay training costs");
            session.Clock.Advance(armyCount * GameData.Troop(TroopType.Barbarian).TrainSeconds);
            c.Tick();
            Check(c.State.Army(TroopType.Barbarian) == armyCount && c.State.TrainQueue.Count == 0,
                "training completes into army");

            yield return Pause(2.8f); // Let construction/collection toasts clear before panel captures.

            ArmyPanel.Show(0);
            yield return Pause(0.3f);
            yield return Shot("05_army");
            UIKit.CloseAllModals();
            AttackPanel.Show(sim => Screens.Battle.Begin(sim));
            yield return Pause(0.3f);
            yield return Shot("06_attack_menu");
            UIKit.CloseAllModals();
            yield return Raid(c, armyCount);

            LeaguePanel.Show(0); yield return Pause(0.3f); yield return Shot("10_league"); UIKit.CloseAllModals();
            ClanPanel.Show(1); yield return Pause(0.3f); yield return Shot("11_clans"); UIKit.CloseAllModals();
            SettingsPanel.Show(); yield return Pause(0.3f); yield return Shot("12_settings"); UIKit.CloseAllModals();
            DefenseLogToast.ShowLog(); yield return Pause(0.3f); yield return Shot("13_mail"); UIKit.CloseAllModals();

            // Leave both a trained troop and a pending queue in the persisted state.
            Check(c.Train(TroopType.Barbarian), "train after raid");
            session.Clock.Advance(GameData.Troop(TroopType.Barbarian).TrainSeconds);
            c.Tick();
            Check(c.Train(TroopType.Barbarian), "queue next troop before reload");
            c.SetName("Smoke Chief");
            c.SetAudio(false, false);
            c.Save();
            string expected = SaveSystem.Serialize(c.State);
            Check(File.ReadAllText(root.SavePath) == expected, "save writes current state to disk");
            var reloaded = new GameController(session.Clock,
                () => File.ReadAllText(root.SavePath), json => File.WriteAllText(root.SavePath, json));
            reloaded.Load();
            Check(!reloaded.IsNewPlayer && !ReferenceEquals(reloaded.State, c.State)
                && SaveSystem.Serialize(reloaded.State) == expected,
                "fresh controller reloads complete saved state");
            Check(File.ReadAllText(root.SavePath) == expected, "reload preserves saved state on disk");

            if (session.ShotsDirectory != null) Check(screenshots == 13, "all 13 screenshots validated");
            else Debug.Log("[Smoke] screenshots not requested (gameplay checks only)");
            Debug.Log("[AutoShots] done");
            session.Complete();
        }

        void VerifyControls(GameRoot root)
        {
            var router = root.Router;
            var cam = root.Cam;
            Vector2 center = cam.Cam.transform.position;
            float zoom = cam.Zoom;
            var screen = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            int taps = 0;
            Action<Int2, Vector2> countTap = (_, __) => taps++;
            router.Tap += countTap;
            try
            {
                router.BeginPointer(screen, true);
                router.MovePointer(screen + new Vector2(80, 0), 0.2f);
                router.EndPointer(screen);
                Check(taps == 0 && (Vector2)cam.Cam.transform.position == center,
                    "UI press cannot tap or pan the world");

                router.BeginPointer(screen, false);
                router.MovePointer(screen + new Vector2(80, 0), 0.2f);
                router.EndPointer(screen + new Vector2(80, 0));
                Check(taps == 0 && (Vector2)cam.Cam.transform.position != center,
                    "world drag pans without tapping");
                cam.Focus(center, zoom);
                var pivot = screen + new Vector2(100, 50);
                var beforeZoom = cam.ScreenToWorld(pivot);
                cam.ZoomAt(1, pivot);
                Check(cam.Zoom < zoom && Vector2.Distance(cam.ScreenToWorld(pivot), beforeZoom) < 0.01f,
                    "zoom preserves the pointer's world position");
                cam.Focus(center, zoom);

                ShopPanel.Show(t => Screens.BuildMode.BeginPlace(t));
                router.BeginPointer(screen, false);
                router.MovePointer(screen + new Vector2(80, 0), 0.2f);
                router.EndPointer(screen);
                Check(!router.Enabled && cam.Locked && taps == 0 && (Vector2)cam.Cam.transform.position == center,
                    "modal blocks world input and camera movement");
                UIKit.CloseAllModals();
                Check(router.Enabled && !cam.Locked, "closing modal restores world input");

                var th = root.Controller.State.Village.TownHall;
                var original = th.Origin;
                var pressCell = original + new Int2(1, 1);
                var press = cam.WorldToScreen(VillageRenderer.CellWorld(pressCell.X + 0.5f, pressCell.Y + 0.5f));
                router.BeginPointer(press, false);
                router.MovePointer(press, 0.6f); // Also covers a slow frame crossing the hold threshold.
                Check(Screens.BuildMode.Active && router.CaptureDrag, "long-press captures building movement");
                var destination = new Int2(10, 10);
                var target = cam.WorldToScreen(VillageRenderer.CellWorld(destination.X + 1.5f, destination.Y + 1.5f));
                router.MovePointer(target, 0.7f);
                router.EndPointer(target);
                Screens.BuildMode.Confirm();
                Check(th.Origin == destination && (Vector2)cam.Cam.transform.position == center,
                    "captured drag moves building without panning");
                root.Controller.MoveBuilding(th.Id, original);

                Screens.BuildMode.BeginPlace(BuildingType.Cannon); // Starts over the Town Hall.
                int count = root.Controller.State.Village.Count(BuildingType.Cannon);
                Screens.BuildMode.Confirm();
                Check(Screens.BuildMode.Active && root.Controller.State.Village.Count(BuildingType.Cannon) == count,
                    "overlapping placement is rejected");
                Screens.BuildMode.Cancel();
                Check(root.Mode == GameMode.Village, "cancel placement returns to village");
            }
            finally
            {
                router.Tap -= countTap;
                Screens.Popup.Hide();
                cam.Focus(center, zoom);
            }
        }

        IEnumerator Raid(GameController c, int armyCount)
        {
            int goldBefore = c.State.Gold, elixirBefore = c.State.Elixir;
            int trophiesBefore = c.State.Trophies, battlesBefore = c.State.BattlesPlayed;
            var battle = c.StartMatch(11);
            Check(battle != null, "start normal raid");
            if (battle == null) yield break;
            Check(c.State.Gold == goldBefore - GameData.MatchCostGold && c.State.ShieldUntilUnix == 0,
                "match charges gold and removes shield");
            goldBefore = c.State.Gold;
            BattleResult result = null;
            int reports = 0;
            Action<BattleResult> onFinished = r => { result = r; reports++; };
            Screens.Battle.Finished += onFinished;
            try
            {
                Screens.Battle.Begin(battle);
                yield return Pause(0.6f);
                yield return Shot("07_battle_start");
                bool deployed = battle.NearestDeployCell(new Int2(20, 2), out var cell);
                for (int i = 0; i < armyCount; i++)
                    deployed &= battle.TryDeploy(TroopType.Barbarian, cell.X + 0.5f, cell.Y + 0.5f);
                Check(deployed && battle.Started && battle.TroopsLeft == 0, "deploy trained army");
                yield return Pause(4f);
                yield return Shot("08_battle_fighting");

                // Accelerate fixed simulation steps while still allowing the renderer and
                // BattleController to consume events and report the actual result.
                for (int frame = 0; !battle.Ended && frame < 50; frame++)
                {
                    for (int step = 0; step < 10 && !battle.Ended; step++) battle.Step(0.5f);
                    yield return null;
                }
                Check(battle.Ended && !battle.Surrendered, "raid ends naturally within battle timer");
                float deadline = Time.realtimeSinceStartup + 5f;
                while (result == null && Time.realtimeSinceStartup < deadline) yield return null;
                Check(result != null && reports == 1 && c.ActiveBattle == null, "battle result reported exactly once");
                if (result != null)
                {
                    Check(!result.Practice && result.Stars >= 1 && result.TrophyDelta > 0,
                        "normal raid earns stars and trophies");
                    int expectedGold = Math.Min(battle.LootGold, Economy.GoldCap(c.State) - goldBefore);
                    int expectedElixir = Math.Min(battle.LootElixir, Economy.ElixirCap(c.State) - elixirBefore);
                    Check(expectedGold > 0 && expectedElixir > 0
                        && result.Gold == expectedGold && result.Elixir == expectedElixir
                        && c.State.Gold == goldBefore + expectedGold && c.State.Elixir == elixirBefore + expectedElixir,
                        "raid credits gold and elixir respecting storage caps");
                    Check(c.State.Trophies == trophiesBefore + result.TrophyDelta
                        && c.State.BattlesPlayed == battlesBefore + 1, "raid records trophies and battle count");
                    Check(result.TroopsUsed[(int)TroopType.Barbarian] == armyCount && c.State.ArmySize == 0,
                        "raid consumes deployed troops");
                    string settled = SaveSystem.Serialize(c.State);
                    Check(c.FinishBattle() == null && SaveSystem.Serialize(c.State) == settled,
                        "finishing battle twice cannot duplicate rewards");
                    Debug.Log($"[Smoke] result stars={result.Stars} percent={result.Percent} gold={result.Gold} elixir={result.Elixir}");
                }
                yield return Pause(2.8f);
                yield return Shot("09_result");
                UIKit.CloseAllModals();
                Screens.Battle.ReturnHome();
                Check(GameRoot.I.Mode == GameMode.Village, "return home after raid");
                yield return Pause(0.5f);
            }
            finally { Screens.Battle.Finished -= onFinished; }
        }
    }
}
