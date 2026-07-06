using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Town
{
    /// <summary>Elf Holy Land proper — grass outdoor grove with house shell.</summary>
    public static class ElfHolyLandLayout
    {
        public const int MapSize = 40;
        public const int Center = 20;

        public const string ChiefMarkerId = "chief_elf";
        public const string HouseEnterMarkerId = "elf_house_enter";

        public const int HouseOriginX = 24;
        public const int HouseOriginY = 18;
        public const int HouseWidth = 8;
        public const int HouseDepth = 8;
        public const int HouseDoorLocalX = 4;

        public static readonly Vector3Int PlayerStartCell = new Vector3Int(Center, Center, 0);
        public static readonly Vector3Int ChiefNpcCell = new Vector3Int(Center, 24, 0);
        public static readonly Vector3Int ReturnToNexusCell = HolyLandNexusLayout.ElfHolyLandGateCell;
        public static readonly Vector3Int HouseDoorCell = new Vector3Int(
            HouseOriginX + HouseDoorLocalX,
            HouseOriginY,
            0);

        public static void Paint(
            Tilemap floorMap,
            Tilemap wallMap,
            TileBase[] grassFloorTiles,
            TileBase stoneWallTile,
            TileBase buildingWallTile,
            TileBase buildingDoorTile)
        {
            if (floorMap == null || wallMap == null || grassFloorTiles == null || grassFloorTiles.Length == 0)
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

                    if (IsHouseWallCell(x, y, out bool isHouseDoor))
                    {
                        if (isHouseDoor && buildingDoorTile != null)
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

                    if (IsHouseInteriorFloorCell(x, y))
                    {
                        floorMap.SetTile(cell, PickFloorTile(x, y, grassFloorTiles));
                        continue;
                    }

                    if (isBorder && !isReturnPortal)
                    {
                        if (stoneWallTile != null)
                            wallMap.SetTile(cell, stoneWallTile);
                        continue;
                    }

                    floorMap.SetTile(cell, PickFloorTile(x, y, grassFloorTiles));
                }
            }
        }

        static bool IsHouseWallCell(int x, int y, out bool isDoor)
        {
            isDoor = false;
            int localX = x - HouseOriginX;
            int localY = y - HouseOriginY;
            if (localX < 0 || localY < 0 || localX >= HouseWidth || localY >= HouseDepth)
                return false;

            bool perimeter = localX == 0 || localY == 0 || localX == HouseWidth - 1 || localY == HouseDepth - 1;
            if (!perimeter)
                return false;

            isDoor = localX == HouseDoorLocalX && localY == 0;
            return !isDoor;
        }

        static bool IsHouseInteriorFloorCell(int x, int y)
        {
            int localX = x - HouseOriginX;
            int localY = y - HouseOriginY;
            return localX > 0 && localY > 0 && localX < HouseWidth - 1 && localY < HouseDepth - 1;
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
