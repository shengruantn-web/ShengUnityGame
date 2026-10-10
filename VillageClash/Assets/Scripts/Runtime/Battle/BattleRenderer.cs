using System.Collections.Generic;
using UnityEngine;
using VillageClash.Core;

namespace VillageClash
{
    /// <summary>Draws units, projectiles, hit effects, HP bars, loot pops and the no-deploy zone for a running battle.</summary>
    public sealed class BattleRenderer : MonoBehaviour
    {
        BattleController ctrl;
        BattleSim Sim => ctrl.Sim;
        GameRoot R => GameRoot.I;

        readonly Dictionary<int, SpriteRenderer> units = new Dictionary<int, SpriteRenderer>();
        readonly Dictionary<Projectile, SpriteRenderer> shots = new Dictionary<Projectile, SpriteRenderer>();
        readonly List<Projectile> deadShots = new List<Projectile>();
        readonly List<int> deadUnits = new List<int>();

        sealed class Fx { public SpriteRenderer Sr; public float T, Life, Scale; public Vector3 Pos; public bool Rise; }
        readonly List<Fx> fx = new List<Fx>();
        readonly Stack<SpriteRenderer> fxPool = new Stack<SpriteRenderer>();

        sealed class Pop { public string Key; public string Text; public Color Color; public Vector3 World; public float T; }
        readonly List<Pop> pops = new List<Pop>();
        int popId;

        Transform noDeployRoot; float noDeployFlash; readonly List<SpriteRenderer> noDeployTiles = new List<SpriteRenderer>();

        public void Init(BattleController c)
        {
            ctrl = c;
            BuildNoDeploy();
        }

        void BuildNoDeploy()
        {
            noDeployRoot = new GameObject("NoDeploy").transform;
            noDeployRoot.SetParent(transform, false);
            int size = Sim.Size;
            var col = Palette.Alpha(Palette.Red, 0.28f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    if (Sim.CanDeploy(new Int2(x, y))) continue;
                    var sr = new GameObject("nd").AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(noDeployRoot, false);
                    sr.sprite = SpriteFactory.Tile;
                    sr.color = col;
                    sr.sortingOrder = VillageRenderer.OverlayOrder;
                    sr.transform.position = VillageRenderer.CellWorld(x + 0.5f, y + 0.5f);
                    noDeployTiles.Add(sr);
                }
            noDeployRoot.gameObject.SetActive(false);
        }

        public void FlashNoDeploy() { noDeployFlash = 0.6f; }

        public void OnEvent(BattleEvent e)
        {
            var w = VillageRenderer.CellWorld(e.X, e.Y);
            switch (e.Kind)
            {
                case BattleEventKind.UnitDeployed:
                    Puff(w, Palette.Alpha(Color.white, 0.6f), 0.35f, 0.25f);
                    break;
                case BattleEventKind.UnitDied:
                    Puff(w, Palette.Alpha(Palette.Red, 0.6f), 0.4f, 0.3f);
                    Sfx.Play(SfxId.Hit, 0.5f);
                    break;
                case BattleEventKind.UnitHit:
                    Puff(w + new Vector3(0, 0.2f, 0), Palette.Alpha(Palette.Orange, 0.7f), 0.2f, 0.15f);
                    break;
                case BattleEventKind.BuildingHit:
                    Puff(w + new Vector3(0, 0.1f, 0), Palette.Alpha(Color.white, 0.5f), 0.15f, 0.12f);
                    if (Random.value < 0.25f) Sfx.Play(SfxId.Hit, 0.35f);
                    break;
                case BattleEventKind.BuildingDestroyed:
                    if (e.Building != null)
                    {
                        R.Village.SetDestroyed(e.Building.Building.Id, true);
                        Puff(w, Palette.Alpha(Palette.StoneDark, 0.8f), 1.2f * e.Building.Building.Size, 0.5f);
                        Puff(w, Palette.Alpha(Palette.Orange, 0.8f), 0.7f * e.Building.Building.Size, 0.3f);
                        if (e.Building.LootGold > 0) AddPop(w, $"+{Fmt.Num(e.Building.LootGold)}", Palette.Gold);
                        if (e.Building.LootElixir > 0) AddPop(w + new Vector3(0, 0.35f, 0), $"+{Fmt.Num(e.Building.LootElixir)}", Palette.Elixir);
                        if (e.Building.IsWall) Sfx.Play(SfxId.Hit, 0.6f); else Sfx.Play(SfxId.Destroy);
                        if (e.Building.IsTownHall) Sfx.Play(SfxId.Star);
                    }
                    break;
                case BattleEventKind.TrapTriggered:
                    if (e.Building != null) R.Village.SetDestroyed(e.Building.Building.Id, true);
                    Puff(w, Palette.Alpha(Palette.Orange, 0.9f), 1.6f, 0.45f);
                    Sfx.Play(SfxId.Explosion);
                    break;
                case BattleEventKind.Explosion:
                    Puff(w, Palette.Alpha(Palette.Orange, 0.9f), 1.1f, 0.4f);
                    Puff(w, Palette.Alpha(Palette.GreyDark, 0.6f), 1.6f, 0.6f);
                    Sfx.Play(SfxId.Explosion, 0.8f);
                    break;
                case BattleEventKind.Shot:
                    Sfx.Play(SfxId.Shot, 0.5f);
                    break;
                case BattleEventKind.Sprung:
                    Puff(w, Palette.Alpha(Color.white, 0.8f), 0.8f, 0.35f);
                    Sfx.Play(SfxId.Deploy, 0.6f);
                    break;
                case BattleEventKind.Ended:
                    break;
            }
        }

        void Puff(Vector3 at, Color c, float scale, float life)
        {
            var sr = fxPool.Count > 0 ? fxPool.Pop() : NewFx();
            sr.gameObject.SetActive(true);
            sr.color = c;
            sr.transform.position = at;
            sr.transform.localScale = Vector3.one * 0.1f;
            sr.sortingOrder = VillageRenderer.EffectsOrder;
            fx.Add(new Fx { Sr = sr, T = 0, Life = life, Scale = scale, Pos = at });
        }

        SpriteRenderer NewFx()
        {
            var sr = new GameObject("fx").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            sr.sprite = SpriteFactory.Circle;
            return sr;
        }

        void AddPop(Vector3 w, string text, Color color)
        {
            pops.Add(new Pop { Key = "lp" + (popId++), Text = text, Color = color, World = w, T = 0 });
        }

        void LateUpdate()
        {
            if (ctrl == null || Sim == null) return;
            float dt = Time.unscaledDeltaTime;

            // units
            deadUnits.Clear();
            foreach (var u in Sim.Units)
            {
                if (!units.TryGetValue(u.Id, out var sr))
                {
                    sr = new GameObject("u" + u.Id).AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(transform, false);
                    sr.sprite = SpriteFactory.Troop(u.Type);
                    units[u.Id] = sr;
                }
                if (u.Dead) { sr.enabled = false; continue; }
                var p = VillageRenderer.CellWorld(u.X, u.Y);
                sr.transform.position = p;
                sr.sortingOrder = VillageRenderer.SortOrder(u.X, u.Y) + 5;
                sr.flipX = u.FacingX < 0;
                float bob = u.Attacking ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 18f)) * 0.08f : (u.Path != null ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 10f + u.Id)) * 0.05f : 0);
                sr.transform.position = p + new Vector3(0, bob, 0);
                if (u.Hp < u.MaxHp)
                    R.Labels.Bar("u" + u.Id, p + new Vector3(0, 0.75f, 0), u.Hp / (float)u.MaxHp, Palette.Green, null, 50);
            }

            // buildings hp
            foreach (var b in Sim.Buildings)
            {
                if (b.Destroyed || b.IsTrap || b.Hp >= b.MaxHp) continue;
                R.Labels.Bar("bh" + b.Building.Id, VillageRenderer.TopOf(b.Building), b.Hp / (float)b.MaxHp, Palette.Red, null, b.IsWall ? 40 : 90);
            }

            // projectiles
            deadShots.Clear();
            foreach (var kv in shots) if (kv.Key.Done || !Sim.Projectiles.Contains(kv.Key)) deadShots.Add(kv.Key);
            foreach (var d in deadShots) { Destroy(shots[d].gameObject); shots.Remove(d); }
            foreach (var pr in Sim.Projectiles)
            {
                if (!shots.TryGetValue(pr, out var sr))
                {
                    sr = new GameObject("shot").AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(transform, false);
                    sr.sprite = SpriteFactory.Projectile(pr.Kind);
                    shots[pr] = sr;
                }
                var w = VillageRenderer.CellWorld(pr.X, pr.Y);
                float arc = pr.Kind == ProjectileKind.Shell ? Mathf.Sin(Mathf.Clamp01(pr.Progress) * Mathf.PI) * 2.2f : pr.Kind == ProjectileKind.Arrow ? Mathf.Sin(Mathf.Clamp01(pr.Progress) * Mathf.PI) * 0.6f : 0.3f;
                sr.transform.position = w + new Vector3(0, 0.5f + arc, 0);
                sr.sortingOrder = VillageRenderer.ProjectileOrder;
                if (pr.Kind == ProjectileKind.Arrow)
                {
                    var dir = new Vector2(pr.TX - pr.X, pr.TY - pr.Y);
                    IsoMath.CellToWorld(dir.x, dir.y, out float dx, out float dy);
                    sr.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
                }
            }

            // fx
            for (int i = fx.Count - 1; i >= 0; i--)
            {
                var f = fx[i]; f.T += dt;
                float k = f.T / f.Life;
                if (k >= 1) { f.Sr.gameObject.SetActive(false); fxPool.Push(f.Sr); fx.RemoveAt(i); continue; }
                f.Sr.transform.localScale = Vector3.one * Mathf.Lerp(0.15f, f.Scale, Mathf.Sqrt(k));
                var c = f.Sr.color; c.a = Mathf.Lerp(c.a, 0, dt / Mathf.Max(0.01f, f.Life - f.T)); f.Sr.color = c;
            }

            // loot pops
            for (int i = pops.Count - 1; i >= 0; i--)
            {
                var p = pops[i]; p.T += dt;
                if (p.T > 1.3f) { pops.RemoveAt(i); continue; }
                R.Labels.Label(p.Key, p.World + new Vector3(0, 0.6f + p.T * 0.9f, 0), p.Text, Palette.Alpha(p.Color, 1f - Mathf.Clamp01((p.T - 0.8f) / 0.5f)), 26);
            }

            // no-deploy overlay: visible while pressing or briefly after an invalid tap
            noDeployFlash -= dt;
            bool show = !Sim.Ended && (ctrl.Pressing || noDeployFlash > 0);
            if (noDeployRoot.gameObject.activeSelf != show) noDeployRoot.gameObject.SetActive(show);
        }
    }
}
