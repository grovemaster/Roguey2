#if UNITY_EDITOR
using System.IO;
using JRogue.Interactables;
using JRogue.Racial;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.Interactables
{
    public static class MeditationShrineAssetPackCreator
    {
        const string ResourcesRoot = "Assets/Resources/Interactables";
        const string RacialResourcesRoot = "Assets/Resources/Racial/Elf";
        const string EffectsPath = "Assets/Data/Interactables/Effects";
        const string SpritesOffPath = "Assets/Art/Interactables/Sprites/LeverSwitch_Off.png";

        const string CreateMenuPath = "Assets/Create/JRogue/Interactables/Create Meditation Shrine Assets";
        const string TopMenuPath = "JRogue/Interactables/Create Meditation Shrine Assets";

        [MenuItem(CreateMenuPath, false, 2)]
        [MenuItem(TopMenuPath, false, 2)]
        public static void CreateMeditationShrineAssets()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources/Interactables"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources/Racial/Elf"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Interactables/Effects"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Racial/Elf"));

            AlwaysTruePrecondition alwaysTrue = AssetDatabase.LoadAssetAtPath<AlwaysTruePrecondition>(
                "Assets/Data/Interactables/Preconditions/AlwaysTrue.asset");
            if (alwaysTrue == null)
            {
                alwaysTrue = ScriptableObject.CreateInstance<AlwaysTruePrecondition>();
                AssetDatabase.CreateAsset(alwaysTrue, "Assets/Data/Interactables/Preconditions/AlwaysTrue.asset");
            }

            ElementalSpiritLevelCurve defaultCurve = GetOrCreate<ElementalSpiritLevelCurve>(
                "Assets/Data/Racial/Elf/ElementalSpiritDefaultLevelCurve.asset");
            defaultCurve.xpToReachNextLevel = new System.Collections.Generic.List<int> { 10, 20, 30 };
            EditorUtility.SetDirty(defaultCurve);

            ElementalSpiritProgressionConfig progression = GetOrCreate<ElementalSpiritProgressionConfig>(
                $"{RacialResourcesRoot}/ElementalSpiritProgressionConfig.asset");
            progression.defaultLevelCurve = defaultCurve;
            EditorUtility.SetDirty(progression);

            ElementalSpiritMeditationGateDefinition gate = GetOrCreate<ElementalSpiritMeditationGateDefinition>(
                "Assets/Data/Racial/Elf/MeditationShrineGate_Town.asset");
            gate.gateId = "meditation_shrine";
            gate.displayName = "Meditation Shrine";
            gate.spiritXpAward = 10;
            gate.cost = default;
            EditorUtility.SetDirty(gate);

            ElementalSpiritMeditationEffect meditationEffect = GetOrCreate<ElementalSpiritMeditationEffect>(
                $"{EffectsPath}/ElementalSpiritMeditation_Town.asset");
            meditationEffect.gate = gate;
            EditorUtility.SetDirty(meditationEffect);

            Sprite spriteOff = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesOffPath);

            InteractableTileDefinition shrine = GetOrCreate<InteractableTileDefinition>(
                $"{ResourcesRoot}/MeditationShrine_Town.asset");
            shrine.interactableId = InteractableTileId.MeditationShrine;
            shrine.displayName = "Meditation Shrine";
            shrine.kind = InteractableTileKind.Shrine;
            shrine.blocksOccupancy = true;
            shrine.bumpEnabled = true;
            shrine.allowRepeatActivation = true;
            shrine.preconditions = new InteractablePrecondition[] { alwaysTrue };
            shrine.onActivateEffects = new InteractableEffect[] { meditationEffect };
            shrine.spriteOff = spriteOff;
            shrine.spriteOn = spriteOff;
            EditorUtility.SetDirty(shrine);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SpiritMeditation] Created meditation shrine assets.");
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
