using UnityEngine;

namespace JRogue.World.MapInteract
{
    public static class MapInteractOrthogonal
    {
        static readonly Vector3Int[] OrthoOffsets =
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right,
        };

        public static bool IsOrthogonallyAdjacent(Vector3Int from, Vector3Int to)
        {
            Vector3Int delta = to - from;
            if (delta.z != 0)
                return false;

            return Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1;
        }

        public static void CopyNeighborCells(Vector3Int from, System.Collections.Generic.List<Vector3Int> dest)
        {
            dest.Clear();
            for (int i = 0; i < OrthoOffsets.Length; i++)
                dest.Add(from + OrthoOffsets[i]);
        }
    }
}
