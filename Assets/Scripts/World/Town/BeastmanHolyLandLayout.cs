using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Town
{
    /// <summary>Beastman Holy Land proper — stone outdoor grounds with den shell.</summary>
    public static class BeastmanHolyLandLayout
    {
        public const int MapSize = 40;
        public const int Center = 20;

        public const string ChiefMarkerId = "chief_beastman";
        public const string DenEnterMarkerId = "beastman_den_enter";

        public const int DenOriginX = 24;
        public const int DenOriginY = 18;
        public const int DenWidth = 8;
        public const int DenDepth = 8;
        public const int DenDoorLocalX = 4;

        public static readonly Vector3Int PlayerStartCell = new Vector3Int(Center, Center, 0);
        public static readonly Vector3Int ChiefNpcCell = new Vector3Int(Center, 24, 0);
        public static readonly Vector3Int ReturnToNexusCell = HolyLandNexusLayout.BeastmanHolyLandGateCell;
        public static readonly Vector3Int DenDoorCell = new Vector3Int(
            DenOriginX + DenDoorLocalX,
            DenOriginY,
            0);

        public static void Paint(
            Tilemap floorMap,
            Tilemap wallMap,
            TileBase[] stoneFloorTiles,
            TileBase stoneWallTile,
            TileBase buildingWallTile,
            TileBase buildingDoorTile)
        {
            if (floorMap == null || wallMap == null || stoneFloorTiles == null || stoneFloorTiles.Length == 0)
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

                    if (IsDenWallCell(x, y, out bool isDenDoor))
                    {
                        if (isDenDoor && buildingDoorTile != null)
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

                    if (IsDenInteriorFloorCell(x, y))
                    {
                        floorMap.SetTile(cell, PickFloorTile(x, y, stoneFloorTiles));
                        continue;
                    }

                    if (isBorder && !isReturnPortal)
                    {
                        if (stoneWallTile != null)
                            wallMap.SetTile(cell, stoneWallTile);
                        continue;
                    }

                    floorMap.SetTile(cell, PickFloorTile(x, y, stoneFloorTiles));
                }
            }
        }

        static bool IsDenWallCell(int x, int y, out bool isDoor)
        {
            isDoor = false;
            int localX = x - DenOriginX;
            int localY = y - DenOriginY;
            if (localX < 0 || localY < 0 || localX >= DenWidth || localY >= DenDepth)
                return false;

            bool perimeter = localX == 0 || localY == 0 || localX == DenWidth - 1 || localY == DenDepth - 1;
            if (!perimeter)
                return false;

            isDoor = localX == DenDoorLocalX && localY == 0;
            return !isDoor;
        }

        static bool IsDenInteriorFloorCell(int x, int y)
        {
            int localX = x - DenOriginX;
            int localY = y - DenOriginY;
            return localX > 0 && localY > 0 && localX < DenWidth - 1 && localY < DenDepth - 1;
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
