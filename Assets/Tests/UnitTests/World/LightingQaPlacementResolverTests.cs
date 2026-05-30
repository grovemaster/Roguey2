using JRogue.World.Lighting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Tests.World
{
    public class LightingQaPlacementResolverTests
    {
        [Test]
        public void TryFindNearestWallCell_PicksMinimumManhattan_TieBreakLowestYThenX()
        {
            var wallGo = new GameObject("Walls");
            var wallMap = wallGo.AddComponent<Tilemap>();
            var tile = ScriptableObject.CreateInstance<Tile>();

            wallMap.SetTile(new Vector3Int(2, 0, 0), tile);
            wallMap.SetTile(new Vector3Int(0, 1, 0), tile);
            wallMap.SetTile(new Vector3Int(1, 1, 0), tile);

            Vector3Int anchor = new Vector3Int(1, 0, 0);
            Assert.IsTrue(
                LightingQaPlacementResolver.TryFindNearestWallCell(wallMap, anchor, out Vector3Int chosen));
            Assert.AreEqual(new Vector3Int(2, 0, 0), chosen);

            Object.DestroyImmediate(wallGo);
        }

        [Test]
        public void CompareTieBreak_LowerYWins()
        {
            var wallGo = new GameObject("Walls");
            var wallMap = wallGo.AddComponent<Tilemap>();
            var tile = ScriptableObject.CreateInstance<Tile>();

            wallMap.SetTile(new Vector3Int(1, 2, 0), tile);
            wallMap.SetTile(new Vector3Int(1, 0, 0), tile);

            Vector3Int anchor = new Vector3Int(1, 1, 0);
            Assert.IsTrue(
                LightingQaPlacementResolver.TryFindNearestWallCell(wallMap, anchor, out Vector3Int chosen));
            Assert.AreEqual(new Vector3Int(1, 0, 0), chosen);

            Object.DestroyImmediate(wallGo);
        }
    }
}
