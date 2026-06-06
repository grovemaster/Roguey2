using System;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation
{
    [CreateAssetMenu(fileName = "DungeonLayoutStamp", menuName = "JRogue/World/Dungeon Layout Stamp")]
    public sealed class DungeonLayoutStamp : ScriptableObject
    {
        [SerializeField] int width = 30;
        [SerializeField] int height = 30;
        [SerializeField] bool[] floorCells;
        [SerializeField] bool[] wallCells;
        [SerializeField] List<StampMarkerEntry> markers = new List<StampMarkerEntry>();

        public int Width => width;
        public int Height => height;
        public IReadOnlyList<StampMarkerEntry> Markers => markers;

        public bool IsFloor(int x, int y) => InBounds(x, y) && GetIndex(x, y, out int i) && floorCells != null && i < floorCells.Length && floorCells[i];

        public bool IsWall(int x, int y) => InBounds(x, y) && GetIndex(x, y, out int i) && wallCells != null && i < wallCells.Length && wallCells[i];

        public bool TryGetMarker(string markerId, out Vector3Int cell)
        {
            cell = default;
            if (string.IsNullOrEmpty(markerId) || markers == null)
                return false;

            for (int i = 0; i < markers.Count; i++)
            {
                StampMarkerEntry entry = markers[i];
                if (entry.markerId != markerId)
                    continue;

                cell = entry.cell;
                return true;
            }

            return false;
        }

        public Vector3Int PlayerStart =>
            TryGetMarker(StampMarkerIds.PlayerStart, out Vector3Int cell) ? cell : new Vector3Int(width / 2, height / 2, 0);

        public void InitializeGrid(int newWidth, int newHeight, bool borderWalls = true)
        {
            width = Mathf.Max(1, newWidth);
            height = Mathf.Max(1, newHeight);
            int count = width * height;
            floorCells = new bool[count];
            wallCells = new bool[count];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool border = borderWalls && (x == 0 || y == 0 || x == width - 1 || y == height - 1);
                    int index = y * width + x;
                    wallCells[index] = border;
                    floorCells[index] = !border;
                }
            }

            markers ??= new List<StampMarkerEntry>();
            markers.RemoveAll(m => m.markerId == StampMarkerIds.PlayerStart);
            markers.Add(new StampMarkerEntry
            {
                markerId = StampMarkerIds.PlayerStart,
                cell = new Vector3Int(width / 2, Mathf.Max(2, height / 4), 0),
            });
        }

        public void SetCell(int x, int y, bool floor, bool wall)
        {
            if (!GetIndex(x, y, out int index))
                return;

            floorCells[index] = floor;
            wallCells[index] = wall;
        }

        public void SetMarker(string markerId, Vector3Int cell)
        {
            markers ??= new List<StampMarkerEntry>();
            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i].markerId != markerId)
                    continue;

                markers[i] = new StampMarkerEntry { markerId = markerId, cell = cell };
                return;
            }

            markers.Add(new StampMarkerEntry { markerId = markerId, cell = cell });
        }

        bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

        bool GetIndex(int x, int y, out int index)
        {
            index = -1;
            if (!InBounds(x, y))
                return false;

            index = y * width + x;
            return true;
        }
    }

    [Serializable]
    public struct StampMarkerEntry
    {
        public string markerId;
        public Vector3Int cell;
    }

    public static class StampMarkerIds
    {
        public const string PlayerStart = "playerStart";
        public const string PortalSouth = "portal_south";
        public const string PortalNorth = "portal_north";
        public const string TownDungeonPortal = "town_dungeon_portal";
        public const string TownNpc1 = "town_npc_1";
        public const string TownNpc2 = "town_npc_2";
        public const string TownNpc3 = "town_npc_3";
        public const string TownNpc4 = "town_npc_4";
        public const string TownNpc5 = "town_npc_5";
    }
}
