#if UNITY_EDITOR
using JRogue.World.Generation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JRogue.Editor.Party
{
    public static class PartyCompositionMenu
    {
        [MenuItem("JRogue/Party/Use Roster/Barbarian, Human, Elf, Undead")]
        public static void UseClassicRoster()
        {
            ApplyPreset(PartyCompositionPreset.ClassicBarbarianHumanElfUndead);
        }

        [MenuItem("JRogue/Party/Use Roster/Tiefling, Beastman, Dragonian, Dwarf")]
        public static void UseRacialMixRoster()
        {
            ApplyPreset(PartyCompositionPreset.TieflingBeastmanDragonianDwarf);
        }

        static void ApplyPreset(PartyCompositionPreset preset)
        {
            if (PartyCompositionSwapService.TryApplyPreset(preset, out string reason))
            {
                if (!Application.isPlaying)
                    MarkLoadedScenesDirty();
            }
            else
            {
                Debug.LogWarning($"[PartyComposition] {reason}");
            }
        }

        static void MarkLoadedScenesDirty()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
#endif
