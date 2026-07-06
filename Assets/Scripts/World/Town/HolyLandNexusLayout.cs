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
        public static readonly Vector3Int ElfHolyLandGateCell = new Vector3Int(7, 35, 0);
        public static readonly Vector3Int BeastmanHolyLandGateCell = new Vector3Int(1, 35, 0);
        public static readonly Vector3Int TieflingHolyLandGateCell = new Vector3Int(13, 29, 0);
        public static readonly Vector3Int HolyLandArrivalCell = new Vector3Int(20, 4, 0);
        public static readonly Vector3Int HolyLandReturnAnchor = HolyLandGateCell;
        public static readonly Vector3Int ElfHolyLandReturnAnchor = ElfHolyLandGateCell;
        public static readonly Vector3Int BeastmanHolyLandReturnAnchor = BeastmanHolyLandGateCell;
        public static readonly Vector3Int TieflingHolyLandReturnAnchor = TieflingHolyLandGateCell;
        /// <summary>Spawn one tile inward from the gate so exit does not land on the admission portal.</summary>
        public static readonly Vector3Int BarbarianHolyLandNexusArrivalCell = new Vector3Int(13, 34, 0);
        public static readonly Vector3Int ElfHolyLandNexusArrivalCell = new Vector3Int(7, 34, 0);
        public static readonly Vector3Int BeastmanHolyLandNexusArrivalCell = new Vector3Int(1, 34, 0);
        public static readonly Vector3Int TieflingHolyLandNexusArrivalCell = new Vector3Int(13, 28, 0);

        /// <summary>
        /// Stand-on cell for racial holy land return portals. The gate marker cell sits in the map corner;
        /// players approach from one tile inward.
        /// </summary>
        public static readonly Vector3Int ElfHolyLandExitStandCell = new Vector3Int(8, 35, 0);
        public static readonly Vector3Int BarbarianHolyLandExitStandCell = new Vector3Int(12, 35, 0);
        public static readonly Vector3Int BeastmanHolyLandExitStandCell = new Vector3Int(2, 35, 0);
        public static readonly Vector3Int TieflingHolyLandExitStandCell = new Vector3Int(14, 29, 0);

        public static bool TryGetHolyLandExitStandCell(string portalLinkId, out Vector3Int standCell)
        {
            if (portalLinkId == HolyLandTransitionIds.TieflingHolyLandToNexus)
            {
                standCell = TieflingHolyLandExitStandCell;
                return true;
            }

            if (portalLinkId == HolyLandTransitionIds.BeastmanHolyLandToNexus)
            {
                standCell = BeastmanHolyLandExitStandCell;
                return true;
            }

            if (portalLinkId == HolyLandTransitionIds.ElfHolyLandToNexus)
            {
                standCell = ElfHolyLandExitStandCell;
                return true;
            }

            if (portalLinkId == HolyLandTransitionIds.HolyLandToNexus)
            {
                standCell = BarbarianHolyLandExitStandCell;
                return true;
            }

            standCell = default;
            return false;
        }

        public static bool IsHolyLandExitActivationCell(Vector3Int cell) =>
            cell == TieflingHolyLandGateCell
            || cell == TieflingHolyLandExitStandCell
            || cell == BeastmanHolyLandGateCell
            || cell == BeastmanHolyLandExitStandCell
            || cell == ElfHolyLandGateCell
            || cell == ElfHolyLandExitStandCell
            || cell == HolyLandGateCell
            || cell == BarbarianHolyLandExitStandCell;

        /// <summary>Walkable bridge from the decagon to the Barbarian Holy Land gate (west of north hub).</summary>
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

        /// <summary>Walkable bridge from the decagon to the Elf Holy Land gate (west of the barbarian gate).</summary>
        public static bool IsElfHolyLandGateApproach(int x, int y)
        {
            Vector3Int gate = ElfHolyLandGateCell;
            if (x == gate.x && y == gate.y)
                return true;

            if (y == gate.y && x > gate.x && x < HolyLandGateCell.x)
                return true;

            if (y == gate.y - 1 && x >= gate.x && x <= HolyLandGateCell.x)
                return true;

            return false;
        }

        /// <summary>Walkable bridge from the decagon to the Beastman Holy Land gate (west of the elf gate).</summary>
        public static bool IsBeastmanHolyLandGateApproach(int x, int y)
        {
            Vector3Int gate = BeastmanHolyLandGateCell;
            if (x == gate.x && y == gate.y)
                return true;

            if (y == gate.y && x > gate.x && x < ElfHolyLandGateCell.x)
                return true;

            if (y == gate.y - 1 && x >= gate.x && x <= ElfHolyLandGateCell.x)
                return true;

            return false;
        }

        /// <summary>Walkable spur south of the barbarian gate along the nexus edge.</summary>
        public static bool IsTieflingHolyLandGateApproach(int x, int y)
        {
            Vector3Int gate = TieflingHolyLandGateCell;
            if (x == gate.x && y == gate.y)
                return true;

            if (x == gate.x && y > gate.y && y <= HolyLandGateCell.y)
                return true;

            if (y == gate.y + 1 && x >= gate.x && x <= HolyLandGateCell.x)
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
            || IsElfHolyLandGateApproach(x, y)
            || IsBeastmanHolyLandGateApproach(x, y)
            || IsTieflingHolyLandGateApproach(x, y)
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
