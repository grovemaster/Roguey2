using System.Collections.Generic;
using JRogue.GridFeatures;
using JRogue.Manager.Door;
using JRogue.World.Generation;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Manager.Map
{
    public class MapManager : MonoBehaviour
    {
        /// <summary>
        /// Center-pivot sprites need +½ cell in local space when the tile anchor sits on the cell corner.
        /// Translation only — no scale.
        /// </summary>
        static readonly Matrix4x4 CenterPivotTileTranslate = Matrix4x4.TRS(
            new Vector3(0.5f, 0.5f, 0f),
            Quaternion.identity,
            Vector3.one);

        public static MapManager Instance { get; private set; } // Add Singleton

        [SerializeField] private Tilemap floorMap;
        [SerializeField] private Tilemap wallMap;
        [SerializeField] private Tilemap hazardOverlayMap;
        [SerializeField] private Tilemap interactableOverlayMap;
        [SerializeField] private Tilemap trapOverlayMap;
        [SerializeField] private Tilemap doorOverlayMap;

        [SerializeField] private TileBase floorPaintTile;
        [SerializeField] private TileBase wallPaintTile;

        public Tilemap FloorMap => floorMap;
        public Tilemap WallMap => wallMap;
        public Tilemap HazardOverlayMap => hazardOverlayMap;
        public Tilemap InteractableOverlayMap => interactableOverlayMap;
        public Tilemap TrapOverlayMap => trapOverlayMap;
        public Tilemap DoorOverlayMap => doorOverlayMap;
        public TileBase FloorPaintTile => floorPaintTile;
        public TileBase WallPaintTile => wallPaintTile;
        public string ActiveFloorId { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public bool IsCellBlocked(Vector3Int gridPos)
        {
            if (floorMap == null)
                return true;

            if (DoorService.Instance != null && DoorService.Instance.BlocksMovement(gridPos))
                return true;

            if (wallMap != null && wallMap.HasTile(gridPos))
                return true;

            if (!floorMap.HasTile(gridPos))
                return true;

            return false;
        }

        public bool IsWalkable(Vector3Int gridPos)
        {
            if (floorMap == null || !floorMap.HasTile(gridPos))
                return false;

            if (DoorService.Instance != null && DoorService.Instance.BlocksMovement(gridPos))
                return false;

            return wallMap == null || !wallMap.HasTile(gridPos);
        }

        public bool IsWall(Vector3Int gridPos)
        {
            return wallMap != null && wallMap.HasTile(gridPos);
        }

        public void ConfigurePaintTiles(TileBase floorTile, TileBase wallTile)
        {
            if (floorTile != null)
                floorPaintTile = floorTile;
            if (wallTile != null)
                wallPaintTile = wallTile;
        }

        public void SetActiveFloor(DungeonFloorTilemaps tilemaps, string floorId)
        {
            if (tilemaps == null)
                return;

            BindTilemaps(
                tilemaps.FloorMap,
                tilemaps.WallMap,
                tilemaps.HazardOverlayMap,
                tilemaps.InteractableOverlayMap,
                tilemaps.TrapOverlayMap,
                tilemaps.DoorOverlayMap);

            ActiveFloorId = floorId;
            LogBoundTilemaps("SetActiveFloor");
        }

        public void BindTilemaps(
            Tilemap floor,
            Tilemap wall,
            Tilemap hazard = null,
            Tilemap interactable = null,
            Tilemap trap = null,
            Tilemap door = null)
        {
            floorMap = floor;
            wallMap = wall;
            hazardOverlayMap = hazard;
            interactableOverlayMap = interactable;
            trapOverlayMap = trap;
            doorOverlayMap = door;

            if (floorMap != null)
                GridOverlayPainter.ConfigureRenderer(floorMap, sortingOrder: 0);
            if (wallMap != null)
                GridOverlayPainter.ConfigureRenderer(wallMap, sortingOrder: 1);

            LogBoundTilemaps("BindTilemaps");
        }

        void LogBoundTilemaps(string context)
        {
            Debug.Log(
                $"[TileDebug] MapManager.{context} activeFloorId={ActiveFloorId}\n" +
                $"  floor={DescribeBoundTilemap(floorMap)}\n" +
                $"  wall={DescribeBoundTilemap(wallMap)}");
        }

        static string DescribeBoundTilemap(Tilemap tilemap)
        {
            if (tilemap == null)
                return "null";

            UnityEngine.Grid grid = tilemap.layoutGrid;
            return $"name={tilemap.name} id={tilemap.GetInstanceID()} anchor={tilemap.tileAnchor} " +
                $"worldPos={tilemap.transform.position} lossyScale={tilemap.transform.lossyScale} " +
                $"layoutGridId={(grid != null ? grid.GetInstanceID().ToString() : "null")} " +
                $"cellSize={(grid != null ? grid.cellSize.ToString() : "n/a")} " +
                $"cellGap={(grid != null ? grid.cellGap.ToString() : "n/a")} " +
                $"cellLayout={(grid != null ? grid.cellLayout.ToString() : "n/a")} " +
                $"cellSwizzle={(grid != null ? grid.cellSwizzle.ToString() : "n/a")}";
        }

        public void ClearAllTiles()
        {
            if (floorMap == null)
            {
                Debug.LogWarning("[MapManager] ClearAllTiles skipped — floorMap not bound. Call SetActiveFloor first.");
                return;
            }

            floorMap.ClearAllTiles();
            wallMap?.ClearAllTiles();
            hazardOverlayMap?.ClearAllTiles();
            interactableOverlayMap?.ClearAllTiles();
            trapOverlayMap?.ClearAllTiles();
            doorOverlayMap?.ClearAllTiles();
        }

        public void SetCellFloor(Vector3Int cell)
        {
            if (floorMap == null || floorPaintTile == null)
                return;

            PaintCell(floorMap, cell, floorPaintTile);
            wallMap?.SetTile(cell, null);
            wallMap?.SetTransformMatrix(cell, Matrix4x4.identity);
        }

        public void SetCellWall(Vector3Int cell)
        {
            if (wallMap == null || wallPaintTile == null)
                return;

            PaintCell(wallMap, cell, wallPaintTile);
            floorMap?.SetTile(cell, null);
            floorMap?.SetTransformMatrix(cell, Matrix4x4.identity);
        }

        static readonly HashSet<Vector3Int> PaintVerifyCells = new HashSet<Vector3Int>
        {
            new Vector3Int(15, 1, 0),
            new Vector3Int(28, 1, 0),
        };

        static void PaintCell(Tilemap tilemap, Vector3Int cell, TileBase tile)
        {
            tilemap.SetTile(cell, tile);
            tilemap.SetTransformMatrix(cell, CenterPivotTileTranslate);

            if (PaintVerifyCells.Contains(cell))
                LogPaintCellVerify(tilemap, cell, tile);
        }

        static void LogPaintCellVerify(Tilemap tilemap, Vector3Int cell, TileBase tile)
        {
            Matrix4x4 stored = tilemap.GetTransformMatrix(cell);
            bool matrixMatches = MatricesApproximatelyEqual(stored, CenterPivotTileTranslate);
            Matrix4x4 tileAssetMatrix = tile is Tile tileObj ? tileObj.transform : Matrix4x4.identity;

            Debug.Log(
                $"[TileDiag] PaintCell verify cell={cell} tilemap={tilemap.name}\n" +
                $"  storedMatrixTranslation={stored.GetColumn(3)} storedMatrixScale=({stored.GetColumn(0).magnitude:F3},{stored.GetColumn(1).magnitude:F3},{stored.GetColumn(2).magnitude:F3})\n" +
                $"  expectedTranslation=(0.5,0.5,0) matrixMatchesExpected={matrixMatches}\n" +
                $"  tileAsset.transform={tileAssetMatrix} tileAssetIsIdentity={tileAssetMatrix == Matrix4x4.identity}\n" +
                $"  HasTile={tilemap.HasTile(cell)} GetTile={(tilemap.GetTile(cell) != null ? tilemap.GetTile(cell).name : "null")}");
        }

        public void PaintLayoutStamp(DungeonLayoutStamp stamp)
        {
            if (stamp == null)
                return;

            Debug.Log(
                $"[TileDebug] PaintLayoutStamp begin stamp={stamp.Width}x{stamp.Height} " +
                $"floorPaintTile={(floorPaintTile != null ? floorPaintTile.name : "null")} " +
                $"wallPaintTile={(wallPaintTile != null ? wallPaintTile.name : "null")}");
            LogBoundTilemaps("PaintLayoutStamp-beforeClear");

            ClearAllTiles();

            for (int y = 0; y < stamp.Height; y++)
            {
                for (int x = 0; x < stamp.Width; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (stamp.IsWall(x, y))
                        SetCellWall(cell);
                    else if (stamp.IsFloor(x, y))
                        SetCellFloor(cell);
                }
            }

            floorMap?.CompressBounds();
            wallMap?.CompressBounds();

            LogBoundTilemaps("PaintLayoutStamp-afterPaint");
            LogTilemapCompressionAndRenderer(floorMap, wallMap);
            LogFloorTileAlignmentDebug(floorMap, wallMap, floorPaintTile, stamp);
        }

        /// <summary>
        /// Temporary diagnostics: compare grid cell indices to Tilemap world positions and sprite bounds.
        /// </summary>
        static void LogTilemapCompressionAndRenderer(Tilemap floorTilemap, Tilemap wallTilemap)
        {
            if (floorTilemap == null)
                return;

            TilemapRenderer floorRenderer = floorTilemap.GetComponent<TilemapRenderer>();
            TilemapRenderer wallRenderer = wallTilemap != null ? wallTilemap.GetComponent<TilemapRenderer>() : null;

            Debug.Log(
                $"[TileDiag] tilemap state after CompressBounds\n" +
                $"  floor origin={floorTilemap.origin} size={floorTilemap.size} tileAnchor={floorTilemap.tileAnchor}\n" +
                $"  floorRenderer mode={(floorRenderer != null ? floorRenderer.mode.ToString() : "null")} " +
                $"sortOrder={(floorRenderer != null ? floorRenderer.sortingOrder.ToString() : "n/a")} " +
                $"maskInteraction={(floorRenderer != null ? floorRenderer.maskInteraction.ToString() : "n/a")}\n" +
                $"  wallRenderer sortOrder={(wallRenderer != null ? wallRenderer.sortingOrder.ToString() : "n/a")} " +
                $"  floor.localToWorldMatrix={floorTilemap.transform.localToWorldMatrix}");
        }

        static void LogFloorTileAlignmentDebug(
            Tilemap floorTilemap,
            Tilemap wallTilemap,
            TileBase floorTile,
            DungeonLayoutStamp stamp)
        {
            if (floorTilemap == null)
            {
                Debug.LogWarning("[TileAlign] floorMap is null — skip tile alignment debug.");
                return;
            }

            UnityEngine.Grid grid = floorTilemap.layoutGrid;
            Sprite sprite = floorTile is Tile tileAsset ? tileAsset.sprite : null;
            Vector3 cellSize = grid != null ? grid.cellSize : Vector3.one;

            Vector3 strideX = floorTilemap.CellToWorld(new Vector3Int(1, 0, 0)) - floorTilemap.CellToWorld(Vector3Int.zero);
            Vector3 strideY = floorTilemap.CellToWorld(new Vector3Int(0, 1, 0)) - floorTilemap.CellToWorld(Vector3Int.zero);
            Vector3 gridStrideX = grid != null
                ? grid.CellToWorld(new Vector3Int(1, 0, 0)) - grid.CellToWorld(Vector3Int.zero)
                : Vector3.zero;
            Vector3 gridStrideY = grid != null
                ? grid.CellToWorld(new Vector3Int(0, 1, 0)) - grid.CellToWorld(Vector3Int.zero)
                : Vector3.zero;

            Debug.Log(
                $"[TileAlign] === floor tilemap '{floorTilemap.name}' id={floorTilemap.GetInstanceID()} ===\n" +
                $"  tileAnchor={floorTilemap.tileAnchor}\n" +
                $"  grid.cellSize={cellSize} grid.cellGap={(grid != null ? grid.cellGap : Vector3.zero)}\n" +
                $"  tilemapStrideX={strideX} tilemapStrideY={strideY}\n" +
                $"  gridStrideX={gridStrideX} gridStrideY={gridStrideY}\n" +
                $"  layoutGridSameRef={(grid != null && ReferenceEquals(grid, floorTilemap.layoutGrid))}\n" +
                $"  floorMap.worldPos={floorTilemap.transform.position} lossyScale={floorTilemap.transform.lossyScale}\n" +
                $"  grid.worldPos={(grid != null ? grid.transform.position : Vector3.zero)} " +
                $"grid.lossyScale={(grid != null ? grid.transform.lossyScale : Vector3.one)}\n" +
                $"  floorPaintTile={(floorTile != null ? floorTile.name : "null")} " +
                $"sprite={(sprite != null ? sprite.name : "null")} " +
                $"spritePivotPx={(sprite != null ? sprite.pivot.ToString() : "n/a")} " +
                $"spriteRect={(sprite != null ? sprite.rect.size.ToString() : "n/a")} " +
                $"ppu={(sprite != null ? sprite.pixelsPerUnit.ToString() : "n/a")}\n" +
                $"  CenterPivotTileTranslate={CenterPivotTileTranslate.GetColumn(3)}");

            var sampleCells = new List<Vector3Int>
            {
                new Vector3Int(15, 1, 0),
            };

            if (stamp != null)
            {
                sampleCells.Add(stamp.PlayerStart);
                if (stamp.TryGetMarker("portal_south", out Vector3Int portalCell))
                    sampleCells.Add(portalCell);

                for (int y = 0; y < stamp.Height; y++)
                {
                    for (int x = 0; x < stamp.Width; x++)
                    {
                        if (!stamp.IsFloor(x, y))
                            continue;

                        bool wallNorth = stamp.IsWall(x, y + 1);
                        bool wallEast = stamp.IsWall(x + 1, y);
                        if (wallNorth || wallEast)
                        {
                            sampleCells.Add(new Vector3Int(x, y, 0));
                            break;
                        }
                    }

                    if (sampleCells.Count > 4)
                        break;
                }
            }

            var logged = new HashSet<Vector3Int>();
            for (int i = 0; i < sampleCells.Count; i++)
            {
                Vector3Int cell = sampleCells[i];
                if (!logged.Add(cell) || !floorTilemap.HasTile(cell))
                    continue;

                LogFloorCellAlignment(floorTilemap, wallTilemap, grid, sprite, cell, cellSize);
            }
        }

        static void LogFloorCellAlignment(
            Tilemap floorMap,
            Tilemap wallMap,
            UnityEngine.Grid grid,
            Sprite sprite,
            Vector3Int cell,
            Vector3 cellSize)
        {
            Vector3 tilemapCellToWorld = floorMap.CellToWorld(cell);
            Vector3 tilemapCellCenter = floorMap.GetCellCenterWorld(cell);
            Vector3 gridCellToWorld = grid != null ? grid.CellToWorld(cell) : Vector3.zero;
            Vector3 gridCellCenter = grid != null ? grid.GetCellCenterWorld(cell) : Vector3.zero;
            Matrix4x4 cellMatrix = floorMap.GetTransformMatrix(cell);
            bool matrixMatches = MatricesApproximatelyEqual(cellMatrix, CenterPivotTileTranslate);

            Vector3 cellBoxMin = tilemapCellCenter - cellSize * 0.5f;
            Vector3 cellBoxMax = tilemapCellCenter + cellSize * 0.5f;
            Vector3 gridCellBoxMin = gridCellCenter - cellSize * 0.5f;
            Vector3 gridCellBoxMax = gridCellCenter + cellSize * 0.5f;
            Vector3 cellStrideX = floorMap.CellToWorld(cell + Vector3Int.right) - tilemapCellToWorld;
            Vector3 cellStrideY = floorMap.CellToWorld(cell + Vector3Int.up) - tilemapCellToWorld;

            Vector3Int north = cell + Vector3Int.up;
            Vector3Int south = cell + Vector3Int.down;
            bool wallNorth = wallMap != null && wallMap.HasTile(north);
            bool wallSouth = wallMap != null && wallMap.HasTile(south);
            bool floorNorth = floorMap.HasTile(north);
            bool floorSouth = floorMap.HasTile(south);

            TileBase placedTile = floorMap.GetTile(cell);
            Sprite placedSprite = placedTile is Tile placedTileAsset ? placedTileAsset.sprite : null;
            Matrix4x4 placedTileTransform = placedTile is Tile placedTileObj
                ? placedTileObj.transform
                : Matrix4x4.identity;

            string placedTileInfo = placedTile == null
                ? "GetTile=null"
                : $"GetTile={placedTile.name} placedSprite={(placedSprite != null ? placedSprite.name : "null")} " +
                  $"placedPivot={(placedSprite != null ? placedSprite.pivot.ToString() : "n/a")} " +
                  $"placedTile.transform={placedTileTransform} " +
                  $"sprite.bounds.size={(placedSprite != null ? placedSprite.bounds.size.ToString() : "n/a")} " +
                  $"sprite.textureRect={(placedSprite != null ? placedSprite.textureRect.ToString() : "n/a")}";

            string boundsReport = BuildSpriteBoundsReport(
                sprite,
                floorMap,
                grid,
                cell,
                cellSize,
                tilemapCellToWorld,
                tilemapCellCenter,
                cellMatrix);

            Vector3 centerDelta = tilemapCellCenter - gridCellCenter;
            Vector3 anchorDelta = tilemapCellToWorld - gridCellCenter;

            Debug.Log(
                $"[TileDiag] cell={cell}\n" +
                $"  tilemap.CellToWorld={tilemapCellToWorld}\n" +
                $"  tilemap.GetCellCenterWorld={tilemapCellCenter}\n" +
                $"  grid.CellToWorld={gridCellToWorld} grid.GetCellCenterWorld={gridCellCenter}\n" +
                $"  delta(tilemapCenter-gridCenter)={centerDelta} delta(tilemapAnchor-gridCenter)={anchorDelta}\n" +
                $"  cellStrideFromCellToWorld: +X={cellStrideX} +Y={cellStrideY}\n" +
                $"  cellBox(tilemapCenter±half) min={cellBoxMin} max={cellBoxMax}\n" +
                $"  cellBox(gridCenter±half) min={gridCellBoxMin} max={gridCellBoxMax}\n" +
                $"  storedMatrix translation={cellMatrix.GetColumn(3)} matchesCenterPivotTranslate={matrixMatches}\n" +
                $"  neighbors: floorN={floorNorth} wallN={wallNorth} floorS={floorSouth} wallS={wallSouth}\n" +
                $"  {placedTileInfo}\n" +
                boundsReport);
        }

        static string BuildSpriteBoundsReport(
            Sprite sprite,
            Tilemap floorMap,
            UnityEngine.Grid grid,
            Vector3Int cell,
            Vector3 cellSize,
            Vector3 tilemapCellToWorld,
            Vector3 tilemapCellCenter,
            Matrix4x4 cellMatrix)
        {
            if (sprite == null)
                return "spriteBounds=no-sprite";

            float worldW = sprite.rect.width / sprite.pixelsPerUnit;
            float worldH = sprite.rect.height / sprite.pixelsPerUnit;
            var pivotNorm = new Vector2(
                sprite.pivot.x / sprite.rect.width,
                sprite.pivot.y / sprite.rect.height);

            // Model A (legacy): pivot at CellToWorld, ignores per-cell matrix.
            Vector3 pivotA = tilemapCellToWorld;
            Vector3 minA = pivotA - new Vector3(worldW * pivotNorm.x, worldH * pivotNorm.y, 0f);
            Vector3 maxA = minA + new Vector3(worldW, worldH, 0f);

            // Model B: matrix translation applied from CellToWorld (corner base).
            Vector3 matrixOffsetWorld = grid != null
                ? grid.transform.TransformVector(Vector3.Scale(cellMatrix.GetColumn(3), cellSize))
                : Vector3.Scale(cellMatrix.GetColumn(3), cellSize);
            Vector3 pivotB = tilemapCellToWorld + matrixOffsetWorld;
            Vector3 minB = pivotB - new Vector3(worldW * pivotNorm.x, worldH * pivotNorm.y, 0f);
            Vector3 maxB = minB + new Vector3(worldW, worldH, 0f);

            Vector3 anchorLocal = floorMap.tileAnchor;
            Vector3 matrixLocal = cellMatrix.GetColumn(3);
            Vector3 anchorOffsetWorld = grid != null
                ? grid.transform.TransformVector(Vector3.Scale(anchorLocal, cellSize))
                : Vector3.Scale(anchorLocal, cellSize);

            // Model C: corner + tileAnchor + matrix (if CellToWorld is the cell corner).
            Vector3 pivotC = tilemapCellToWorld + anchorOffsetWorld + matrixOffsetWorld;
            Vector3 minC = pivotC - new Vector3(worldW * pivotNorm.x, worldH * pivotNorm.y, 0f);
            Vector3 maxC = minC + new Vector3(worldW, worldH, 0f);

            // Model E: tileAnchor world point + matrix only (Unity: matrix origin is tile anchor in cell).
            Vector3 tileAnchorWorld = tilemapCellToWorld + anchorOffsetWorld;
            Vector3 pivotE = tileAnchorWorld + matrixOffsetWorld;
            Vector3 minE = pivotE - new Vector3(worldW * pivotNorm.x, worldH * pivotNorm.y, 0f);
            Vector3 maxE = minE + new Vector3(worldW, worldH, 0f);

            // Model D: pivot at GetCellCenterWorld (what portal uses).
            Vector3 pivotD = tilemapCellCenter;
            Vector3 minD = pivotD - new Vector3(worldW * pivotNorm.x, worldH * pivotNorm.y, 0f);
            Vector3 maxD = minD + new Vector3(worldW, worldH, 0f);

            Vector3 gridCenter = grid != null ? grid.GetCellCenterWorld(cell) : tilemapCellCenter;
            Vector3 targetMin = gridCenter - cellSize * 0.5f;
            Vector3 targetMax = gridCenter + cellSize * 0.5f;

            return "spriteBounds models (XY overlap vs grid cell box; 1.0=full cell):\n" +
                $"  A=noMatrixFromCorner pivot={pivotA} overlapXY={OverlapAreaXY(minA, maxA, targetMin, targetMax):F3}\n" +
                $"  B=corner+matrixOnly pivot={pivotB} overlapXY={OverlapAreaXY(minB, maxB, targetMin, targetMax):F3}\n" +
                $"  C=corner+anchor+matrix pivot={pivotC} overlapXY={OverlapAreaXY(minC, maxC, targetMin, targetMax):F3}\n" +
                $"  D=tilemapGetCellCenter pivot={pivotD} overlapXY={OverlapAreaXY(minD, maxD, targetMin, targetMax):F3}\n" +
                $"  E=tileAnchorWorld+matrix pivot={pivotE} tileAnchorWorld={tileAnchorWorld} overlapXY={OverlapAreaXY(minE, maxE, targetMin, targetMax):F3}\n" +
                $"  targetCellBox(gridCenter±half) min={targetMin} max={targetMax} anchorLocal={anchorLocal} matrixLocal={matrixLocal}\n" +
                $"  => whichever model has overlapXY≈1.0 matches how Unity is drawing; if all low, cause is not pivot math (renderer/art/wall).";
        }

        static float OverlapAreaXY(Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB)
        {
            float overlapW = Mathf.Max(0f, Mathf.Min(maxA.x, maxB.x) - Mathf.Max(minA.x, minB.x));
            float overlapH = Mathf.Max(0f, Mathf.Min(maxA.y, maxB.y) - Mathf.Max(minA.y, minB.y));
            float cellArea = Mathf.Max(0.0001f, (maxB.x - minB.x) * (maxB.y - minB.y));
            return (overlapW * overlapH) / cellArea;
        }

        static bool MatricesApproximatelyEqual(Matrix4x4 a, Matrix4x4 b, float epsilon = 0.001f)
        {
            for (int i = 0; i < 4; i++)
            {
                if ((a.GetColumn(i) - b.GetColumn(i)).sqrMagnitude > epsilon * epsilon)
                    return false;
            }

            return true;
        }
    }
}