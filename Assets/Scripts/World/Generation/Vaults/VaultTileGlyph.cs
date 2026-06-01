namespace JRogue.World.Generation.Vaults
{
    /// <summary>
    /// Maps one MAP character to a tile registry key and/or door registry id.
    /// </summary>
    public readonly struct VaultTileGlyph
    {
        public const string DefaultDoorRegistryId = "door_corridor";

        public VaultCellKind Kind { get; }
        /// <summary>Registry key for <see cref="VaultAssetRegistry"/> tile lookup (floor/wall).</summary>
        public string TileKey { get; }
        /// <summary>Registry id when <see cref="Kind"/> is <see cref="VaultCellKind.Door"/>.</summary>
        public string DoorRegistryId { get; }

        public VaultTileGlyph(VaultCellKind kind, string tileKey, string doorRegistryId = null)
        {
            Kind = kind;
            TileKey = tileKey;
            DoorRegistryId = doorRegistryId;
        }

        public static VaultTileGlyph Empty => new VaultTileGlyph(VaultCellKind.Empty, null);

        public static VaultTileGlyph Floor(string tileKey) =>
            new VaultTileGlyph(VaultCellKind.Floor, tileKey);

        public static VaultTileGlyph Wall(string tileKey) =>
            new VaultTileGlyph(VaultCellKind.Wall, tileKey);

        public static VaultTileGlyph Door(string floorTileKey, string doorRegistryId = DefaultDoorRegistryId) =>
            new VaultTileGlyph(VaultCellKind.Door, floorTileKey, doorRegistryId);
    }
}
