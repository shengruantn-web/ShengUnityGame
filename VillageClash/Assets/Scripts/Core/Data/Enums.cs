namespace VillageClash.Core
{
    public enum BuildingType
    {
        TownHall, BuilderHut, GoldMine, ElixirCollector, GoldStorage, ElixirStorage,
        Barracks, ArmyCamp, Laboratory, Cannon, ArcherTower, Mortar, Wall, Bomb, SpringTrap
    }

    public enum BuildingCategory { Resource, Army, Defense, Trap }

    public enum TroopType { Barbarian, Archer, Giant, Goblin, WallBreaker }

    public enum TargetPreference { Any, Defenses, Resources, Walls }

    public enum ObstacleKind { Tree, Bush, Rock, Stump }

    public enum ResourceKind { Gold, Elixir, Gems }
}
