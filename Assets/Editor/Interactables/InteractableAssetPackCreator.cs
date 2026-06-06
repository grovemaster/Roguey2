#if UNITY_EDITOR
using System.IO;
using JRogue.Interactables;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.Interactables
{
    /// <summary>
    /// Creates the §9 QA lever definitions, preconditions, effects, and a SampleScene placement set.
    /// </summary>
    public static class InteractableAssetPackCreator
    {
        const string Root = "Assets/Data/Interactables";
        const string PreconditionsPath = Root + "/Preconditions";
        const string EffectsPath = Root + "/Effects";
        const string DefinitionsPath = Root;
        const string PlacementSetsPath = Root + "/PlacementSets";
        const string SpritesPath = "Assets/Art/Interactables/Sprites";

        const string CreateMenuPath = "Assets/Create/JRogue/Interactables/Create QA Lever Asset Pack";
        const string TopMenuPath = "JRogue/Interactables/Create QA Lever Asset Pack";

        [MenuItem(CreateMenuPath, false, 0)]
        [MenuItem(TopMenuPath, false, 0)]
        public static void CreateQaLeverAssetPack()
        {
            EnsureFolders();

            Sprite spriteOff = LoadOrCreateSprite(
                SpritesPath + "/LeverSwitch_Off.png",
                new Color(0.75f, 0.45f, 0.2f, 1f));
            Sprite spriteOn = LoadOrCreateSprite(
                SpritesPath + "/LeverSwitch_On.png",
                new Color(0.2f, 0.55f, 0.75f, 1f));
            // LoadOrCreateSprite keeps existing PNGs (CC0 art in Sprites/); placeholders only if missing.

            AlwaysTruePrecondition alwaysTrue = GetOrCreate<AlwaysTruePrecondition>(
                PreconditionsPath + "/AlwaysTrue.asset");

            OtherInteractableOnPrecondition requiresFirst = GetOrCreate<OtherInteractableOnPrecondition>(
                PreconditionsPath + "/RequiresLeverFirstOn.asset");
            requiresFirst.requiredInteractableId = InteractableTileId.LeverSwitchFirst;
            EditorUtility.SetDirty(requiresFirst);

            OtherInteractableOnPrecondition requiresThird = GetOrCreate<OtherInteractableOnPrecondition>(
                PreconditionsPath + "/RequiresLeverThirdOn.asset");
            requiresThird.requiredInteractableId = InteractableTileId.LeverSwitchThird;
            EditorUtility.SetDirty(requiresThird);

            ScriptOnlyPrecondition scriptOnly = GetOrCreate<ScriptOnlyPrecondition>(
                PreconditionsPath + "/ScriptOnly.asset");

            ActivateInteractableEffect activateThird = GetOrCreate<ActivateInteractableEffect>(
                EffectsPath + "/ActivateLeverThird.asset");
            activateThird.targetInteractableId = InteractableTileId.LeverSwitchThird;
            EditorUtility.SetDirty(activateThird);

            GrantPartyExperienceEffect grantXp = GetOrCreate<GrantPartyExperienceEffect>(
                EffectsPath + "/GrantPartyXp25.asset");
            grantXp.experienceAmount = 25;
            EditorUtility.SetDirty(grantXp);

            InteractableTileDefinition lever1 = CreateLeverDefinition(
                DefinitionsPath + "/LeverSwitch_First.asset",
                InteractableTileId.LeverSwitchFirst,
                "Lever 1",
                bumpEnabled: true,
                spriteOff,
                spriteOn,
                new InteractablePrecondition[] { alwaysTrue },
                System.Array.Empty<InteractableEffect>());

            InteractableTileDefinition lever2 = CreateLeverDefinition(
                DefinitionsPath + "/LeverSwitch_Second.asset",
                InteractableTileId.LeverSwitchSecond,
                "Lever 2",
                bumpEnabled: true,
                spriteOff,
                spriteOn,
                new InteractablePrecondition[] { requiresFirst },
                new InteractableEffect[] { activateThird });

            InteractableTileDefinition lever3 = CreateLeverDefinition(
                DefinitionsPath + "/LeverSwitch_Third.asset",
                InteractableTileId.LeverSwitchThird,
                "Lever 3",
                bumpEnabled: false,
                spriteOff,
                spriteOn,
                new InteractablePrecondition[] { scriptOnly },
                System.Array.Empty<InteractableEffect>());

            InteractableTileDefinition lever4 = CreateLeverDefinition(
                DefinitionsPath + "/LeverSwitch_Fourth.asset",
                InteractableTileId.LeverSwitchFourth,
                "Lever 4",
                bumpEnabled: true,
                spriteOff,
                spriteOn,
                new InteractablePrecondition[] { requiresThird },
                new InteractableEffect[] { grantXp });

            InteractablePlacementSet placementSet = GetOrCreate<InteractablePlacementSet>(
                PlacementSetsPath + "/SampleScene_Levers.asset");
            placementSet.placements = new[]
            {
                new InteractablePlacement
                {
                    cell = new Vector3Int(4, -6, 0),
                    definition = lever1,
                },
                new InteractablePlacement
                {
                    cell = new Vector3Int(5, -6, 0),
                    definition = lever2,
                },
                new InteractablePlacement
                {
                    cell = new Vector3Int(6, -6, 0),
                    definition = lever3,
                },
                new InteractablePlacement
                {
                    cell = new Vector3Int(7, -6, 0),
                    definition = lever4,
                },
            };
            EditorUtility.SetDirty(placementSet);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = placementSet;
            Debug.Log(
                $"[Interactable] QA lever pack created under {Root}. " +
                $"Assign '{placementSet.name}' on InteractableTileBootstrap in your scene.");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Interactables/Preconditions"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Interactables/Effects"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Interactables/PlacementSets"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/Interactables/Sprites"));
            AssetDatabase.Refresh();
        }

        static T GetOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
                return existing;

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }

        static InteractableTileDefinition CreateLeverDefinition(
            string assetPath,
            InteractableTileId id,
            string displayName,
            bool bumpEnabled,
            Sprite spriteOff,
            Sprite spriteOn,
            InteractablePrecondition[] preconditions,
            InteractableEffect[] effects)
        {
            InteractableTileDefinition def = GetOrCreate<InteractableTileDefinition>(assetPath);
            def.interactableId = id;
            def.displayName = displayName;
            def.kind = InteractableTileKind.Lever;
            def.blocksOccupancy = true;
            def.bumpEnabled = bumpEnabled;
            def.spriteOff = spriteOff;
            def.spriteOn = spriteOn;
            def.preconditions = preconditions;
            def.onActivateEffects = effects;
            EditorUtility.SetDirty(def);
            return def;
        }

        static Sprite LoadOrCreateSprite(string assetPath, Color color)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null)
                return existing;

            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(assetPath, png);
            AssetDatabase.ImportAsset(assetPath);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }
}
#endif
