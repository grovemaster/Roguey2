using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Town
{
    /// <summary>Shared 40×40 plus layout for Dimension Square hub districts.</summary>
    public static class DimensionSquareLayout
    {
        public const int MapSize = 40;
        public const int Center = 20;
        public const int CorridorWidth = 10;
        public const int ArmMin = Center - CorridorWidth / 2;
        public const int ArmMax = Center + CorridorWidth / 2 - 1;

        public static readonly Vector3Int PlayerStartCell = new Vector3Int(Center, 18, 0);
        public static readonly Vector3Int DungeonPortalCell = new Vector3Int(Center, Center, 0);
        public static readonly Vector3Int NpcSlotNorthCell = new Vector3Int(Center, 30, 0);
        public static readonly Vector3Int NpcSlotSouthCell = new Vector3Int(Center, 10, 0);
        public static readonly Vector3Int NpcSlotEastCell = new Vector3Int(30, Center, 0);
        public static readonly Vector3Int NpcSlotWestCell = new Vector3Int(10, Center, 0);

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
                    bool border = x == 0 || y == 0 || x == MapSize - 1 || y == MapSize - 1;
                    bool plus = x >= ArmMin && x <= ArmMax || y >= ArmMin && y <= ArmMax;

                    if (border || !plus)
                        wallMap.SetTile(cell, wallTile);
                    else
                        floorMap.SetTile(cell, PickFloorTile(x, y, floorTiles));
                }
            }
        }

        public static TileBase PickFloorTile(int x, int y, TileBase[] tiles)
        {
            if (tiles == null || tiles.Length == 0)
                return null;

            int hash = unchecked((x * 73856093) ^ (y * 19349663));
            return tiles[Mathf.Abs(hash) % tiles.Length];
        }
    }
}
