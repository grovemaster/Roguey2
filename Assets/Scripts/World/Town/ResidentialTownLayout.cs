using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Town
{
    /// <summary>20×30 residential district west of town_market; east column links to market.</summary>
    public static class ResidentialTownLayout
    {
        public const int MapWidth = 20;
        public const int MapHeight = 30;

        public static readonly Vector3Int PlayerStartCell = new Vector3Int(10, 8, 0);

        public static void Paint(Tilemap floorMap, Tilemap wallMap, TileBase[] floorTiles, TileBase wallTile)
        {
            if (floorMap == null || wallMap == null || floorTiles == null || floorTiles.Length == 0 || wallTile == null)
                return;

            floorMap.ClearAllTiles();
            wallMap.ClearAllTiles();

            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    bool isEastTransition = MarketResidentialTransition.IsResidentialEastTransitionCell(cell);
                    bool isPerimeterWall = x == 0 || y == 0 || y == MapHeight - 1;
                    if (isPerimeterWall && !isEastTransition)
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
