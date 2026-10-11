using UnityEngine;
using VillageClash.Core;

namespace VillageClash.UI
{
    /// <summary>Entry point: builds the whole game from code once the (empty) scene has loaded.</summary>
    public static class Bootstrap
    {
        public static Hud Hud { get; private set; }
        public static bool Booted { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Booted || GameRoot.I != null) return;
            var smoke = SmokeSession.StartFromCommandLine();
            if (smoke != null && smoke.Finished) return;
            Booted = true;
            var root = GameRoot.Create();
            UIKit.Init();
            Hud = Hud.Create();

            GameEvents.Toast += msg => UIKit.Toast(msg);
            GameEvents.LevelUp += lvl => { UIKit.Toast($"Level up! You reached level {lvl}", Palette.Hex("2d7a2d")); Sfx.Play(SfxId.Win); };
            GameEvents.BuildingCompleted += _ => Sfx.Play(SfxId.Build);
            GameEvents.ResourcesCollected += (_, g, e) =>
            {
                if (g > 0) UIKit.Toast($"+{Fmt.Num(g)} gold", Palette.Hex("8a6a10"));
                if (e > 0) UIKit.Toast($"+{Fmt.Num(e)} elixir", Palette.Hex("7a2a78"));
            };
            UIKit.ModalStateChanged += open =>
            {
                root.Router.Enabled = !open;
                root.Cam.Locked = open;
            };
            root.ModeChanged += m => Hud.gameObject.SetActive(m != GameMode.Battle);
            root.Router.Escape += () => { if (UIKit.AnyModalOpen) UIKit.TopModal.Close(); };

            Screens.Wire(root, Hud);
            if (root.Controller.IsNewPlayer) UIKit.Toast("Welcome, Chief! Tap the Shop to build your village.");
            else DefenseLogToast.ShowPending();
            string saveNotice = root.SaveLoadNotice;
            if (root.Controller.LoadNotice != null) saveNotice = (saveNotice == null ? "" : saveNotice + " ") + root.Controller.LoadNotice;
            if (root.Controller.LastSaveError != null && root.Controller.LoadNotice == null)
                saveNotice = "Could not save your village. Check disk space and file access, then try Save Now.";
            if (saveNotice != null) UIKit.Toast(saveNotice);
            Application.runInBackground = true;
            AutoShots.TryStart();
        }
    }
}
