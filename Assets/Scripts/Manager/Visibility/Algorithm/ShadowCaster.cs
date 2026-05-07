using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Manager.Visibility.Algorithm
{
    public static class ShadowCaster
    {
        // Define the 8 octants
        private static readonly int[,] Multipliers = {
        {1, 0, 0, 1}, {0, 1, 1, 0}, {0, -1, 1, 0}, {-1, 0, 0, 1},
        {-1, 0, 0, -1}, {0, -1, -1, 0}, {0, 1, -1, 0}, {1, 0, 0, -1}
    };

        public delegate bool IsOpaque(Vector3Int pos);

        public static List<Vector3Int> GetVisibleTiles(Vector3Int origin, int range, IsOpaque isOpaque)
        {
            List<Vector3Int> visibleTiles = new List<Vector3Int> { origin };

            for (int octant = 0; octant < 8; octant++)
            {
                Scan(origin, range, 1, 1.0f, 0.0f,
                    Multipliers[octant, 0], Multipliers[octant, 1],
                    Multipliers[octant, 2], Multipliers[octant, 3],
                    isOpaque, visibleTiles);
            }

            return visibleTiles;
        }

        private static void Scan(Vector3Int origin, int range, int row, float startSlope, float endSlope,
                                 int xx, int xy, int yx, int yy,
                                 IsOpaque isOpaque, List<Vector3Int> visibleTiles)
        {
            if (startSlope < endSlope) return;

            float nextStartSlope = startSlope;

            for (int i = row; i <= range; i++)
            {
                bool blocked = false;

                for (int dx = -i, dy = -i; dx <= 0; dx++)
                {
                    float lSlope = (dx - 0.5f) / (dy + 0.5f);
                    float rSlope = (dx + 0.5f) / (dy - 0.5f);

                    if (startSlope < rSlope) continue;
                    if (endSlope > lSlope) break;

                    // Map the relative dx, dy to actual grid coordinates using the octant multipliers
                    int sax = origin.x + dx * xx + dy * xy;
                    int say = origin.y + dx * yx + dy * yy;
                    Vector3Int currentPos = new Vector3Int(sax, say, origin.z);

                    // Strict vision: diagonal "crack" (walkable notch with solid orthogonals) does not glow,
                    // but still casts a shadow — otherwise LOS slips past via slope recursion / other wedges.
                    bool strictDiagonalLightBlock = false;
                    if (dx < 0 && dy < 0)
                    {
                        Vector3Int cardinal1 = new Vector3Int(
                            origin.x + (dx + 1) * xx + dy * xy,
                            origin.y + (dx + 1) * yx + dy * yy,
                            origin.z);
                        Vector3Int cardinal2 = new Vector3Int(
                            origin.x + dx * xx + (dy + 1) * xy,
                            origin.y + dx * yx + (dy + 1) * yy,
                            origin.z);
                        strictDiagonalLightBlock =
                            isOpaque(cardinal1) && isOpaque(cardinal2) && !isOpaque(currentPos);
                    }

                    bool blocksLight = isOpaque(currentPos) || strictDiagonalLightBlock;

                    if ((dx * dx + dy * dy) <= (range * range) && !strictDiagonalLightBlock)
                    {
                        visibleTiles.Add(currentPos);
                    }

                    if (blocked)
                    {
                        if (blocksLight)
                        {
                            nextStartSlope = rSlope;
                        }
                        else
                        {
                            blocked = false;
                            startSlope = nextStartSlope;
                        }
                    }
                    else if (blocksLight && i < range)
                    {
                        blocked = true;
                        Scan(origin, range, i + 1, startSlope, lSlope, xx, xy, yx, yy, isOpaque, visibleTiles);
                        nextStartSlope = rSlope;
                    }
                }

                if (blocked) break;
            }
        }
    }
}