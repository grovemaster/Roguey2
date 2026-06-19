namespace JRogue.World.Town
{
    /// <summary>Resource and asset paths for the DistrictTest town hub slice.</summary>
    public static class TownDistrictTestPaths
    {
        public const string DistrictTestRoot = "Assets/Resources/Town/DistrictTest";
        public const string DimensionSquareFolder = DistrictTestRoot + "/TownArea/DimensionSquare";
        public const string DimensionSquareFloorDef = DimensionSquareFolder + "/Floor_dimension_square.asset";
        public const string DimensionSquareFloorPalette = DimensionSquareFolder + "/Palette_dimension_square_floor.asset";
        public const string DimensionSquareWallPalette = DimensionSquareFolder + "/Palette_dimension_square_wall.asset";
        public const string DistrictTestCatalog = DistrictTestRoot + "/DistrictTestCatalog.asset";
        public const string DistrictTownTestScene = "Assets/Scenes/Town/DistrictTownTest.unity";

        public const string AdventureGuildExchangeFolder = DistrictTestRoot + "/Building/AdventureGuildExchange";
        public const string AdventureGuildInteriorFloorDef =
            AdventureGuildExchangeFolder + "/Floor_town_interior_adventure_guild_exchange.asset";
        public const string AdventureGuildInteriorFacadeOverlay =
            AdventureGuildExchangeFolder + "/FacadeOverlay_town_interior_adventure_guild_exchange.asset";
        public const string DimensionSquareFacadeOverlay =
            DimensionSquareFolder + "/FacadeOverlay_dimension_square.asset";

        public const string ResourcesCatalog = "Town/DistrictTest/DistrictTestCatalog";
    }
}
