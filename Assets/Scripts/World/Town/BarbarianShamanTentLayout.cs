using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Town
{
    /// <summary>8×8 shaman tent interior on the Holy Land slice.</summary>
    public static class BarbarianShamanTentLayout
    {
        public const int InteriorWidth = 8;
        public const int InteriorHeight = 8;

        public const string ShamanMarkerId = "shaman_barbarian";

        public static readonly Vector3Int InteriorArrivalCell = new Vector3Int(4, 1, 0);
        public static readonly Vector3Int InteriorExitCell = new Vector3Int(4, 0, 0);
        public static readonly Vector3Int ShamanNpcCell = new Vector3Int(4, 5, 0);
        public static readonly Vector3Int ExteriorReturnCell = BarbarianHolyLandLayout.TentDoorCell;

        public static void Paint(
            Tilemap floorMap,
            Tilemap wallMap,
            TileBase[] floorTiles,
            TileBase wallTile,
            TileBase doorTile)
        {
            if (floorMap == null || wallMap == null || floorTiles == null || floorTiles.Length == 0 || wallTile == null)
                return;

            floorMap.ClearAllTiles();
            wallMap.ClearAllTiles();

            for (int y = 0; y < InteriorHeight; y++)
            {
                for (int x = 0; x < InteriorWidth; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    bool isPerimeter = x == 0 || y == 0 || x == InteriorWidth - 1 || y == InteriorHeight - 1;
                    bool isExit = cell == InteriorExitCell;

                    if (isPerimeter && !isExit)
                    {
                        wallMap.SetTile(cell, wallTile);
                        continue;
                    }

                    if (isExit && doorTile != null)
                    {
                        floorMap.SetTile(cell, doorTile);
                        continue;
                    }

                    floorMap.SetTile(cell, PickFloorTile(x, y, floorTiles));
                }
            }
        }

        static TileBase PickFloorTile(int x, int y, TileBase[] tiles)
        {
            int hash = unchecked((x * 73856093) ^ (y * 19349663));
            return tiles[Mathf.Abs(hash) % tiles.Length];
        }
    }
}
