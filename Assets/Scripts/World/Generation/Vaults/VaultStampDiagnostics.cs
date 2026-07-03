using System.Collections.Generic;
using System.Text;
using JRogue.Interactables;
using JRogue.Manager.Map;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation.Vaults
{
    /// <summary>
    /// Actionable vault stamp / render diagnostics tagged [DungeonGen][VaultDiag].
    /// Filter the Unity Console with "VaultDiag" after generating Floor 1.
    /// </summary>
    public static class VaultStampDiagnostics
    {
        public const string Tag = "[DungeonGen][VaultDiag]";
        public const string MonumentRenderTag = "[DungeonGen][VaultDiag][MonumentRender]";

        public const string MonumentVaultId = "vault_monument_8x8";

        static readonly string[] ProductionTileKeys =
        {
            "DcssCavern:grey_dirt_0_new",
            "DcssCavern:stone2_gray_2_new",
            "DcssCavern:shoals_shallow_water_1_new",
            "DcssCavern:shoals_shallow_water_2_new",
            "DcssCavern:shoals_shallow_water_3_new",
            "DcssCavern:shoals_shallow_water_4_new",
            "DcssCavern:_cyan_floor_nerves_2_new",
            "DcssCavern:_cyan_floor_nerves_4_new",
        };

        public static void LogRegistryAudit(VaultAssetRegistry registry, string catalogLabel)
        {
            if (registry == null)
            {
                Debug.LogWarning($"{Tag} RegistryAudit catalog={catalogLabel}: registry is null.");
                return;
            }

            registry.RebuildLookups();
            var log = new StringBuilder();
            log.Append("RegistryAudit catalog=").Append(catalogLabel).Append(" keys=[");
            for (int i = 0; i < ProductionTileKeys.Length; i++)
            {
                if (i > 0)
                    log.Append("; ");

                string key = ProductionTileKeys[i];
                bool resolved = registry.TryResolveTile(key, out TileBase tile);
                log.Append(key).Append('=').Append(resolved ? DescribeTile(tile) : "MISSING");
            }

            log.Append(']');
            Debug.Log($"{Tag} {log}");
        }

        public static void LogPaintCell(
            string vaultId,
            Vector3Int world,
            VaultCellKind kind,
            string tileKey,
            TileBase resolvedTile,
            MapManager map,
            string beforeSummary)
        {
            if (map == null)
                return;

            Debug.Log(
                $"{Tag} Paint vault={vaultId} {kind} world=({world.x},{world.y}) key='{tileKey}' " +
                $"resolved={DescribeTile(resolvedTile)} before=[{beforeSummary}] after=[{DescribeCell(map, world)}]");
        }

        public static void LogStampComplete(
            string vaultId,
            Vector3Int origin,
            VaultBlueprint blueprint,
            MapManager map,
            int cellCount)
        {
            Debug.Log(
                $"{Tag} StampComplete vault={vaultId} origin=({origin.x},{origin.y}) cells={cellCount} " +
                $"footprint={SummarizeFootprint(blueprint, origin, map)}");
        }

        public static void LogStampFailed(string vaultId, Vector3Int origin, string error)
        {
            Debug.LogWarning($"{Tag} StampFailed vault={vaultId} origin=({origin.x},{origin.y}) error={error}");
        }

        public static void LogPlacedVaultsAudit(
            IReadOnlyList<VaultPlacementRecord> records,
            MapManager map,
            string stageLabel)
        {
            if (records == null || records.Count == 0)
            {
                Debug.Log($"{Tag} FootprintAudit stage={stageLabel}: no placed vault records.");
                return;
            }

            if (map == null)
            {
                Debug.LogWarning($"{Tag} FootprintAudit stage={stageLabel}: MapManager null.");
                return;
            }

            int issueCount = 0;
            bool countFogAlphaAsIssue = stageLabel != "afterVisibilityRefresh";
            var log = new StringBuilder();
            log.Append("FootprintAudit stage=").Append(stageLabel).Append(" vaults=").Append(records.Count);

            for (int i = 0; i < records.Count; i++)
            {
                VaultPlacementRecord record = records[i];
                log.Append(" | ").Append(record.VaultId).Append('@').Append(record.Origin.x).Append(',')
                    .Append(record.Origin.y).Append(" cells=[");

                IReadOnlyList<Vector3Int> cells = record.FootprintCells;
                for (int c = 0; c < cells.Count; c++)
                {
                    Vector3Int cell = cells[c];
                    CellAudit audit = AuditCell(map, cell, countFogAlphaAsIssue);
                    if (c > 0)
                        log.Append("; ");

                    log.Append('(').Append(cell.x).Append(',').Append(cell.y).Append(' ')
                        .Append(audit.Summary).Append(')');
                    if (audit.HasIssue)
                        issueCount++;
                }

                log.Append(']');
            }

            if (issueCount > 0)
                Debug.LogWarning($"{Tag} {log} ISSUES={issueCount}");
            else
                Debug.Log($"{Tag} {log} ok");
        }

        public static void LogVisibilityAudit(
            IReadOnlyList<VaultPlacementRecord> records,
            MapManager map,
            VisibilityManager visibility,
            string stageLabel)
        {
            if (records == null || records.Count == 0 || map == null)
                return;

            var log = new StringBuilder();
            log.Append("VisibilityAudit stage=").Append(stageLabel);
            int issueCount = 0;

            for (int i = 0; i < records.Count; i++)
            {
                VaultPlacementRecord record = records[i];
                bool isPond = record.VaultId != null && record.VaultId.StartsWith("vault_pond_");
                bool isMonument = record.VaultId == "vault_monument_8x8";
                if (!isPond && !isMonument)
                    continue;

                log.Append(" | ").Append(record.VaultId).Append(" [");
                IReadOnlyList<Vector3Int> cells = record.FootprintCells;
                for (int c = 0; c < cells.Count; c++)
                {
                    Vector3Int cell = cells[c];
                    bool visible = visibility != null && visibility.IsVisible(cell);
                    CellAudit audit = AuditCell(map, cell);
                    if (c > 0)
                        log.Append("; ");

                    log.Append('(').Append(cell.x).Append(',').Append(cell.y).Append(" vis=")
                        .Append(visible).Append(' ').Append(audit.Summary).Append(')');

                    if (audit.FloorAlphaNearZero && visible)
                        issueCount++;
                    if (!audit.HasFloor && !audit.HasWall)
                        issueCount++;
                }

                log.Append(']');
            }

            if (issueCount > 0)
                Debug.LogWarning($"{Tag} {log} VIS_ISSUES={issueCount}");
            else
                Debug.Log($"{Tag} {log}");
        }

        public static void LogFloorScanForVaultTiles(MapManager map, string stageLabel)
        {
            if (map?.FloorMap == null)
                return;

            Tilemap floor = map.FloorMap;
            int shoals = 0;
            int glow = 0;
            int alphaZero = 0;
            int missingSprite = 0;
            var samples = new StringBuilder();

            foreach (Vector3Int pos in floor.cellBounds.allPositionsWithin)
            {
                if (!floor.HasTile(pos))
                    continue;

                TileBase tile = floor.GetTile(pos);
                string name = tile != null ? tile.name : "?";
                bool isShoals = name.Contains("shoals");
                bool isGlow = name.Contains("cyan_floor_nerves") || name.Contains("floor_nerves");
                if (!isShoals && !isGlow)
                    continue;

                if (isShoals)
                    shoals++;
                if (isGlow)
                    glow++;

                Color color = floor.GetColor(pos);
                if (color.a < 0.01f)
                    alphaZero++;

                if (tile is Tile unityTile && unityTile.sprite == null)
                    missingSprite++;

                if (samples.Length < 512)
                {
                    if (samples.Length > 0)
                        samples.Append("; ");

                    samples.Append('(').Append(pos.x).Append(',').Append(pos.y).Append(' ')
                        .Append(DescribeTile(tile)).Append(" a=").Append(color.a.ToString("F2")).Append(')');
                }
            }

            Debug.Log(
                $"{Tag} FloorScan stage={stageLabel} shoals={shoals} glow={glow} alphaZero={alphaZero} " +
                $"missingSprite={missingSprite} samples=[{samples}]");
        }

        static string SummarizeFootprint(VaultBlueprint blueprint, Vector3Int origin, MapManager map)
        {
            if (blueprint == null || map == null)
                return "?";

            int floorCells = 0;
            int wallCells = 0;
            int missingFloor = 0;
            int missingWall = 0;
            int alphaZero = 0;

            foreach (VaultMapCell cell in blueprint.OccupiedCells())
            {
                Vector3Int world = blueprint.LocalToWorld(origin, cell.X, cell.Y);
                CellAudit audit = AuditCell(map, world);
                if (cell.Kind == VaultCellKind.Wall)
                {
                    wallCells++;
                    if (!audit.HasWall)
                        missingWall++;
                }
                else if (cell.Kind == VaultCellKind.Floor || cell.Kind == VaultCellKind.Door)
                {
                    floorCells++;
                    if (!audit.HasFloor)
                        missingFloor++;
                }

                if (audit.FloorAlphaNearZero || audit.WallAlphaNearZero)
                    alphaZero++;
            }

            return $"floor={floorCells} wall={wallCells} missingFloor={missingFloor} missingWall={missingWall} alpha0={alphaZero}";
        }

        public static string DescribeCell(MapManager map, Vector3Int cell)
        {
            CellAudit audit = AuditCell(map, cell);
            return audit.Summary;
        }

        static CellAudit AuditCell(MapManager map, Vector3Int cell, bool countFogAlphaAsIssue = true)
        {
            Tilemap floor = map.FloorMap;
            Tilemap wall = map.WallMap;
            TileBase floorTile = floor != null && floor.HasTile(cell) ? floor.GetTile(cell) : null;
            TileBase wallTile = wall != null && wall.HasTile(cell) ? wall.GetTile(cell) : null;
            Color floorColor = floor != null && floor.HasTile(cell) ? floor.GetColor(cell) : default;
            Color wallColor = wall != null && wall.HasTile(cell) ? wall.GetColor(cell) : default;

            bool floorAlphaZero = floorTile != null && floorColor.a < 0.01f;
            bool wallAlphaZero = wallTile != null && wallColor.a < 0.01f;
            bool missingFloorSprite = floorTile is Tile ft && ft.sprite == null;
            bool missingWallSprite = wallTile is Tile wt && wt.sprite == null;

            var summary = new StringBuilder();
            summary.Append("F=").Append(floorTile != null ? DescribeTile(floorTile) : "none");
            summary.Append(" a=").Append(floorColor.a.ToString("F2"));
            summary.Append(" W=").Append(wallTile != null ? DescribeTile(wallTile) : "none");
            summary.Append(" wa=").Append(wallColor.a.ToString("F2"));
            if (missingFloorSprite || missingWallSprite)
                summary.Append(" MISSING_SPRITE");

            bool hasIssue = (floorTile == null && wallTile == null)
                || missingFloorSprite
                || missingWallSprite
                || (countFogAlphaAsIssue && (floorAlphaZero || wallAlphaZero));

            return new CellAudit
            {
                HasFloor = floorTile != null,
                HasWall = wallTile != null,
                FloorAlphaNearZero = floorAlphaZero,
                WallAlphaNearZero = wallAlphaZero,
                HasIssue = hasIssue,
                Summary = summary.ToString(),
            };
        }

        public static string DescribeTile(TileBase tile)
        {
            if (tile == null)
                return "null";

            if (tile is Tile unityTile)
            {
                if (unityTile.sprite == null)
                    return $"{tile.name}(MISSING_SPRITE)";

                return $"{tile.name}(sprite={unityTile.sprite.name})";
            }

            return tile.name;
        }

        /// <summary>
        /// Full per-layer render audit for monument vault cells. Filter Console: MonumentRender.
        /// Compares floor / wall / interactable overlay vs fog visibility (live vs memory).
        /// </summary>
        public static void LogMonumentVaultRenderAudit(
            IReadOnlyList<VaultPlacementRecord> records,
            MapManager map,
            VisibilityManager visibility,
            InteractableTileService interactables,
            string stageLabel)
        {
            if (records == null || map == null)
                return;

            VaultPlacementRecord? monument = null;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].VaultId == MonumentVaultId)
                {
                    monument = records[i];
                    break;
                }
            }

            if (monument == null)
                return;

            Tilemap interactableOverlay = interactables != null
                ? map.InteractableOverlayMap
                : null;

            var log = new StringBuilder();
            log.Append("MonumentRenderAudit stage=").Append(stageLabel);
            log.Append(" origin=").Append(monument.Value.Origin.x).Append(',').Append(monument.Value.Origin.y);

            int issueCount = 0;
            IReadOnlyList<Vector3Int> cells = monument.Value.FootprintCells;
            for (int c = 0; c < cells.Count; c++)
            {
                Vector3Int cell = cells[c];
                MonumentCellRenderStack stack = BuildMonumentCellRenderStack(
                    cell,
                    map,
                    interactableOverlay,
                    interactables,
                    visibility);

                log.Append(" | cell=(").Append(cell.x).Append(',').Append(cell.y).Append(") ")
                    .Append(stack.Summary);

                if (stack.HasLikelyRenderIssue)
                    issueCount++;
            }

            if (issueCount > 0)
                Debug.LogWarning($"{MonumentRenderTag} {log} LIKELY_ISSUES={issueCount}");
            else
                Debug.Log($"{MonumentRenderTag} {log} ok");
        }

        public static void LogMonumentInteractableOverlayPaint(
            Vector3Int cell,
            InteractableTileInstance instance,
            Sprite paintedSprite,
            bool cellVisible,
            string spriteSource)
        {
            if (instance?.Definition == null
                || instance.Definition.interactableId != InteractableTileId.BumpMonumentInscription)
            {
                return;
            }

            MapManager map = MapManager.Instance;
            VisibilityManager visibility = Object.FindAnyObjectByType<VisibilityManager>();
            Tilemap overlay = map?.InteractableOverlayMap;
            MonumentCellRenderStack stack = BuildMonumentCellRenderStack(
                cell,
                map,
                overlay,
                InteractableTileService.Instance,
                visibility);

            Debug.Log(
                $"{MonumentRenderTag} InteractableOverlayPaint cell=({cell.x},{cell.y}) " +
                $"visible={cellVisible} spriteSource={spriteSource} " +
                $"paintedSprite={(paintedSprite != null ? paintedSprite.name : "null")} " +
                $"def.spriteOff={(instance.Definition.spriteOff != null ? instance.Definition.spriteOff.name : "null")} " +
                $"def.spriteOn={(instance.Definition.spriteOn != null ? instance.Definition.spriteOn.name : "null")} " +
                $"stack=[{stack.Summary}]");
        }

        static MonumentCellRenderStack BuildMonumentCellRenderStack(
            Vector3Int cell,
            MapManager map,
            Tilemap interactableOverlay,
            InteractableTileService interactables,
            VisibilityManager visibility)
        {
            Tilemap floor = map?.FloorMap;
            Tilemap wall = map?.WallMap;

            TileBase floorTile = floor != null && floor.HasTile(cell) ? floor.GetTile(cell) : null;
            TileBase wallTile = wall != null && wall.HasTile(cell) ? wall.GetTile(cell) : null;
            Color floorColor = floor != null && floor.HasTile(cell) ? floor.GetColor(cell) : default;
            Color wallColor = wall != null && wall.HasTile(cell) ? wall.GetColor(cell) : default;

            bool hasOverlay = interactableOverlay != null && interactableOverlay.HasTile(cell);
            TileBase overlayTile = hasOverlay ? interactableOverlay.GetTile(cell) : null;
            Color overlayColor = hasOverlay ? interactableOverlay.GetColor(cell) : default;
            Matrix4x4 overlayMatrix = hasOverlay ? interactableOverlay.GetTransformMatrix(cell) : Matrix4x4.identity;

            InteractableTileInstance interactable = null;
            if (interactables != null)
                interactables.TryGetInstance(cell, out interactable);

            bool isVisible = visibility != null && visibility.IsVisible(cell);
            bool isExplored = visibility != null && visibility.IsExplored(cell);
            bool isLitVisible = visibility != null && visibility.IsLitVisible(cell);

            string knowledge = isVisible ? "Visible" : isExplored ? "Explored" : "Unseen";
            string overlaySpriteName = DescribeOverlaySprite(overlayTile);
            bool placeholderOverlay = overlaySpriteName.Contains("Lever_Off")
                || overlaySpriteName.Contains("Lever_On")
                || (interactable?.Definition != null
                    && interactable.Definition.spriteOff == null
                    && interactable.Definition.spriteOn == null
                    && hasOverlay
                    && isVisible);

            var summary = new StringBuilder();
            summary.Append("know=").Append(knowledge);
            summary.Append(" lit=").Append(isLitVisible);
            summary.Append(" F=").Append(floorTile != null ? DescribeTile(floorTile) : "none");
            summary.Append(" fa=").Append(floorColor.a.ToString("F2"));
            summary.Append(" W=").Append(wallTile != null ? DescribeTile(wallTile) : "none");
            summary.Append(" wa=").Append(wallColor.a.ToString("F2"));
            summary.Append(" overlay=").Append(hasOverlay ? DescribeTile(overlayTile) : "none");
            summary.Append(" oSprite=").Append(overlaySpriteName);
            summary.Append(" oa=").Append(overlayColor.a.ToString("F2"));
            summary.Append(" oScale=").Append(overlayMatrix.lossyScale.x.ToString("F2"))
                .Append('x').Append(overlayMatrix.lossyScale.y.ToString("F2"));

            if (interactable?.Definition != null)
            {
                summary.Append(" interactable=").Append(interactable.Definition.displayName);
                summary.Append(" blocks=").Append(interactable.Definition.blocksOccupancy);
            }

            string diagnosis = DiagnoseMonumentRender(
                wallTile,
                hasOverlay,
                isVisible,
                isExplored,
                placeholderOverlay,
                interactable);

            summary.Append(" >> ").Append(diagnosis);

            return new MonumentCellRenderStack
            {
                Summary = summary.ToString(),
                Diagnosis = diagnosis,
                HasLikelyRenderIssue = diagnosis.StartsWith("LIKELY_CAUSE"),
            };
        }

        static string DiagnoseMonumentRender(
            TileBase wallTile,
            bool hasOverlay,
            bool isVisible,
            bool isExplored,
            bool placeholderOverlay,
            InteractableTileInstance interactable)
        {
            if (isVisible && hasOverlay && placeholderOverlay && wallTile != null)
            {
                return "LIKELY_CAUSE: interactable overlay paints placeholder (orange lever) over wall while visible; " +
                       "explored memory clears overlay so wall looks correct in fog";
            }

            if (isVisible && hasOverlay && wallTile != null)
            {
                return "LIKELY_CAUSE: interactable overlay tile visible on top of wall tile (check overlay sprite / sorting)";
            }

            if (isVisible && wallTile == null && interactable != null)
            {
                return "LIKELY_CAUSE: monument wall missing on wall layer while interactable registered";
            }

            if (isVisible && wallTile != null && !hasOverlay && interactable != null)
            {
                return "OK: wall tile visible, interactable overlay cleared/hidden";
            }

            if (isExplored && wallTile != null && !hasOverlay)
            {
                return "OK: explored memory shows wall tile only (overlay hidden when not visible)";
            }

            if (isVisible && wallTile != null && !hasOverlay)
            {
                return "OK: wall tile visible without overlay";
            }

            return "INFO: no monument render anomaly detected for this cell state";
        }

        static string DescribeOverlaySprite(TileBase overlayTile)
        {
            if (overlayTile == null)
                return "none";

            if (overlayTile is Tile unityTile)
            {
                if (unityTile.sprite == null)
                    return $"{overlayTile.name}(MISSING_SPRITE)";

                return unityTile.sprite.name;
            }

            return overlayTile.name;
        }

        struct MonumentCellRenderStack
        {
            public string Summary;
            public string Diagnosis;
            public bool HasLikelyRenderIssue;
        }

        struct CellAudit
        {
            public bool HasFloor;
            public bool HasWall;
            public bool FloorAlphaNearZero;
            public bool WallAlphaNearZero;
            public bool HasIssue;
            public string Summary;
        }
    }

    public struct VaultPlacementRecord
    {
        public string VaultId;
        public Vector3Int Origin;
        public List<Vector3Int> FootprintCells;
    }
}
