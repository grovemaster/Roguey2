namespace JRogue.World.Generation
{
    /// <summary>Stamp markers and resource paths for dwarf clan stewards and plaza altars on town_main.</summary>
    public static class DwarfClanTownEntries
    {
        public readonly struct AltarEntry
        {
            public readonly string MarkerId;
            public readonly string AltarResourcesPath;
            public readonly string AltarEditorPath;

            public AltarEntry(string markerId, string altarResourcesPath, string altarEditorPath)
            {
                MarkerId = markerId;
                AltarResourcesPath = altarResourcesPath;
                AltarEditorPath = altarEditorPath;
            }
        }

        public readonly struct StewardEntry
        {
            public readonly string MarkerId;
            public readonly string PrefabResourcesPath;
            public readonly string PrefabEditorPath;

            public StewardEntry(string markerId, string prefabResourcesPath, string prefabEditorPath)
            {
                MarkerId = markerId;
                PrefabResourcesPath = prefabResourcesPath;
                PrefabEditorPath = prefabEditorPath;
            }
        }

        public static readonly AltarEntry[] Altars =
        {
            new(
                StampMarkerIds.ForgeBrothersAltar,
                "Interactables/HallOfAncestorsAltar_ForgeBrothers",
                "Assets/Resources/Interactables/HallOfAncestorsAltar_ForgeBrothers.asset"),
            new(
                StampMarkerIds.StoneWardensAltar,
                "Interactables/HallOfAncestorsAltar_StoneWardens",
                "Assets/Resources/Interactables/HallOfAncestorsAltar_StoneWardens.asset"),
        };

        public static readonly StewardEntry[] Stewards =
        {
            new(
                StampMarkerIds.ForgeBrothersSteward,
                "Town/Npc/TownNpc_ForgeBrothersSteward",
                "Assets/Resources/Town/Npc/TownNpc_ForgeBrothersSteward.prefab"),
            new(
                StampMarkerIds.StoneWardensSteward,
                "Town/Npc/TownNpc_StoneWardensSteward",
                "Assets/Resources/Town/Npc/TownNpc_StoneWardensSteward.prefab"),
        };
    }
}
