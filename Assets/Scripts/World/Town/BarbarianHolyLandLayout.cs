using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Town
{
    /// <summary>Barbarian Holy Land proper — dirt outdoor camp with shaman tent shell.</summary>
    public static class BarbarianHolyLandLayout
    {
        public const int MapSize = 40;
        public const int Center = 20;

        public const string ChiefMarkerId = "chief_barbarian";
        public const string TentEnterMarkerId = "barbarian_tent_enter";

        public const int TentOriginX = 24;
        public const int TentOriginY = 18;
        public const int TentWidth = 8;
        public const int TentDepth = 8;
        public const int TentDoorLocalX = 4;

        public static readonly Vector3Int PlayerStartCell = new Vector3Int(Center, Center, 0);
        public static readonly Vector3Int ChiefNpcCell = new Vector3Int(Center, 24, 0);
        public static readonly Vector3Int ReturnToNexusCell = HolyLandNexusLayout.HolyLandGateCell;
        public static readonly Vector3Int TentDoorCell = new Vector3Int(
            TentOriginX + TentDoorLocalX,
            TentOriginY,
            0);

        public static void Paint(
            Tilemap floorMap,
            Tilemap wallMap,
            TileBase[] dirtFloorTiles,
            TileBase stoneWallTile,
            TileBase buildingWallTile,
            TileBase buildingDoorTile)
        {
            if (floorMap == null || wallMap == null || dirtFloorTiles == null || dirtFloorTiles.Length == 0)
                return;

            floorMap.ClearAllTiles();
            wallMap.ClearAllTiles();

            for (int y = 0; y < MapSize; y++)
            {
                for (int x = 0; x < MapSize; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    bool isBorder = x == 0 || y == 0 || x == MapSize - 1 || y == MapSize - 1;
                    bool isReturnPortal = cell == ReturnToNexusCell;

                    if (IsTentWallCell(x, y, out bool isTentDoor))
                    {
                        if (isTentDoor && buildingDoorTile != null)
                        {
                            floorMap.SetTile(cell, buildingDoorTile);
                            wallMap.SetTile(cell, null);
                        }
                        else if (buildingWallTile != null)
                        {
                            wallMap.SetTile(cell, buildingWallTile);
                            floorMap.SetTile(cell, null);
                        }

                        continue;
                    }

                    if (IsTentInteriorFloorCell(x, y))
                    {
                        floorMap.SetTile(cell, PickFloorTile(x, y, dirtFloorTiles));
                        continue;
                    }

                    if (isBorder && !isReturnPortal)
                    {
                        if (stoneWallTile != null)
                            wallMap.SetTile(cell, stoneWallTile);
                        continue;
                    }

                    floorMap.SetTile(cell, PickFloorTile(x, y, dirtFloorTiles));
                }
            }
        }

        static bool IsTentWallCell(int x, int y, out bool isDoor)
        {
            isDoor = false;
            int localX = x - TentOriginX;
            int localY = y - TentOriginY;
            if (localX < 0 || localY < 0 || localX >= TentWidth || localY >= TentDepth)
                return false;

            bool perimeter = localX == 0 || localY == 0 || localX == TentWidth - 1 || localY == TentDepth - 1;
            if (!perimeter)
                return false;

            isDoor = localX == TentDoorLocalX && localY == 0;
            return !isDoor;
        }

        static bool IsTentInteriorFloorCell(int x, int y)
        {
            int localX = x - TentOriginX;
            int localY = y - TentOriginY;
            return localX > 0 && localY > 0 && localX < TentWidth - 1 && localY < TentDepth - 1;
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
