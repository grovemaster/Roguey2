using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Residential inn — 8×3 exterior (3 south doors) on town_residential; unique U-counter interior.</summary>
    public static class ResidentialInnLayout
    {
        public const string InteriorFloorId = "town_interior_residential_inn";
        public const string EnterWestLinkId = "building_residential_inn_enter_west";
        public const string ExitWestLinkId = "building_residential_inn_exit_west";
        public const string EnterCenterLinkId = "building_residential_inn_enter_center";
        public const string ExitCenterLinkId = "building_residential_inn_exit_center";
        public const string EnterEastLinkId = "building_residential_inn_enter_east";
        public const string ExitEastLinkId = "building_residential_inn_exit_east";
        public const string NpcMarkerId = "residential_inn_keeper";
        public const string NpcId = "residential_inn_keeper";

        public const int ExteriorWidth = 8;
        public const int ExteriorDepth = 3;

        public const int ExteriorOriginX = (ResidentialTownLayout.MapWidth - ExteriorWidth) / 2;
        public const int ExteriorOriginY = (ResidentialTownLayout.MapHeight - ExteriorDepth) / 2;

        public const int ExteriorWestDoorLocalX = 1;
        public const int ExteriorCenterDoorLocalX = 3;
        public const int ExteriorEastDoorLocalX = 5;

        public static readonly Vector3Int ExteriorWestDoorCell =
            new Vector3Int(ExteriorOriginX + ExteriorWestDoorLocalX, ExteriorOriginY, 0);
        public static readonly Vector3Int ExteriorCenterDoorCell =
            new Vector3Int(ExteriorOriginX + ExteriorCenterDoorLocalX, ExteriorOriginY, 0);
        public static readonly Vector3Int ExteriorEastDoorCell =
            new Vector3Int(ExteriorOriginX + ExteriorEastDoorLocalX, ExteriorOriginY, 0);

        public const int InteriorWidth = 14;
        public const int InteriorHeight = 12;

        public const int CounterRowY = 5;
        public const int CustomerRowY = 4;

        public static readonly Vector3Int InteriorWestExitCell = new Vector3Int(3, 0, 0);
        public static readonly Vector3Int InteriorCenterExitCell = new Vector3Int(6, 0, 0);
        public static readonly Vector3Int InteriorEastExitCell = new Vector3Int(9, 0, 0);

        public static readonly Vector3Int InteriorWestArrivalCell = new Vector3Int(3, 1, 0);
        public static readonly Vector3Int InteriorCenterArrivalCell = new Vector3Int(6, 1, 0);
        public static readonly Vector3Int InteriorEastArrivalCell = new Vector3Int(9, 1, 0);

        public static readonly Vector3Int InnkeeperNpcCell = new Vector3Int(7, 8, 0);
        public static readonly Vector3Int CustomerTalkCell = new Vector3Int(7, CustomerRowY, 0);

        static readonly Vector3Int[] CounterCells =
        {
            new Vector3Int(5, 7, 0), new Vector3Int(6, 7, 0), new Vector3Int(7, 7, 0), new Vector3Int(8, 7, 0),
            new Vector3Int(5, 6, 0), new Vector3Int(8, 6, 0),
            new Vector3Int(6, 5, 0), new Vector3Int(7, 5, 0),
        };

        static readonly Vector3Int[] BedCells =
        {
            new Vector3Int(1, 3, 0), new Vector3Int(1, 6, 0), new Vector3Int(1, 9, 0),
            new Vector3Int(12, 3, 0), new Vector3Int(12, 6, 0), new Vector3Int(12, 9, 0),
        };

        public static bool IsCounterCell(Vector3Int cell)
        {
            for (int i = 0; i < CounterCells.Length; i++)
            {
                if (CounterCells[i] == cell)
                    return true;
            }

            return false;
        }

        public static bool IsBedCell(Vector3Int cell)
        {
            for (int i = 0; i < BedCells.Length; i++)
            {
                if (BedCells[i] == cell)
                    return true;
            }

            return false;
        }

        public static bool IsInteriorExitCell(Vector3Int cell) =>
            cell == InteriorWestExitCell || cell == InteriorCenterExitCell || cell == InteriorEastExitCell;

        public static IEnumerable<Vector3Int> EnumerateCounterCells()
        {
            for (int i = 0; i < CounterCells.Length; i++)
                yield return CounterCells[i];
        }

        public static IEnumerable<Vector3Int> EnumerateBedCells()
        {
            for (int i = 0; i < BedCells.Length; i++)
                yield return BedCells[i];
        }
    }
}
