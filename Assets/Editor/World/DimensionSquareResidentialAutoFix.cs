#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    /// <summary>
    /// One-shot: after scripts reload, rebuild DimensionSquareTest with residential + inn if a marker file is present.
    /// </summary>
    [InitializeOnLoad]
    static class DimensionSquareResidentialAutoFix
    {
        public const string MarkerPath = "Temp/JRoguePendingDimensionSquareResidentialFix";

        static DimensionSquareResidentialAutoFix()
        {
            EditorApplication.delayCall += TryRun;
        }

        static void TryRun()
        {
            if (!File.Exists(MarkerPath))
                return;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                Debug.Log(
                    "[DimensionSquare] Residential auto-fix waiting for Play Mode to end, then will rebuild " +
                    "DimensionSquareTest with town_residential + inn.");
                return;
            }

            RunFix();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.delayCall += TryRun;
        }

        static void RunFix()
        {
            try
            {
                if (File.Exists(MarkerPath))
                    File.Delete(MarkerPath);
            }
            catch
            {
                // Best-effort; Fix still proceeds.
            }

            Debug.Log("[DimensionSquare] Auto-applying residential district + inn into DimensionSquareTest…");
            DimensionSquareSceneCreator.FixDimensionSquareTestScene();
        }
    }
}
#endif
