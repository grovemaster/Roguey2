using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Canonical one-marker-per-tile layout for <see cref="StampPath"/> town plaza.
    /// Keep all pack creators in sync by updating positions here only.
    /// </summary>
    public static class TownPlazaMarkerLayout
    {
        public const string StampPath = "Assets/Resources/Town/Stamp_TownPlaza_20x20.asset";

        public static readonly Vector3Int PlayerStartCell = new Vector3Int(10, 8, 0);
        public static readonly Vector3Int DungeonPortalCell = new Vector3Int(10, 10, 0);

        static readonly (string markerId, Vector3Int cell)[] Markers =
        {
            (StampMarkerIds.PlayerStart, PlayerStartCell),
            (StampMarkerIds.TownDungeonPortal, DungeonPortalCell),

            (StampMarkerIds.TownNpc1, new Vector3Int(4, 8, 0)),
            (StampMarkerIds.TownNpc2, new Vector3Int(6, 8, 0)),
            (StampMarkerIds.TownNpc3, new Vector3Int(8, 8, 0)),
            (StampMarkerIds.TownNpc4, new Vector3Int(2, 8, 0)),
            (StampMarkerIds.TownNpc5, new Vector3Int(12, 8, 0)),
            (StampMarkerIds.BuildingDemoDoor, new Vector3Int(14, 8, 0)),

            (StampMarkerIds.MageTutor, new Vector3Int(4, 7, 0)),
            (StampMarkerIds.KnightDrillMaster, new Vector3Int(6, 7, 0)),
            (StampMarkerIds.ArcaneVendor, new Vector3Int(8, 7, 0)),

            (StampMarkerIds.TownTimeLeverA, new Vector3Int(8, 6, 0)),
            (StampMarkerIds.TownTimeLeverB, new Vector3Int(9, 6, 0)),

            (StampMarkerIds.BeastBloodMerchant, new Vector3Int(2, 5, 0)),
            (StampMarkerIds.MeditationShrine, new Vector3Int(4, 5, 0)),
            (StampMarkerIds.DragonianElderVolscale, new Vector3Int(6, 5, 0)),
            (StampMarkerIds.FleshmetalForgemaster, new Vector3Int(8, 5, 0)),
            (StampMarkerIds.ShamanBarbarian, new Vector3Int(10, 5, 0)),
            (StampMarkerIds.FairyMerchant, new Vector3Int(12, 5, 0)),
            (StampMarkerIds.SoulBeastRitualCircle, new Vector3Int(14, 5, 0)),

            (StampMarkerIds.TownTorchWest, new Vector3Int(0, 10, 0)),
            (StampMarkerIds.TownTorchNorth, new Vector3Int(10, 19, 0)),
            (StampMarkerIds.TownTorchEast, new Vector3Int(19, 10, 0)),
        };

        public static IReadOnlyList<(string markerId, Vector3Int cell)> AllMarkers => Markers;

        public static bool TryGetCell(string markerId, out Vector3Int cell)
        {
            for (int i = 0; i < Markers.Length; i++)
            {
                if (Markers[i].markerId == markerId)
                {
                    cell = Markers[i].cell;
                    return true;
                }
            }

            cell = default;
            return false;
        }

        public static void ApplyAll(DungeonLayoutStamp stamp)
        {
            if (stamp == null)
                return;

            for (int i = 0; i < Markers.Length; i++)
                stamp.SetMarker(Markers[i].markerId, Markers[i].cell);
        }

        public static bool ValidateUniqueCells(out string error)
        {
            var seenCells = new Dictionary<Vector3Int, string>();
            for (int i = 0; i < Markers.Length; i++)
            {
                (string markerId, Vector3Int cell) = Markers[i];
                if (seenCells.TryGetValue(cell, out string other))
                {
                    error = $"Town plaza markers '{other}' and '{markerId}' share cell {cell}.";
                    return false;
                }

                seenCells[cell] = markerId;
            }

            error = null;
            return true;
        }
    }
}
