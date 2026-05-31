#if UNITY_EDITOR
using JRogue.Spawn;
using JRogue.World.Altar;
using JRogue.World.MapInteract;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.Altar
{
    public static class AltarAssetPackCreator
    {
        const string DataRoot = "Assets/Data/Altar";
        const string FiltersPath = "Assets/Data/Altar/Filters";
        const string EffectsPath = "Assets/Data/Altar/Effects";
        const string PlacementSetsPath = "Assets/Data/Altar/PlacementSets";
        const string SpawnPath = "Assets/Data/Spawn/Spawn_Skeleton_NorthOfLever.asset";
        const string AltarSpritePath = "Assets/Art/Altars/Sprites/Altar_StoneShrine.png";

        const string MenuPath = "JRogue/World/Create Mana Stone Altar v0 Assets";

        [MenuItem(MenuPath, false, 40)]
        public static void CreateManaStoneAltarAssets()
        {
            EnsureFolder(DataRoot);
            EnsureFolder(FiltersPath);
            EnsureFolder(EffectsPath);
            EnsureFolder(PlacementSetsPath);

            var tier9Filter = LoadOrCreate<ManaStoneTierAcceptFilter>($"{FiltersPath}/ManaStoneTier9.asset");
            tier9Filter.tier = 9;
            EditorUtility.SetDirty(tier9Filter);

            var tier8Filter = LoadOrCreate<ManaStoneTierAcceptFilter>($"{FiltersPath}/ManaStoneTier8.asset");
            tier8Filter.tier = 8;
            EditorUtility.SetDirty(tier8Filter);

            var spawnDef = AssetDatabase.LoadAssetAtPath<EnemySpawnDefinition>(SpawnPath);
            var spawnEffect = LoadOrCreate<SpawnEnemyAltarCompletionEffect>(
                $"{EffectsPath}/SpawnSkeletonOnAltarComplete.asset");
            spawnEffect.spawnDefinition = spawnDef;
            EditorUtility.SetDirty(spawnEffect);

            var altarDef = LoadOrCreate<AltarDefinition>($"{DataRoot}/Altar_ManaStonePairV0.asset");
            altarDef.altarId = "altar_mana_stone_pair_v0";
            altarDef.displayName = "Stone altar";
            altarDef.descriptionTemplate =
                "This altar has places for a tier 9 mana stone and a tier 8 mana stone.";
            altarDef.usedDescriptionTemplate =
                "This altar has been used. Its power is spent.";
            altarDef.overlaySprite = AssetDatabase.LoadAssetAtPath<Sprite>(AltarSpritePath);
            altarDef.blocksOccupancy = true;
            altarDef.pickerSortOrder = 0;
            altarDef.slots = new[]
            {
                new AltarSlotDefinition
                {
                    slotId = "tier9",
                    label = "Tier 9 mana stone",
                    acceptFilter = tier9Filter,
                    maxCount = 1,
                },
                new AltarSlotDefinition
                {
                    slotId = "tier8",
                    label = "Tier 8 mana stone",
                    acceptFilter = tier8Filter,
                    maxCount = 1,
                },
            };
            altarDef.completionRules = new[]
            {
                new AltarCompletionRule
                {
                    ruleId = "skeleton_spawn",
                    requiredSlotIds = System.Array.Empty<string>(),
                    effects = new AltarCompletionEffect[] { spawnEffect },
                },
            };
            EditorUtility.SetDirty(altarDef);

            var placementSet = LoadOrCreate<AltarPlacementSet>(
                $"{PlacementSetsPath}/SampleScene_ManaStoneAltar.asset");
            placementSet.placements = new[]
            {
                new AltarPlacement
                {
                    cell = new Vector3Int(6, -2, 0),
                    definition = altarDef,
                },
            };
            EditorUtility.SetDirty(placementSet);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Altar] Created/refreshed mana stone altar assets. "
                + "Run JRogue/World/Wire SampleScene Mana Stone Altar to add scene objects.");
        }

        [MenuItem("JRogue/World/Wire SampleScene Mana Stone Altar", false, 41)]
        public static void WireSampleSceneAltar()
        {
            CreateManaStoneAltarAssets();

            var placementSet = AssetDatabase.LoadAssetAtPath<AltarPlacementSet>(
                $"{PlacementSetsPath}/SampleScene_ManaStoneAltar.asset");

            GameObject existing = GameObject.Find("MapInteract_AltarBootstrap");
            if (existing == null)
            {
                existing = new GameObject("MapInteract_AltarBootstrap");
                Undo.RegisterCreatedObjectUndo(existing, "Create altar bootstrap");
            }

            if (existing.GetComponent<AdjacentMapInteractableService>() == null)
            {
                var service = existing.AddComponent<AdjacentMapInteractableService>();
                Tilemap overlay = FindOrCreateAltarOverlay();
                SerializedObject so = new SerializedObject(service);
                so.FindProperty("altarOverlayMap").objectReferenceValue = overlay;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AltarBootstrap bootstrap = existing.GetComponent<AltarBootstrap>();
            if (bootstrap == null)
                bootstrap = Undo.AddComponent<AltarBootstrap>(existing);

            SerializedObject bootstrapSo = new SerializedObject(bootstrap);
            bootstrapSo.FindProperty("placementSet").objectReferenceValue = placementSet;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = existing;
            Debug.Log("[Altar] Wired SampleScene altar at (6,-2). Stand south at (6,-3) and press E.");
        }

        [MenuItem("JRogue/World/Grant Test Mana Stones (Party)", false, 42)]
        public static void GrantTestManaStones()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Altar] Enter Play Mode to grant test mana stones.");
                return;
            }

            var ledger = JRogue.Manager.Party.PartyManaStoneLedger.Instance;
            if (ledger == null)
            {
                Debug.LogWarning("[Altar] No PartyManaStoneLedger in scene.");
                return;
            }

            ledger.Add(9, "skeleton", 2);
            ledger.Add(9, "giant_skeleton", 1);
            ledger.Add(8, "skeleton", 3);
            ledger.Add(8, "orc", 1);
            Debug.Log("[Altar] Granted test tier 8/9 mana stones to party ledger.");
        }

        static Tilemap FindOrCreateAltarOverlay()
        {
            const string overlayName = "Altar_Overlay";
            GameObject gridGo = GameObject.Find("Grid");
            Transform parent = gridGo != null ? gridGo.transform : null;

            Transform existing = parent != null
                ? parent.Find(overlayName)
                : GameObject.Find(overlayName)?.transform;

            if (existing != null)
                return existing.GetComponent<Tilemap>();

            var overlayGo = new GameObject(overlayName, typeof(Tilemap), typeof(TilemapRenderer));
            if (parent != null)
                overlayGo.transform.SetParent(parent, false);

            Tilemap map = overlayGo.GetComponent<Tilemap>();
            TilemapRenderer renderer = overlayGo.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = 3;
            return map;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
