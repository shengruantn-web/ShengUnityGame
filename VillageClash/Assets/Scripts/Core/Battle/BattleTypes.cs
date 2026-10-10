using System;
using System.Collections.Generic;

namespace VillageClash.Core
{
    public sealed class BuildingState
    {
        public BuildingInstance Building;
        public int Hp, MaxHp;
        public float Cooldown;
        public Unit Target;
        public int LootGold, LootElixir;
        public bool Triggered;
        public bool IsWall, IsTrap, IsDefense, IsTownHall;
        public float CenterX, CenterY;
        public bool Destroyed => Hp <= 0;
        public bool CountsForDestruction => !IsWall && !IsTrap;
        public bool Alive => !Destroyed && !(IsTrap && Triggered);
        public float Range, MinRange, Splash;
        public int Damage;

        public float DistanceTo(float x, float y)
        {
            var o = Building.Origin; int s = Building.Size;
            float dx = Math.Max(0, Math.Max(o.X - x, x - (o.X + s)));
            float dy = Math.Max(0, Math.Max(o.Y - y, y - (o.Y + s)));
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public sealed class Unit
    {
        public int Id;
        public TroopType Type;
        public int Level;
        public TroopDef Def;
        public int Hp, MaxHp;
        public float X, Y;
        public float FacingX = 1;
        public BuildingState Target;
        public BuildingState Blocker;
        public List<Int2> Path;
        public int PathIndex;
        public float AttackCd;
        public bool Attacking;
        public bool Sprung;
        public bool Dead => Hp <= 0;
        public int Dps => Def.Dps[Level - 1];
        public Int2 Cell => new Int2((int)Math.Floor(X), (int)Math.Floor(Y));
    }

    public enum ProjectileKind { Cannonball, Arrow, Shell }

    public sealed class Projectile
    {
        public ProjectileKind Kind;
        public float X, Y, StartX, StartY, TX, TY;
        public float Speed;
        public int Damage;
        public float Splash;
        public Unit TargetUnit;
        public bool Done;
        public float Progress; // 0..1 for arcs
    }

    public enum BattleEventKind { UnitDeployed, UnitDied, UnitHit, BuildingHit, BuildingDestroyed, TrapTriggered, Shot, Explosion, Sprung, Ended }

    public struct BattleEvent
    {
        public BattleEventKind Kind;
        public float X, Y;
        public Unit Unit;
        public BuildingState Building;
    }

    public sealed class BattleResult
    {
        public int Stars, Percent, Gold, Elixir, TrophyDelta;
        public bool TownHallDown, Practice, Surrendered;
        public int[] TroopsUsed;
        public float Duration;
        public string EnemyName;
        public bool Won => Stars > 0;
    }

    public static class LootCalculator
    {
        public static int Cap(int enemyTh) => 1000 * enemyTh * enemyTh;

        public static int Available(int stored, int enemyTh) => Math.Min((int)(stored * 0.2), Cap(enemyTh));

        public static float HolderWeight(BuildingType t)
        {
            switch (t)
            {
                case BuildingType.TownHall: return 2f;
                case BuildingType.GoldStorage: case BuildingType.ElixirStorage: return 3f;
                case BuildingType.GoldMine: case BuildingType.ElixirCollector: return 1f;
                default: return 0f;
            }
        }
    }
}
