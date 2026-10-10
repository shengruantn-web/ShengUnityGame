using System;

namespace VillageClash.Core
{
    [Serializable]
    public sealed class BuildingInstance
    {
        public int Id;
        public BuildingType Type;
        public int Level = 1;
        public Int2 Origin;
        public int Size;
        /// <summary>True while being built (level 1) or upgraded (to Level+1).</summary>
        public bool Constructing;
        public long FinishAtUnix;
        public long StartedAtUnix;
        /// <summary>Resources accumulated inside a collector, not yet collected.</summary>
        public double Stored;
        /// <summary>Set for traps after they fire; rearmed when the player taps them.</summary>
        public bool Triggered;

        public BuildingDef Def => GameData.Building(Type);
        /// <summary>Stats of the currently effective level (an upgrading building keeps its old stats).</summary>
        public LevelStats Stats => Def.Stats(Level);
        public int TargetLevel => Constructing ? Level + (IsNewBuild ? 0 : 1) : Level;
        public bool IsNewBuild;
        public int CenterX2 => Origin.X * 2 + Size; // doubled coordinates to avoid floats
        public int CenterY2 => Origin.Y * 2 + Size;
        public float CenterX => Origin.X + Size * 0.5f;
        public float CenterY => Origin.Y + Size * 0.5f;

        public bool Contains(Int2 c) => c.X >= Origin.X && c.Y >= Origin.Y && c.X < Origin.X + Size && c.Y < Origin.Y + Size;
    }

    [Serializable]
    public sealed class ObstacleInstance
    {
        public int Id;
        public ObstacleKind Kind;
        public Int2 Origin;
        public int Size;
        public bool Removing;
        public long FinishAtUnix;
        public bool Contains(Int2 c) => c.X >= Origin.X && c.Y >= Origin.Y && c.X < Origin.X + Size && c.Y < Origin.Y + Size;
    }
}
