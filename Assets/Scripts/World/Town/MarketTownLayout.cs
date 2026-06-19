using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Town
{
    /// <summary>40×40 open market district; south strip links to dimension_square, west strip links to town_residential.</summary>
    public static class MarketTownLayout
    {
        public const int MapSize = 40;

        public static readonly Vector3Int PlayerStartCell = new Vector3Int(DimensionSquareLayout.Center, 5, 0);

        public static void Paint(Tilemap floorMap, Tilemap wallMap, TileBase[] floorTiles, TileBase wallTile)
        {
            if (floorMap == null || wallMap == null || floorTiles == null || floorTiles.Length == 0 || wallTile == null)
                return;

            floorMap.ClearAllTiles();
            wallMap.ClearAllTiles();

            for (int y = 0; y < MapSize; y++)
            {
                for (int x = 0; x < MapSize; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    bool isSouthTransition = DistrictSquareMarketTransition.IsMarketSouthTransitionCell(cell);
                    bool isWestResidentialTransition = MarketResidentialTransition.IsMarketWestTransitionCell(cell);
                    bool isPerimeterWall = x == 0 || x == MapSize - 1 || y == MapSize - 1 || y == 0;
                    if (isPerimeterWall && !isSouthTransition && !isWestResidentialTransition)
                    {
                        wallMap.SetTile(cell, wallTile);
                        continue;
                    }

                    floorMap.SetTile(cell, DimensionSquareLayout.PickFloorTile(x, y, floorTiles));
                }
            }
        }
    }
}
