using System;

namespace VillageClash.Core
{
    /// <summary>Simple static event hub between Core and presentation layers.</summary>
    public static class GameEvents
    {
        /// <summary>Fired after any state mutation that the UI should reflect.</summary>
        public static event Action StateChanged;
        /// <summary>Short message for the player (error or confirmation).</summary>
        public static event Action<string> Toast;
        /// <summary>A building finished constructing/upgrading (id).</summary>
        public static event Action<int> BuildingCompleted;
        /// <summary>Resources were collected at a building (id, gold, elixir).</summary>
        public static event Action<int, int, int> ResourcesCollected;
        /// <summary>Player level went up (new level).</summary>
        public static event Action<int> LevelUp;

        public static void RaiseStateChanged() => StateChanged?.Invoke();
        public static void RaiseToast(string msg) { if (!string.IsNullOrEmpty(msg)) Toast?.Invoke(msg); }
        public static void RaiseBuildingCompleted(int id) => BuildingCompleted?.Invoke(id);
        public static void RaiseResourcesCollected(int id, int gold, int elixir) => ResourcesCollected?.Invoke(id, gold, elixir);
        public static void RaiseLevelUp(int level) => LevelUp?.Invoke(level);

        public static void ClearAll()
        {
            StateChanged = null; Toast = null; BuildingCompleted = null; ResourcesCollected = null; LevelUp = null;
        }
    }
}
