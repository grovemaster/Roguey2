#if UNITY_EDITOR
using System.IO;
using JRogue.Interactables;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.Interactables
{
    public static class TownTimeLeverAssetPackCreator
    {
        const string ResourcesRoot = "Assets/Resources/Interactables";
        const string EffectsPath = "Assets/Data/Interactables/Effects";
        const string SpritesOffPath = "Assets/Art/Interactables/Sprites/LeverSwitch_Off.png";
        const string SpritesOnPath = "Assets/Art/Interactables/Sprites/LeverSwitch_On.png";

        const string CreateMenuPath = "Assets/Create/JRogue/Interactables/Create Town Time Lever Assets";
        const string TopMenuPath = "JRogue/Interactables/Create Town Time Lever Assets";

        [MenuItem(CreateMenuPath, false, 1)]
        [MenuItem(TopMenuPath, false, 1)]
        public static void CreateTownTimeLeverAssets()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources/Interactables"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Interactables/Effects"));

            Sprite spriteOff = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesOffPath);
            Sprite spriteOn = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesOnPath);
            AlwaysTruePrecondition alwaysTrue = AssetDatabase.LoadAssetAtPath<AlwaysTruePrecondition>(
                "Assets/Data/Interactables/Preconditions/AlwaysTrue.asset");

            if (alwaysTrue == null)
            {
                alwaysTrue = ScriptableObject.CreateInstance<AlwaysTruePrecondition>();
                AssetDatabase.CreateAsset(alwaysTrue, "Assets/Data/Interactables/Preconditions/AlwaysTrue.asset");
            }

            TownTimeLeverEffect advancePhase = GetOrCreate<TownTimeLeverEffect>(
                EffectsPath + "/TownTimeAdvancePhase.asset");

            CreateLever(
                ResourcesRoot + "/LeverSwitch_TownTime_A.asset",
                InteractableTileId.TownTimeLeverA,
                "Time lever A",
                spriteOff,
                spriteOn,
                alwaysTrue,
                advancePhase);

            CreateLever(
                ResourcesRoot + "/LeverSwitch_TownTime_B.asset",
                InteractableTileId.TownTimeLeverB,
                "Time lever B",
                spriteOff,
                spriteOn,
                alwaysTrue,
                advancePhase);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TownTime] Created town time lever assets under Assets/Resources/Interactables/.");
        }

        static void CreateLever(
            string assetPath,
            InteractableTileId id,
            string displayName,
            Sprite spriteOff,
            Sprite spriteOn,
            AlwaysTruePrecondition alwaysTrue,
            TownTimeLeverEffect advancePhase)
        {
            InteractableTileDefinition def = GetOrCreate<InteractableTileDefinition>(assetPath);
            def.interactableId = id;
            def.displayName = displayName;
            def.kind = InteractableTileKind.Lever;
            def.blocksOccupancy = true;
            def.bumpEnabled = true;
            def.preconditions = new InteractablePrecondition[] { alwaysTrue };
            def.onActivateEffects = new InteractableEffect[] { advancePhase };
            def.spriteOff = spriteOff;
            def.spriteOn = spriteOn;
            EditorUtility.SetDirty(def);
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
    }
}
#endif
