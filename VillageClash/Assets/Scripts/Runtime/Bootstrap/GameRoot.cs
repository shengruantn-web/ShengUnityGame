using System;
using System.IO;
using UnityEngine;
using VillageClash.Core;

namespace VillageClash
{
    public enum GameMode { Village, Build, Battle }

    /// <summary>Scene singleton that owns the controller and the world-side presentation objects.</summary>
    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot I { get; private set; }
        public GameController Controller { get; private set; }
        public CameraRig Cam { get; private set; }
        public VillageRenderer Village { get; private set; }
        public InputRouter Router { get; private set; }
        public WorldLabels Labels { get; private set; }
        public GameMode Mode { get; private set; } = GameMode.Village;
        public event Action<GameMode> ModeChanged;

        float tickTimer;
        SaveFileStore saveStore;
        public string SaveLoadNotice => saveStore?.LoadNotice;
        public string SavePath => SmokeSession.Current != null
            ? SmokeSession.Current.SavePath
            : Path.Combine(Application.persistentDataPath, "village.json");

        public static GameRoot Create()
        {
            if (I != null) return I;
            var go = new GameObject("GameRoot");
            return go.AddComponent<GameRoot>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Application.targetFrameRate = 60;
            IClock clock = SmokeSession.Current != null && !SmokeSession.Current.Interactive
                ? (IClock)SmokeSession.Current.Clock : new SystemClock();
            saveStore = new SaveFileStore(SavePath);
            Controller = new GameController(clock, saveStore.Load, saveStore.Save);
            Controller.Load();
            Cam = CameraRig.Create();
            Village = new GameObject("Village").AddComponent<VillageRenderer>();
            Village.transform.SetParent(transform, false);
            Labels = WorldLabels.Create(transform);
            Router = gameObject.AddComponent<InputRouter>();
            Sfx.Init(transform);
            Sfx.SetMusic(Controller.State.MusicOn);
            Sfx.SetSfx(Controller.State.SfxOn);
            ShowHomeVillage();
        }

        public void ShowHomeVillage()
        {
            Village.Show(Controller.State.Village, true);
            var th = Controller.State.Village.TownHall;
            if (th != null)
            {
                IsoMath.FootprintCenter(th.Origin, th.Size, out var wx, out var wy);
                Cam.Focus(new Vector2(wx, wy), 7f);
            }
        }

        public void SetMode(GameMode mode)
        {
            if (Mode == mode) return;
            Mode = mode;
            ModeChanged?.Invoke(mode);
        }

        void Update()
        {
            if (Mode == GameMode.Battle) return;
            tickTimer += Time.unscaledDeltaTime;
            if (tickTimer >= 0.25f)
            {
                tickTimer = 0;
                Controller.Tick();
            }
        }

        void OnApplicationQuit() => Controller?.Save();
        void OnApplicationPause(bool paused) { if (paused) Controller?.Save(); }

    }
}
