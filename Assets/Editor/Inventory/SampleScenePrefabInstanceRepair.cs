#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JRogue.Editor.Inventory
{
    /// <summary>
    /// Removes orphan / broken prefab instance roots that cause SerializedObjectNotCreatableException in the Inspector.
    /// </summary>
    public static class SampleScenePrefabInstanceRepair
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("JRogue/Inventory/Remove Broken Scroll Instances From SampleScene")]
        public static void RemoveBrokenScrollInstances()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int removed = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null)
                    continue;

                if (root.name.Contains("WorldItem_Scroll_Fireball")
                    && PrefabUtility.GetPrefabInstanceStatus(root) == PrefabInstanceStatus.MissingAsset)
                {
                    Object.DestroyImmediate(root);
                    removed++;
                }
            }

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[Scroll:Fireball] Removed {removed} broken scroll instance(s). Use Place Fireball Scroll to re-add.");
            }
            else
            {
                Debug.Log("[Scroll:Fireball] No broken scroll instances found.");
            }
        }
    }
}
#endif
