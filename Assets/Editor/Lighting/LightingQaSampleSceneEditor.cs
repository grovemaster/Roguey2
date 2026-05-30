#if UNITY_EDITOR
using System.Collections.Generic;
using JRogue.Interactables;
using JRogue.Manager.Map;
using JRogue.World.Lighting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.Lighting
{
    public static class LightingQaSampleSceneEditor
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string LightingSystemName = "LightingSystem";
        const string Phase3RootName = "LightingPhase_Phase3_RuntimeEmitters";
        const string DarkPocketRootName = "LightingTest_DarkPocket";
        const string WallTorchObjectName = "WallTorch_Test";

        const string TorchDefinitionPath = "Assets/Prefabs/Lighting/Torch.asset";
        const string WallTorchInteractablePath = "Assets/Prefabs/Lighting/WallTorchInteractable.asset";

        const int Phase3ScenarioIndex = 2;
        const int RoomSize = 5;

        [MenuItem("JRogue/Lighting/Place Wall Torch Near Tiefling Mage (SampleScene)", false, 20)]
        public static void PlaceWallTorchNearTiefling()
        {
            PlaceWallTorchNearTieflingInternal(ensureHarness: true);
        }

        [MenuItem("JRogue/Lighting/Create Dark QA Pocket Near Tiefling (SampleScene)", false, 21)]
        public static void CreateDarkQaPocketNearTiefling()
        {
            CreateDarkQaPocketNearTieflingInternal();
        }

        [MenuItem("JRogue/Lighting/Carve Path To Dark QA Pocket (SampleScene)", false, 23)]
        public static void CarvePathToDarkQaPocket()
        {
            if (!OpenSampleScene(out Scene scene))
                return;

            if (!TryResolvePlacement(out Vector3Int anchor, out Vector3Int torchWall, out _))
                return;

            MapManager map = Object.FindAnyObjectByType<MapManager>();
            if (map == null || map.WallMap == null || map.FloorMap == null)
            {
                Debug.LogError("[Lighting:QA] MapManager floor/wall maps missing.");
                return;
            }

            if (!TryResolveDarkPocketDoorway(anchor, torchWall, out Vector3Int doorway))
            {
                Vector3Int roomOrigin = new Vector3Int(anchor.x + 3, anchor.y - 2, 0);
                doorway = ResolveWestDoorway(roomOrigin, anchor, torchWall);
            }

            ReplaceWallWithFloor(map, doorway);
            MarkDirty(scene, map.WallMap, map.FloorMap);
            Debug.Log(
                $"[Lighting:QA] Doorway at {doorway}: wall removed, floor painted. "
                + $"Walk from lit area into dark room at {doorway + Vector3Int.right}. Torch wall: {torchWall}.");
        }

        [MenuItem("JRogue/Lighting/Apply Dark QA Lighting Profile", false, 22)]
        public static void ApplyDarkQaLightingProfile()
        {
            ApplyDarkQaLightingProfileInternal(ensureHarness: true);
        }

        [MenuItem("JRogue/Lighting/Setup Lighting QA (All SampleScene Steps)", false, 10)]
        public static void SetupLightingQaAll()
        {
            if (!OpenSampleScene(out Scene scene))
                return;

            Undo.SetCurrentGroupName("Setup Lighting QA");
            int undoGroup = Undo.GetCurrentGroup();

            LightingScenarioSampleSceneBootstrap.EnsureLightingHarness(
                applyScenarioIndex: null,
                selectLightingSystem: false);

            CreateDarkQaPocketNearTieflingInternal();
            PlaceWallTorchNearTieflingInternal(ensureHarness: false);
            ApplyDarkQaLightingProfileInternal(ensureHarness: false);

            GameObject lightingSystem = FindOrCreateLightingSystem();
            LightingScenarioController controller =
                lightingSystem.GetComponent<LightingScenarioController>();
            if (controller != null)
                controller.ApplyScenarioByIndex(Phase3ScenarioIndex);

            EnsureDarkPocketActive(lightingSystem.transform);
            MarkDirty(scene, lightingSystem);
            Selection.activeGameObject = lightingSystem;
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log(
                "[Lighting:QA] All steps complete. Phase 3 active; dark pocket + wall torch near Tiefling. Press Play and use L for debug overlay.");
        }

        static void PlaceWallTorchNearTieflingInternal(bool ensureHarness)
        {
            if (!OpenSampleScene(out Scene scene))
                return;

            if (!TryResolvePlacement(out Vector3Int anchor, out Vector3Int wallCell, out Vector3Int floorCell))
                return;

            if (ensureHarness)
                LightingScenarioSampleSceneBootstrap.EnsureLightingHarness(
                    applyScenarioIndex: Phase3ScenarioIndex,
                    selectLightingSystem: false);

            GameObject lightingSystem = FindOrCreateLightingSystem();
            Transform phase3 = EnsureChild(lightingSystem.transform, Phase3RootName);
            LightingPhase3SampleContent content = EnsureWallTorchContent(phase3);
            if (content == null)
            {
                Debug.LogError("[Lighting:QA] Failed to create WallTorch_Test content.");
                return;
            }

            LightEmitterDefinition torchDef =
                AssetDatabase.LoadAssetAtPath<LightEmitterDefinition>(TorchDefinitionPath);
            InteractableTileDefinition interactable =
                AssetDatabase.LoadAssetAtPath<InteractableTileDefinition>(WallTorchInteractablePath);

            ApplySerialized(
                content,
                so =>
                {
                    so.FindProperty("wallTorchConfigured").boolValue = true;
                    so.FindProperty("wallTorchCell").vector3IntValue = wallCell;
                    so.FindProperty("torchDefinition").objectReferenceValue = torchDef;
                    so.FindProperty("wallTorchInteractable").objectReferenceValue = interactable;
                });

            MarkDirty(scene, lightingSystem, content);
            Debug.Log(
                $"[Lighting:QA] Wall torch at {wallCell} (nearest wall to {LightingQaPlacementResolver.TieflingAnchorName} "
                + $"at {anchor}). Bump from floor {floorCell}.");
        }

        static void CreateDarkQaPocketNearTieflingInternal()
        {
            if (!OpenSampleScene(out Scene scene))
                return;

            if (!TryResolvePlacement(out Vector3Int anchor, out Vector3Int torchWall, out _))
                return;

            MapManager map = Object.FindAnyObjectByType<MapManager>();
            Grid grid = Object.FindAnyObjectByType<Grid>();
            if (map == null || map.FloorMap == null || map.WallMap == null || grid == null)
            {
                Debug.LogError("[Lighting:QA] MapManager floor/wall maps or Grid missing.");
                return;
            }

            GameObject lightingSystem = FindOrCreateLightingSystem();
            Transform pocketRoot = EnsureChild(lightingSystem.transform, DarkPocketRootName);
            ClearChildren(pocketRoot);

            Vector3Int roomOrigin = new Vector3Int(anchor.x + 3, anchor.y - 2, 0);
            TileBase floorTile = SampleTileFromMap(map.FloorMap);
            TileBase wallTile = SampleTileFromMap(map.WallMap);
            if (floorTile == null)
            {
                Debug.LogError("[Lighting:QA] No floor tile found to paint dark pocket.");
                return;
            }

            Undo.RecordObject(map.FloorMap, "Paint dark QA floor");
            Vector3Int doorway = ResolveWestDoorway(roomOrigin, anchor, torchWall);

            if (wallTile != null)
            {
                Undo.RecordObject(map.WallMap, "Paint dark QA walls");
                PaintRoomWalls(map.WallMap, roomOrigin, RoomSize, wallTile, doorway);
            }

            ReplaceWallWithFloor(map, doorway);

            var floorCells = new List<Vector3Int>();
            for (int dx = 0; dx < RoomSize; dx++)
            {
                for (int dy = 0; dy < RoomSize; dy++)
                {
                    Vector3Int cell = new Vector3Int(roomOrigin.x + dx, roomOrigin.y + dy, 0);
                    map.FloorMap.SetTile(cell, floorTile);
                    floorCells.Add(cell);
                }
            }

            for (int i = 0; i < floorCells.Count; i++)
                EnsureReceiverMarker(pocketRoot, grid, floorCells[i], LightingQaPlacementResolver.QaAmbientRegionId);

            EnsureChild(lightingSystem.transform, Phase3RootName);
            pocketRoot.gameObject.SetActive(true);
            EnsureDarkPocketActive(lightingSystem.transform);

            MarkDirty(scene, lightingSystem, map.FloorMap, map.WallMap);
            Debug.Log(
                $"[Lighting:QA] Dark pocket {RoomSize}x{RoomSize} at origin {roomOrigin} "
                + $"({floorCells.Count} receivers, region {LightingQaPlacementResolver.QaAmbientRegionId}). "
                + $"Doorway at {doorway} → enter dark room at {doorway + Vector3Int.right}.");
        }

        /// <summary>West-wall gap; avoids the wall-torch cell when possible.</summary>
        static Vector3Int ResolveWestDoorway(
            Vector3Int roomOrigin,
            Vector3Int anchor,
            Vector3Int torchWallCell)
        {
            int westX = roomOrigin.x - 1;
            var candidates = new[]
            {
                new Vector3Int(westX, anchor.y + 1, 0),
                new Vector3Int(westX, anchor.y - 1, 0),
                new Vector3Int(westX, anchor.y + 2, 0),
                new Vector3Int(westX, roomOrigin.y + RoomSize / 2, 0)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != torchWallCell)
                    return candidates[i];
            }

            return new Vector3Int(westX, anchor.y + 1, 0);
        }

        static void ApplyDarkQaLightingProfileInternal(bool ensureHarness)
        {
            if (!OpenSampleScene(out Scene scene))
                return;

            if (ensureHarness)
                LightingScenarioSampleSceneBootstrap.EnsureLightingHarness(
                    applyScenarioIndex: Phase3ScenarioIndex,
                    selectLightingSystem: false);

            GameObject lightingSystem = FindOrCreateLightingSystem();
            ApplyServiceDefaults(lightingSystem);
            ApplyBootstrapDarkProfile(lightingSystem);
            EnsureDebugOverlay(lightingSystem);

            LightingScenarioController controller =
                lightingSystem.GetComponent<LightingScenarioController>();
            if (controller != null)
                controller.ApplyScenarioByIndex(Phase3ScenarioIndex);

            EnsureDarkPocketActive(lightingSystem.transform);
            MarkDirty(scene, lightingSystem);
            Debug.Log("[Lighting:QA] Dark pocket profile applied (global ambient 10, region 99 = 0, no day/night cycle).");
        }

        static void EnsureDarkPocketActive(Transform lightingSystem)
        {
            Transform pocket = lightingSystem.Find(DarkPocketRootName);
            if (pocket != null && !pocket.gameObject.activeSelf)
                pocket.gameObject.SetActive(true);
        }

        static bool OpenSampleScene(out Scene scene)
        {
            scene = EditorSceneManager.GetActiveScene();
            if (scene.path == ScenePath)
                return true;

            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError($"[Lighting:QA] Missing scene at {ScenePath}.");
                return false;
            }

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return true;
        }

        static bool TryResolvePlacement(
            out Vector3Int anchor,
            out Vector3Int wallCell,
            out Vector3Int floorCell)
        {
            anchor = default;
            wallCell = default;
            floorCell = default;

            GameObject tiefling = LightingQaPlacementResolver.FindAnchor();
            if (tiefling == null)
            {
                Debug.LogError(
                    $"[Lighting:QA] Could not find {LightingQaPlacementResolver.TieflingAnchorName} in scene.");
                return false;
            }

            anchor = LightingQaPlacementResolver.GridCellFromWorld(tiefling.transform.position);
            MapManager map = Object.FindAnyObjectByType<MapManager>();
            if (map == null)
            {
                Debug.LogError("[Lighting:QA] MapManager not found.");
                return false;
            }

            if (!LightingQaPlacementResolver.TryFindNearestWallCell(map, anchor, out wallCell))
            {
                Debug.LogError($"[Lighting:QA] No wall tile found near anchor {anchor}.");
                return false;
            }

            if (!LightingQaPlacementResolver.TryFindAdjacentFloorCell(map, wallCell, out floorCell))
                Debug.LogWarning($"[Lighting:QA] No adjacent walkable floor beside wall {wallCell}.");

            return true;
        }

        static void ApplyServiceDefaults(GameObject lightingSystem)
        {
            LightingService service = lightingSystem.GetComponent<LightingService>();
            if (service == null)
                service = Undo.AddComponent<LightingService>(lightingSystem);

            ApplySerialized(
                service,
                so =>
                {
                    so.FindProperty("defaultFloorAmbientRegionId").intValue = 0;
                    so.FindProperty("defaultFloorAmbientLight").intValue = LightLevel.FullDaylightAmbient;
                });
        }

        static void ApplyBootstrapDarkProfile(GameObject lightingSystem)
        {
            LightingBootstrap bootstrap = lightingSystem.GetComponent<LightingBootstrap>();
            if (bootstrap == null)
                bootstrap = Undo.AddComponent<LightingBootstrap>(lightingSystem);

            ApplySerialized(
                bootstrap,
                so =>
                {
                    SerializedProperty regions = so.FindProperty("ambientRegions");
                    regions.arraySize = 2;

                    WriteRegion(regions.GetArrayElementAtIndex(0), 0, LightLevel.FullDaylightAmbient);
                    WriteRegion(
                        regions.GetArrayElementAtIndex(1),
                        LightingQaPlacementResolver.QaAmbientRegionId,
                        LightLevel.PitchDark);
                });
        }

        static void WriteRegion(SerializedProperty element, int regionId, int ambient)
        {
            element.FindPropertyRelative("regionId").intValue = regionId;
            element.FindPropertyRelative("currentAmbientLight").intValue = ambient;
            element.FindPropertyRelative("cycleLengthTurns").intValue = 0;
            element.FindPropertyRelative("phases").arraySize = 0;
        }

        static void EnsureDebugOverlay(GameObject lightingSystem)
        {
            if (lightingSystem.GetComponent<LightingDebugOverlay>() != null)
                return;

            Undo.AddComponent<LightingDebugOverlay>(lightingSystem);
        }

        static void ApplySerialized(Object target, System.Action<SerializedObject> write)
        {
            if (target == null)
                return;

            Undo.RecordObject(target, "Lighting QA");
            var so = new SerializedObject(target);
            write(so);
            so.ApplyModifiedProperties();
        }

        static GameObject FindOrCreateLightingSystem()
        {
            LightingScenarioController existing = Object.FindAnyObjectByType<LightingScenarioController>();
            if (existing != null)
                return existing.gameObject;

            GameObject byName = GameObject.Find(LightingSystemName);
            if (byName != null)
                return byName;

            var created = new GameObject(LightingSystemName);
            Undo.RegisterCreatedObjectUndo(created, "Create LightingSystem");
            return created;
        }

        static Transform EnsureChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
                return child;

            var go = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(go, "Create " + childName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            return go.transform;
        }

        static LightingPhase3SampleContent EnsureWallTorchContent(Transform phase3)
        {
            Transform existing = phase3.Find(WallTorchObjectName);
            if (existing != null)
            {
                LightingPhase3SampleContent content = existing.GetComponent<LightingPhase3SampleContent>();
                if (content != null)
                    return content;

                return Undo.AddComponent<LightingPhase3SampleContent>(existing.gameObject);
            }

            var go = new GameObject(WallTorchObjectName);
            Undo.RegisterCreatedObjectUndo(go, "Create WallTorch_Test");
            go.transform.SetParent(phase3, false);
            return Undo.AddComponent<LightingPhase3SampleContent>(go);
        }

        static void EnsureReceiverMarker(Transform parent, Grid grid, Vector3Int cell, int regionId)
        {
            string markerName = $"LightingMarker_R{regionId}_{cell.x}_{cell.y}";
            Transform existing = parent.Find(markerName);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(markerName);
                Undo.RegisterCreatedObjectUndo(go, "Create receiver marker");
                go.transform.SetParent(parent, false);
            }

            go.transform.position = grid.GetCellCenterWorld(cell);
            LightingCellMarker marker = go.GetComponent<LightingCellMarker>();
            if (marker == null)
                marker = Undo.AddComponent<LightingCellMarker>(go);

            ApplySerialized(
                marker,
                so =>
                {
                    so.FindProperty("isEmitter").boolValue = false;
                    so.FindProperty("isReceiver").boolValue = true;
                    so.FindProperty("ambientRegionId").intValue = regionId;
                });
        }

        static void PaintRoomWalls(
            Tilemap wallMap,
            Vector3Int origin,
            int size,
            TileBase wallTile,
            Vector3Int westDoorway)
        {
            int minX = origin.x - 1;
            int maxX = origin.x + size;
            int minY = origin.y - 1;
            int maxY = origin.y + size;

            for (int x = minX; x <= maxX; x++)
            {
                SetWallUnlessDoor(wallMap, new Vector3Int(x, minY, 0), wallTile, westDoorway);
                SetWallUnlessDoor(wallMap, new Vector3Int(x, maxY, 0), wallTile, westDoorway);
            }

            for (int y = minY; y <= maxY; y++)
            {
                SetWallUnlessDoor(wallMap, new Vector3Int(minX, y, 0), wallTile, westDoorway);
                SetWallUnlessDoor(wallMap, new Vector3Int(maxX, y, 0), wallTile, westDoorway);
            }
        }

        static void SetWallUnlessDoor(
            Tilemap wallMap,
            Vector3Int cell,
            TileBase wallTile,
            Vector3Int doorway)
        {
            if (cell == doorway)
            {
                wallMap.SetTile(cell, null);
                return;
            }

            wallMap.SetTile(cell, wallTile);
        }

        /// <summary>Walkable doorway: clear wall and paint floor on the same cell.</summary>
        static void ReplaceWallWithFloor(MapManager map, Vector3Int cell)
        {
            TileBase floorTile = SampleTileFromMap(map.FloorMap);
            if (floorTile == null)
            {
                Debug.LogError("[Lighting:QA] No floor tile to paint doorway.");
                return;
            }

            Undo.RecordObject(map.WallMap, "Doorway clear wall");
            Undo.RecordObject(map.FloorMap, "Doorway paint floor");
            map.WallMap.SetTile(cell, null);
            map.FloorMap.SetTile(cell, floorTile);
        }

        static bool TryResolveDarkPocketDoorway(
            Vector3Int anchor,
            Vector3Int torchWallCell,
            out Vector3Int doorway)
        {
            doorway = default;
            GameObject pocketRoot = GameObject.Find(DarkPocketRootName);
            if (pocketRoot == null)
                return false;

            int minInteriorX = int.MaxValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (Transform child in pocketRoot.transform)
            {
                if (!child.name.StartsWith("LightingMarker_R"))
                    continue;

                LightingCellMarker marker = child.GetComponent<LightingCellMarker>();
                if (marker == null)
                    continue;

                Vector3Int cell = LightingQaPlacementResolver.GridCellFromWorld(child.position);
                if (cell.x < minInteriorX)
                    minInteriorX = cell.x;

                if (cell.y < minY)
                    minY = cell.y;
                if (cell.y > maxY)
                    maxY = cell.y;
            }

            if (minInteriorX == int.MaxValue)
                return false;

            int westWallX = minInteriorX - 1;
            int doorY = Mathf.Clamp(anchor.y + 1, minY, maxY);
            doorway = new Vector3Int(westWallX, doorY, 0);

            if (doorway == torchWallCell && doorY < maxY)
                doorway = new Vector3Int(westWallX, doorY + 1, 0);
            else if (doorway == torchWallCell && doorY > minY)
                doorway = new Vector3Int(westWallX, doorY - 1, 0);

            return true;
        }

        static TileBase SampleTileFromMap(Tilemap map)
        {
            if (map == null)
                return null;

            foreach (Vector3Int pos in map.cellBounds.allPositionsWithin)
            {
                if (!map.HasTile(pos))
                    continue;

                return map.GetTile(pos);
            }

            return null;
        }

        static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if (Selection.activeGameObject == child)
                    Selection.activeGameObject = parent.gameObject;

                Undo.DestroyObjectImmediate(child);
            }
        }

        static void MarkDirty(Scene scene, params Object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                    EditorUtility.SetDirty(objects[i]);
            }

            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
