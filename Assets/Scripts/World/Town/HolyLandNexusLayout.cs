using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Town
{
    /// <summary>40×40 decagon nexus south of Dimension Square.</summary>
    public static class HolyLandNexusLayout
    {
        public const int MapSize = 40;
        public const int Center = 20;
        public const float DecagonRadius = 17.5f;

        public static readonly Vector3Int PlayerStartCell = new Vector3Int(Center, Center, 0);
        public static readonly Vector3Int HolyLandGateCell = new Vector3Int(13, 35, 0);
        public static readonly Vector3Int HolyLandArrivalCell = new Vector3Int(20, 4, 0);
        public static readonly Vector3Int HolyLandReturnAnchor = HolyLandGateCell;

        /// <summary>Walkable bridge from the decagon to the Holy Land gate (west of north hub).</summary>
        public static bool IsHolyLandGateApproach(int x, int y)
        {
            Vector3Int gate = HolyLandGateCell;
            if (x == gate.x && y == gate.y)
                return true;

            if (y == gate.y && x > gate.x && x < DistrictSquareHolyNexusTransition.StripMinX)
                return true;

            if (y == gate.y - 1 && x >= gate.x && x <= DistrictSquareHolyNexusTransition.StripMinX)
                return true;

            return false;
        }

        /// <summary>North strip + corridor linking the decagon interior to dimension_square.</summary>
        public static bool IsNorthHubConnection(int x, int y)
        {
            if (x < DistrictSquareHolyNexusTransition.StripMinX
                || x > DistrictSquareHolyNexusTransition.StripMaxX)
            {
                return false;
            }

            return y >= 35 && y <= DistrictSquareHolyNexusTransition.NexusNorthEdgeY;
        }

        public static bool IsWalkableCell(int x, int y) =>
            IsInsideDecagon(x, y)
            || IsNorthHubConnection(x, y)
            || IsHolyLandGateApproach(x, y)
            || DistrictSquareHolyNexusTransition.IsNexusNorthTransitionCell(new Vector3Int(x, y, 0));

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
                    if (IsWalkableCell(x, y))
                        floorMap.SetTile(cell, PickFloorTile(x, y, floorTiles));
                    else
                        wallMap.SetTile(cell, wallTile);
                }
            }
        }

        public static bool IsInsideDecagon(int x, int y)
        {
            float dx = x - Center + 0.5f;
            float dy = y - Center + 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist <= 0.01f)
                return true;

            float angle = Mathf.Atan2(dy, dx);
            const float sector = 2f * Mathf.PI / 10f;
            float localAngle = angle - Mathf.Floor((angle + sector * 0.5f) / sector) * sector;
            float maxRadius = DecagonRadius * Mathf.Cos(sector * 0.5f) / Mathf.Cos(localAngle);
            return dist <= maxRadius + 0.25f;
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
