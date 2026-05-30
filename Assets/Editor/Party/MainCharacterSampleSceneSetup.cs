#if UNITY_EDITOR
using JRogue.Manager.Party;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JRogue.Editor.Party
{
    /// <summary>SampleScene helpers for immutable main-character designation.</summary>
    public static class MainCharacterSampleSceneSetup
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string BarbarianObjectName = "Party_Barbarian_Warrior";

        [MenuItem("JRogue/Party/Seed Main Character on Party_Barbarian_Warrior")]
        public static void SeedMainCharacterOnBarbarian()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject barbarian = FindSceneObjectByName(scene, BarbarianObjectName);
            if (barbarian == null)
            {
                Debug.LogError($"[GameOver] Could not find {BarbarianObjectName} in {ScenePath}.");
                return;
            }

            PartyMainCharacterMarker existing = barbarian.GetComponent<PartyMainCharacterMarker>();
            if (existing == null)
                Undo.AddComponent<PartyMainCharacterMarker>(barbarian);

            int markerCount = CountMainCharacterMarkers(scene);
            if (markerCount != 1)
                Debug.LogWarning($"[GameOver] Scene has {markerCount} main-character markers (expected 1).");

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = barbarian;
            Debug.Log($"[GameOver] Added {nameof(PartyMainCharacterMarker)} to {BarbarianObjectName}.");
        }

        [MenuItem("JRogue/Party/Validate Main Character Markers in SampleScene")]
        public static void ValidateMainCharacterMarkersInSampleScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            int count = CountMainCharacterMarkers(scene);
            if (count == 1)
                Debug.Log($"[GameOver] SampleScene OK — exactly one {nameof(PartyMainCharacterMarker)}.");
            else
                Debug.LogError($"[GameOver] SampleScene has {count} main-character markers (expected exactly 1).");
        }

        static int CountMainCharacterMarkers(Scene scene)
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                PartyMainCharacterMarker[] markers =
                    roots[r].GetComponentsInChildren<PartyMainCharacterMarker>(true);
                count += markers.Length;
            }

            return count;
        }

        static GameObject FindSceneObjectByName(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < all.Length; t++)
                {
                    if (all[t].name == objectName)
                        return all[t].gameObject;
                }
            }

            return null;
        }
    }
}
#endif
