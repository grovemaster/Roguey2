namespace JRogue.World.Generation
{
    public enum PartyCompositionPreset
    {
        ClassicBarbarianHumanElfUndead,
        TieflingBeastmanDragonianDwarf
    }

    public static class PartyCompositionPresets
    {
        public const string BarbarianPrefabPath = "Assets/Prefabs/Actor/Race/BarbarianPlayer.prefab";
        public const string HumanPrefabPath = "Assets/Prefabs/Actor/Race/HumanPlayer.prefab";
        public const string ElfPrefabPath = "Assets/Prefabs/Actor/Race/ElfPlayer.prefab";
        public const string UndeadPrefabPath = "Assets/Prefabs/Actor/Race/UndeadPlayer.prefab";
        public const string TieflingPrefabPath = "Assets/Prefabs/Actor/Race/TieflingPlayer.prefab";
        public const string BeastmanPrefabPath = "Assets/Prefabs/Actor/Race/BeastmanPlayer.prefab";
        public const string DragonianPrefabPath = "Assets/Prefabs/Actor/Race/DragonianPlayer.prefab";
        public const string DwarfPrefabPath = "Assets/Prefabs/Actor/Race/DwarfPlayer.prefab";

        public static string GetDisplayName(PartyCompositionPreset preset)
        {
            switch (preset)
            {
                case PartyCompositionPreset.ClassicBarbarianHumanElfUndead:
                    return "Barbarian, Human, Elf, Undead";
                case PartyCompositionPreset.TieflingBeastmanDragonianDwarf:
                    return "Tiefling, Beastman, Dragonian, Dwarf";
                default:
                    return preset.ToString();
            }
        }

        public static string[] GetPrefabPaths(PartyCompositionPreset preset)
        {
            switch (preset)
            {
                case PartyCompositionPreset.ClassicBarbarianHumanElfUndead:
                    return new[]
                    {
                        BarbarianPrefabPath,
                        HumanPrefabPath,
                        ElfPrefabPath,
                        UndeadPrefabPath
                    };
                case PartyCompositionPreset.TieflingBeastmanDragonianDwarf:
                    return new[]
                    {
                        TieflingPrefabPath,
                        BeastmanPrefabPath,
                        DragonianPrefabPath,
                        DwarfPrefabPath
                    };
                default:
                    return null;
            }
        }
    }
}
