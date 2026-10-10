using System;
using System.Collections.Generic;

namespace VillageClash.Core
{
    /// <summary>Deterministic real-time battle simulation on the enemy grid. Positions are in cell units.</summary>
    public sealed class BattleSim
    {
        public const float FixedDt = 0.05f;
        const float HitInterval = 1f;
        const float RangeSlack = 0.2f;
        const float TrapRadius = 0.9f;

        public readonly EnemyBase Enemy;
        public readonly List<BuildingState> Buildings = new List<BuildingState>();
        public readonly List<Unit> Units = new List<Unit>();
        public readonly List<Projectile> Projectiles = new List<Projectile>();
        public readonly List<BattleEvent> Events = new List<BattleEvent>();
        public readonly int[] Remaining;
        public readonly int[] Deployed;
        readonly int[] levels;
        readonly Random rng;
        readonly int size;
        readonly bool[] noDeploy;
        readonly float[] cost;
        bool costDirty = true;
        float accumulator;
        float endGrace;
        int nextUnitId = 1;

        public float Time;
        public bool Started, Ended, Surrendered;
        public int AvailableGold, AvailableElixir, LootGold, LootElixir;
        public int TotalDestructible, DestroyedCount;
        public bool TownHallDown;
        public int Size => size;

        public int Percent => TotalDestructible == 0 ? 100 : DestroyedCount * 100 / TotalDestructible;
        public int Stars => Progression.Stars(Percent, TownHallDown);
        public float TimeLeft => Math.Max(0, GameData.BattleSeconds - Time);
        public int TroopsLeft { get { int n = 0; foreach (var r in Remaining) n += r; return n; } }
        public int UnitsAlive { get { int n = 0; foreach (var u in Units) if (!u.Dead) n++; return n; } }

        public BattleSim(EnemyBase enemy, int[] army, int[] troopLevels, int seed)
        {
            Enemy = enemy;
            Remaining = (int[])army.Clone();
            Deployed = new int[army.Length];
            levels = (int[])troopLevels.Clone();
            rng = new Random(seed);
            size = enemy.Village.Grid.Size;
            noDeploy = new bool[size * size];
            cost = new float[size * size];

            AvailableGold = LootCalculator.Available(enemy.Gold, enemy.TownHallLevel);
            AvailableElixir = LootCalculator.Available(enemy.Elixir, enemy.TownHallLevel);
            float goldW = 0, elixirW = 0;
            foreach (var b in enemy.Village.Buildings)
            {
                var st = new BuildingState
                {
                    Building = b, Hp = b.Stats.Hp, MaxHp = b.Stats.Hp,
                    IsWall = b.Def.IsWall, IsTrap = b.Def.IsTrap, IsDefense = b.Def.IsDefense, IsTownHall = b.Type == BuildingType.TownHall,
                    CenterX = b.Origin.X + b.Size / 2f, CenterY = b.Origin.Y + b.Size / 2f,
                    Range = b.Stats.Range, MinRange = b.Stats.MinRange, Splash = b.Stats.SplashRadius, Damage = b.Stats.Damage,
                };
                if (st.IsTrap) { st.Hp = st.MaxHp = 1; }
                Buildings.Add(st);
                if (st.CountsForDestruction) TotalDestructible++;
                if (b.Type == BuildingType.TownHall || b.Type == BuildingType.GoldStorage || b.Type == BuildingType.GoldMine) goldW += LootCalculator.HolderWeight(b.Type);
                if (b.Type == BuildingType.TownHall || b.Type == BuildingType.ElixirStorage || b.Type == BuildingType.ElixirCollector) elixirW += LootCalculator.HolderWeight(b.Type);
                if (!st.IsTrap)
                    for (int y = b.Origin.Y - 1; y <= b.Origin.Y + b.Size; y++)
                        for (int x = b.Origin.X - 1; x <= b.Origin.X + b.Size; x++)
                            if (x >= 0 && y >= 0 && x < size && y < size) noDeploy[y * size + x] = true;
            }
            foreach (var st in Buildings)
            {
                var t = st.Building.Type;
                float w = LootCalculator.HolderWeight(t);
                if (w <= 0) continue;
                if (t == BuildingType.TownHall || t == BuildingType.GoldStorage || t == BuildingType.GoldMine) st.LootGold = (int)(AvailableGold * w / Math.Max(1e-3f, goldW));
                if (t == BuildingType.TownHall || t == BuildingType.ElixirStorage || t == BuildingType.ElixirCollector) st.LootElixir = (int)(AvailableElixir * w / Math.Max(1e-3f, elixirW));
            }
        }

        // ---- deployment -----------------------------------------------------
        public bool CanDeploy(Int2 cell)
        {
            if (Ended) return false;
            if (cell.X < 0 || cell.Y < 0 || cell.X >= size || cell.Y >= size) return false;
            return !noDeploy[cell.Y * size + cell.X];
        }

        public bool TryDeploy(TroopType type, float x, float y)
        {
            var cell = new Int2((int)Math.Floor(x), (int)Math.Floor(y));
            if (!CanDeploy(cell)) return false;
            int i = (int)type;
            if (Remaining[i] <= 0) return false;
            Remaining[i]--;
            Deployed[i]++;
            var def = GameData.Troop(type);
            int lvl = Math.Max(1, Math.Min(def.MaxLevel, levels[i]));
            var u = new Unit
            {
                Id = nextUnitId++, Type = type, Level = lvl, Def = def, Hp = def.Hp[lvl - 1], MaxHp = def.Hp[lvl - 1],
                X = Clamp(x + (float)(rng.NextDouble() - 0.5) * 0.6f, 0.1f, size - 0.1f),
                Y = Clamp(y + (float)(rng.NextDouble() - 0.5) * 0.6f, 0.1f, size - 0.1f),
            };
            Units.Add(u);
            Started = true;
            Events.Add(new BattleEvent { Kind = BattleEventKind.UnitDeployed, X = u.X, Y = u.Y, Unit = u });
            return true;
        }

        public void Surrender()
        {
            if (Ended) return;
            Surrendered = true;
            End();
        }

        // ---- simulation -----------------------------------------------------
        public void Step(float dt)
        {
            if (Ended) return;
            accumulator += Math.Min(dt, 0.5f);
            while (accumulator >= FixedDt && !Ended)
            {
                accumulator -= FixedDt;
                Tick(FixedDt);
            }
        }

        void Tick(float dt)
        {
            if (Started) Time += dt;
            if (!Started) return;

            foreach (var u in Units) if (!u.Dead) UpdateUnit(u, dt);
            foreach (var b in Buildings) if (b.Alive) { if (b.IsDefense) UpdateDefense(b, dt); else if (b.IsTrap) UpdateTrap(b); }
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                var p = Projectiles[i];
                UpdateProjectile(p, dt);
                if (p.Done) Projectiles.RemoveAt(i);
            }

            if (Percent >= 100) { End(); return; }
            if (Time >= GameData.BattleSeconds) { End(); return; }
            if (UnitsAlive == 0 && TroopsLeft == 0)
            {
                endGrace += dt;
                if (endGrace >= 1.5f) End();
            }
            else endGrace = 0;
        }

        void End()
        {
            if (Ended) return;
            Ended = true;
            Events.Add(new BattleEvent { Kind = BattleEventKind.Ended });
        }

        public BattleResult Result()
        {
            return new BattleResult
            {
                Stars = Stars, Percent = Percent, Gold = LootGold, Elixir = LootElixir,
                TrophyDelta = Enemy.Practice ? 0 : Progression.TrophyDelta(Stars),
                TownHallDown = TownHallDown, Practice = Enemy.Practice, Surrendered = Surrendered,
                TroopsUsed = (int[])Deployed.Clone(), Duration = Time, EnemyName = Enemy.Name,
            };
        }

        // ---- units ------------------------------------------------------------
        void UpdateUnit(Unit u, float dt)
        {
            u.Attacking = false;
            if (u.Target == null || !u.Target.Alive) AcquireTarget(u);
            if (u.Target == null) return;
            if (u.Blocker != null && !u.Blocker.Alive) u.Blocker = null;

            var at = u.Blocker ?? u.Target;
            float range = Math.Max(0.5f, u.Def.Range);
            if (at.DistanceTo(u.X, u.Y) <= range + RangeSlack)
            {
                u.Attacking = true;
                u.FacingX = at.CenterX >= u.X ? 1 : -1;
                u.AttackCd -= dt;
                if (u.AttackCd <= 0)
                {
                    u.AttackCd += HitInterval;
                    Attack(u, at);
                }
                return;
            }
            u.AttackCd = Math.Min(u.AttackCd, 0.3f);

            if (u.Path == null || u.PathIndex >= u.Path.Count)
            {
                if (u.Path != null && u.PathIndex >= u.Path.Count && u.Path.Count > 0)
                {
                    // Path exhausted but still not in range: repath.
                    u.Path = null;
                }
                if (u.Path == null) ComputePath(u, u.Target);
                if (u.Path == null || u.PathIndex >= u.Path.Count) return; // unreachable or already there; wait
            }
            var next = u.Path[u.PathIndex];
            var wall = WallAt(next);
            if (wall != null && wall != u.Target)
            {
                u.Blocker = wall;
                return; // next tick we attack the blocker (adjacent)
            }
            float tx = next.X + 0.5f, ty = next.Y + 0.5f;
            float dx = tx - u.X, dy = ty - u.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            float step = u.Def.Speed * dt;
            if (dist <= step) { u.X = tx; u.Y = ty; u.PathIndex++; }
            else { u.X += dx / dist * step; u.Y += dy / dist * step; }
            if (Math.Abs(dx) > 0.01f) u.FacingX = dx > 0 ? 1 : -1;
        }

        BuildingState WallAt(Int2 cell)
        {
            int id = Enemy.Village.Grid.OccupantAt(cell);
            if (id == GridMap.Empty) return null;
            foreach (var b in Buildings) if (b.Building.Id == id) return b.IsWall && b.Alive ? b : null;
            return null;
        }

        void AcquireTarget(Unit u)
        {
            u.Blocker = null;
            u.Path = null;
            BuildingState best = null;
            float bestD = float.MaxValue;
            bool preferClass = false;
            switch (u.Def.Preference)
            {
                case TargetPreference.Defenses: foreach (var b in Buildings) if (b.Alive && b.IsDefense) { preferClass = true; break; } break;
                case TargetPreference.Resources: foreach (var b in Buildings) if (b.Alive && b.Building.Def.Category == BuildingCategory.Resource && !b.IsTrap && b.Building.Type != BuildingType.BuilderHut) { preferClass = true; break; } break;
                case TargetPreference.Walls: foreach (var b in Buildings) if (b.Alive && b.IsWall) { preferClass = true; break; } break;
            }
            foreach (var b in Buildings)
            {
                if (!b.Alive || b.IsTrap) continue;
                if (preferClass)
                {
                    if (u.Def.Preference == TargetPreference.Defenses && !b.IsDefense) continue;
                    if (u.Def.Preference == TargetPreference.Resources && (b.Building.Def.Category != BuildingCategory.Resource || b.Building.Type == BuildingType.BuilderHut)) continue;
                    if (u.Def.Preference == TargetPreference.Walls && !b.IsWall) continue;
                }
                else if (b.IsWall) continue;
                float d = b.DistanceTo(u.X, u.Y);
                if (d < bestD) { bestD = d; best = b; }
            }
            u.Target = best;
            if (best != null) ComputePath(u, best);
        }

        void RebuildCost()
        {
            for (int i = 0; i < cost.Length; i++) cost[i] = 0;
            foreach (var b in Buildings)
            {
                if (!b.Alive || b.IsTrap) continue;
                var o = b.Building.Origin; int s = b.Building.Size;
                float c = b.IsWall ? AStar.WallCost : -1f;
                for (int y = o.Y; y < o.Y + s; y++)
                    for (int x = o.X; x < o.X + s; x++)
                        cost[y * size + x] = c;
            }
            costDirty = false;
        }

        void ComputePath(Unit u, BuildingState target)
        {
            if (costDirty) RebuildCost();
            float range = Math.Max(0.5f, u.Def.Range);
            var start = u.Cell;
            start = new Int2(Math.Max(0, Math.Min(size - 1, start.X)), Math.Max(0, Math.Min(size - 1, start.Y)));
            // The unit's own cell might be inside a destroyed footprint or wall; treat as free.
            float saved = cost[start.Y * size + start.X];
            cost[start.Y * size + start.X] = 0;
            u.Path = AStar.FindPath(size, cost, start,
                c => cost[c.Y * size + c.X] == 0 && target.DistanceTo(c.X + 0.5f, c.Y + 0.5f) <= range + 0.05f,
                c => Math.Max(0, target.DistanceTo(c.X + 0.5f, c.Y + 0.5f) - range));
            cost[start.Y * size + start.X] = saved;
            u.PathIndex = 0;
        }

        void Attack(Unit u, BuildingState b)
        {
            if (u.Type == TroopType.WallBreaker)
            {
                Explode(u);
                return;
            }
            Damage(b, u.Dps, u);
        }

        void Explode(Unit u)
        {
            int dmg = u.Dps;
            float mult = u.Def.PreferenceMultiplier;
            Events.Add(new BattleEvent { Kind = BattleEventKind.Explosion, X = u.X, Y = u.Y, Unit = u });
            foreach (var b in Buildings)
            {
                if (!b.Alive || b.IsTrap) continue;
                if (b.DistanceTo(u.X, u.Y) > 1.0f) continue;
                Damage(b, b.IsWall ? (int)(dmg * mult) : dmg * 2, u);
            }
            u.Hp = 0;
            Events.Add(new BattleEvent { Kind = BattleEventKind.UnitDied, X = u.X, Y = u.Y, Unit = u });
        }

        void Damage(BuildingState b, int amount, Unit by)
        {
            if (!b.Alive) return;
            b.Hp -= amount;
            Events.Add(new BattleEvent { Kind = BattleEventKind.BuildingHit, X = b.CenterX, Y = b.CenterY, Building = b, Unit = by });
            if (b.Hp <= 0)
            {
                b.Hp = 0;
                costDirty = true;
                if (b.CountsForDestruction) DestroyedCount++;
                if (b.IsTownHall) TownHallDown = true;
                LootGold += b.LootGold; LootElixir += b.LootElixir;
                Events.Add(new BattleEvent { Kind = BattleEventKind.BuildingDestroyed, X = b.CenterX, Y = b.CenterY, Building = b });
                foreach (var u in Units) { if (u.Target == b) u.Target = null; if (u.Blocker == b) u.Blocker = null; }
            }
        }

        // ---- defenses and traps ------------------------------------------------
        void UpdateDefense(BuildingState d, float dt)
        {
            d.Cooldown -= dt;
            if (d.Target != null && (d.Target.Dead || !InRange(d, d.Target))) d.Target = null;
            if (d.Target == null)
            {
                float best = float.MaxValue;
                foreach (var u in Units)
                {
                    if (u.Dead || !InRange(d, u)) continue;
                    float dist = Dist(d.CenterX, d.CenterY, u.X, u.Y);
                    if (dist < best) { best = dist; d.Target = u; }
                }
            }
            if (d.Target == null || d.Cooldown > 0) return;
            d.Cooldown = Math.Max(0.1f, d.Building.Stats.AttackCooldown);
            var kind = d.Building.Type == BuildingType.Mortar ? ProjectileKind.Shell : d.Building.Type == BuildingType.ArcherTower ? ProjectileKind.Arrow : ProjectileKind.Cannonball;
            var p = new Projectile
            {
                Kind = kind, X = d.CenterX, Y = d.CenterY, StartX = d.CenterX, StartY = d.CenterY,
                TX = d.Target.X, TY = d.Target.Y, Damage = d.Damage, Splash = d.Splash,
                Speed = kind == ProjectileKind.Shell ? 5f : kind == ProjectileKind.Arrow ? 14f : 10f,
                TargetUnit = kind == ProjectileKind.Shell ? null : d.Target,
            };
            Projectiles.Add(p);
            Events.Add(new BattleEvent { Kind = BattleEventKind.Shot, X = d.CenterX, Y = d.CenterY, Building = d });
        }

        bool InRange(BuildingState d, Unit u)
        {
            float dist = Dist(d.CenterX, d.CenterY, u.X, u.Y);
            return dist <= d.Range && dist >= d.MinRange;
        }

        void UpdateTrap(BuildingState t)
        {
            if (t.Triggered) return;
            Unit trigger = null;
            foreach (var u in Units) if (!u.Dead && Dist(t.CenterX, t.CenterY, u.X, u.Y) <= TrapRadius) { trigger = u; break; }
            if (trigger == null) return;
            t.Triggered = true;
            t.Hp = 0;
            Events.Add(new BattleEvent { Kind = BattleEventKind.TrapTriggered, X = t.CenterX, Y = t.CenterY, Building = t });
            if (t.Building.Type == BuildingType.Bomb)
            {
                foreach (var u in Units)
                    if (!u.Dead && Dist(t.CenterX, t.CenterY, u.X, u.Y) <= Math.Max(1f, t.Splash)) Hurt(u, t.Damage);
            }
            else
            {
                int housing = 0;
                foreach (var u in Units)
                {
                    if (u.Dead || Dist(t.CenterX, t.CenterY, u.X, u.Y) > 1.2f) continue;
                    if (housing + u.Def.Housing > 15) continue;
                    housing += u.Def.Housing;
                    u.Sprung = true;
                    u.Hp = 0;
                    Events.Add(new BattleEvent { Kind = BattleEventKind.Sprung, X = u.X, Y = u.Y, Unit = u });
                }
            }
        }

        void UpdateProjectile(Projectile p, float dt)
        {
            if (p.TargetUnit != null && !p.TargetUnit.Dead) { p.TX = p.TargetUnit.X; p.TY = p.TargetUnit.Y; }
            float dx = p.TX - p.X, dy = p.TY - p.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            float step = p.Speed * dt;
            float total = Dist(p.StartX, p.StartY, p.TX, p.TY);
            p.Progress = total <= 0.01f ? 1 : Math.Min(1, 1 - dist / total);
            if (dist <= step)
            {
                p.X = p.TX; p.Y = p.TY; p.Done = true;
                if (p.Splash > 0)
                {
                    Events.Add(new BattleEvent { Kind = BattleEventKind.Explosion, X = p.X, Y = p.Y });
                    foreach (var u in Units) if (!u.Dead && Dist(p.X, p.Y, u.X, u.Y) <= p.Splash) Hurt(u, p.Damage);
                }
                else if (p.TargetUnit != null && !p.TargetUnit.Dead && Dist(p.X, p.Y, p.TargetUnit.X, p.TargetUnit.Y) < 0.6f)
                {
                    Hurt(p.TargetUnit, p.Damage);
                }
                return;
            }
            p.X += dx / dist * step; p.Y += dy / dist * step;
        }

        void Hurt(Unit u, int dmg)
        {
            if (u.Dead) return;
            u.Hp -= dmg;
            Events.Add(new BattleEvent { Kind = BattleEventKind.UnitHit, X = u.X, Y = u.Y, Unit = u });
            if (u.Hp <= 0)
            {
                u.Hp = 0;
                Events.Add(new BattleEvent { Kind = BattleEventKind.UnitDied, X = u.X, Y = u.Y, Unit = u });
                foreach (var b in Buildings) if (b.Target == u) b.Target = null;
            }
        }

        static float Dist(float ax, float ay, float bx, float by) { float dx = ax - bx, dy = ay - by; return (float)Math.Sqrt(dx * dx + dy * dy); }
        static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;

        /// <summary>Finds a deployable cell nearest to the given cell (spiral search). Returns false if none.</summary>
        public bool NearestDeployCell(Int2 from, out Int2 cell)
        {
            for (int r = 0; r < size; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                        var c = new Int2(from.X + dx, from.Y + dy);
                        if (CanDeploy(c)) { cell = c; return true; }
                    }
            cell = Int2.Zero;
            return false;
        }
    }
}
