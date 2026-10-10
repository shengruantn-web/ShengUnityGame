namespace VillageClash.Core
{
    /// <summary>User-facing messages returned by systems (null means success).</summary>
    public static class Msg
    {
        public const string NotEnoughGold = "Not enough Gold!";
        public const string NotEnoughElixir = "Not enough Elixir!";
        public const string NotEnoughGems = "Not enough Gems!";
        public const string BuildersBusy = "All builders are busy!";
        public const string MaxReached = "You already have the maximum number of these.";
        public const string TownHallRequired = "Upgrade your Town Hall first!";
        public const string CantPlace = "You can't place it there!";
        public const string MaxLevel = "Already at max level!";
        public const string AlreadyUpgrading = "This building is already being upgraded!";
        public const string NotFound = "Nothing there.";
        public const string ArmyFull = "Your Army Camps are full!";
        public const string NoBarracks = "Build a Barracks first!";
        public const string BarracksLevel = "Upgrade your Barracks to unlock this troop!";
        public const string NoLab = "Build a Laboratory first!";
        public const string LabBusy = "The Laboratory is already researching!";
        public const string LabLevel = "Upgrade your Laboratory first!";
        public const string EmptyArmy = "Train some troops first!";
        public const string NothingToCollect = "Nothing to collect yet.";
        public const string StorageFull = "Your storages are full!";
        public const string Collected = "Collected";
    }
}
