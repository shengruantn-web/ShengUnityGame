using System.Collections.Generic;
using UnityEngine;
using VillageClash.Core;

namespace VillageClash
{
    /// <summary>Draws a VillageModel: ground, buildings, obstacles. Reconciles every frame so model mutations show up automatically.</summary>
    public sealed class VillageRenderer : MonoBehaviour
    {
        public VillageModel Model { get; private set; }
        /// <summary>Home village: show construction timers and collect bubbles.</summary>
        public bool Interactive;
        public int HiddenId = -1;
        public int HighlightedId = -1;
        public bool ShowTraps = true;

        readonly Dictionary<int, SpriteRenderer> buildings = new Dictionary<int, SpriteRenderer>();
        readonly Dictionary<int, SpriteRenderer> obstacles = new Dictionary<int, SpriteRenderer>();
        readonly HashSet<int> destroyed = new HashSet<int>();
        readonly List<int> tmp = new List<int>();
        SpriteRenderer ground;

        public static int SortOrder(float cx, float cy) => -(int)((cx + cy) * 50f);

        public static Vector3 FootprintWorld(Int2 origin, int size)
        {
            IsoMath.FootprintCenter(origin, size, out var wx, out var wy);
            return new Vector3(wx, wy, 0);
        }

        public static Vector3 CellWorld(float cx, float cy)
        {
            IsoMath.CellToWorld(cx, cy, out var wx, out var wy);
            return new Vector3(wx, wy, 0);
        }

        /// <summary>A point above the building's roof for labels.</summary>
        public static Vector3 TopOf(BuildingInstance b) => FootprintWorld(b.Origin, b.Size) + new Vector3(0, b.Size * 0.25f + 0.55f, 0);

        public void Show(VillageModel model, bool interactive)
        {
            Clear();
            Model = model;
            Interactive = interactive;
            int size = model.Grid.Size;
            ground = new GameObject("Ground").AddComponent<SpriteRenderer>();
            ground.transform.SetParent(transform, false);
            ground.sprite = SpriteFactory.Ground(size);
            ground.transform.position = new Vector3(0, size * 0.25f, 0);
            ground.sortingOrder = -100000;
            Sync();
        }

        public void Clear()
        {
            foreach (Transform c in transform) Destroy(c.gameObject);
            buildings.Clear(); obstacles.Clear(); destroyed.Clear();
            Model = null; ground = null; HiddenId = -1; HighlightedId = -1;
        }

        public void SetDestroyed(int id, bool d) { if (d) destroyed.Add(id); else destroyed.Remove(id); }
        public bool IsDestroyed(int id) => destroyed.Contains(id);

        public SpriteRenderer View(int id) => buildings.TryGetValue(id, out var v) ? v : null;

        void LateUpdate() { if (Model != null) Sync(); }

        public void Sync()
        {
            // buildings
            tmp.Clear();
            foreach (var kv in buildings) if (Model.Find(kv.Key) == null) tmp.Add(kv.Key);
            foreach (var id in tmp) { Destroy(buildings[id].gameObject); buildings.Remove(id); }
            foreach (var b in Model.Buildings)
            {
                if (!buildings.TryGetValue(b.Id, out var sr))
                {
                    sr = new GameObject("B" + b.Id).AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(transform, false);
                    buildings[b.Id] = sr;
                }
                bool isTrap = b.Def.IsTrap;
                bool dead = destroyed.Contains(b.Id);
                sr.sprite = dead ? SpriteFactory.Rubble(b.Size) : SpriteFactory.Building(b.Type, b.Level, false);
                sr.transform.position = FootprintWorld(b.Origin, b.Size);
                sr.sortingOrder = SortOrder(b.CenterX, b.CenterY) + (isTrap ? -5 : 0);
                sr.enabled = b.Id != HiddenId && (ShowTraps || !isTrap);
                Color c = Color.white;
                if (b.Id == HighlightedId) c = new Color(1.15f, 1.15f, 0.9f);
                else if (b.Constructing) c = new Color(0.8f, 0.8f, 0.8f);
                sr.color = c;

                if (Interactive && GameRoot.I != null)
                {
                    var labels = GameRoot.I.Labels;
                    var ctrl = GameRoot.I.Controller;
                    long now = ctrl.Now;
                    if (b.Constructing)
                    {
                        long total = System.Math.Max(1, b.FinishAtUnix - b.StartedAtUnix);
                        long rem = Builders.Remaining(b, now);
                        labels.Bar("bt" + b.Id, TopOf(b), 1f - rem / (float)total, Palette.Green, Fmt.Time(rem));
                    }
                    else if (Economy.IsProducer(b.Type))
                    {
                        int cap = b.Stats.Capacity;
                        if (b.Stored >= System.Math.Max(10, cap * 0.08))
                        {
                            int id = b.Id;
                            labels.Bubble("cb" + b.Id, TopOf(b), SpriteFactory.Icon(b.Type == BuildingType.GoldMine ? "gold" : "elixir"),
                                () => { if (ctrl.Collect(id)) Sfx.Play(SfxId.Collect); });
                        }
                    }
                }
            }
            // obstacles
            tmp.Clear();
            foreach (var kv in obstacles) if (Model.FindObstacle(kv.Key) == null) tmp.Add(kv.Key);
            foreach (var id in tmp) { Destroy(obstacles[id].gameObject); obstacles.Remove(id); }
            foreach (var o in Model.Obstacles)
            {
                if (!obstacles.TryGetValue(o.Id, out var sr))
                {
                    sr = new GameObject("O" + o.Id).AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(transform, false);
                    sr.sprite = SpriteFactory.Obstacle(o.Kind);
                    obstacles[o.Id] = sr;
                }
                sr.transform.position = FootprintWorld(o.Origin, o.Size);
                sr.sortingOrder = SortOrder(o.Origin.X + o.Size * 0.5f, o.Origin.Y + o.Size * 0.5f);
                sr.color = o.Removing ? new Color(0.8f, 0.8f, 0.8f) : Color.white;
                if (Interactive && o.Removing && GameRoot.I != null)
                {
                    long rem = System.Math.Max(0, o.FinishAtUnix - GameRoot.I.Controller.Now);
                    int total = GameData.ObstacleRemoveSeconds(o.Kind);
                    GameRoot.I.Labels.Bar("ot" + o.Id, FootprintWorld(o.Origin, o.Size) + new Vector3(0, 0.9f, 0), 1f - rem / (float)total, Palette.Green, Fmt.Time(rem));
                }
            }
        }
    }
}
