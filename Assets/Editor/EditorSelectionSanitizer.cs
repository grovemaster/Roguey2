#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JRogue.Editor
{
    /// <summary>
    /// Clears stale Inspector selection and tracker state that survives domain reload.
    /// Prevents SerializedObjectNotCreatableException (GameObject / RectTransform / CanvasScaler inspectors).
    /// </summary>
    [InitializeOnLoad]
    static class EditorSelectionSanitizer
    {
        static readonly Type InspectorWindowType =
            typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");

        static readonly MethodInfo RepaintAllInspectorsMethod = InspectorWindowType?.GetMethod(
            "RepaintAllInspectors",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        static EditorSelectionSanitizer()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneOpening += OnSceneOpening;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += () => ResetInspectorState();
        }

        static void OnBeforeAssemblyReload() => ResetInspectorState();

        static void OnAfterAssemblyReload() =>
            EditorApplication.delayCall += () => ResetInspectorState();

        static void OnSceneOpening(string path, OpenSceneMode mode) => ResetInspectorState();

        static void OnSceneOpened(Scene scene, OpenSceneMode mode) =>
            EditorApplication.delayCall += () => ResetInspectorState();

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state is PlayModeStateChange.ExitingEditMode or PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += () => ResetInspectorState();
        }

        [MenuItem("JRogue/Editor/Clear Broken Inspector Selection")]
        public static void ClearBrokenSelectionMenu()
        {
            ResetInspectorState(log: true);
        }

        [MenuItem("JRogue/Editor/Reset Inspector Windows")]
        public static void ResetInspectorWindowsMenu()
        {
            ResetInspectorState(log: true, closeDuplicateInspectors: true);
        }

        [MenuItem("JRogue/Editor/Diagnose Inspector State")]
        public static void DiagnoseInspectorStateMenu()
        {
            UnityEngine.Object active = Selection.activeObject;
            string activeName = active != null ? active.name : "null";
            Debug.Log($"[EditorSelectionSanitizer] Selection.activeObject={activeName}, count={Selection.count}");

            ActiveEditorTracker shared = ActiveEditorTracker.sharedTracker;
            Debug.Log($"[EditorSelectionSanitizer] sharedTracker locked={shared.isLocked}, activeEditors={shared.activeEditors?.Length ?? 0}");

            UnityEditor.Editor[] editors = shared.activeEditors;
            if (editors != null)
            {
                for (int i = 0; i < editors.Length; i++)
                {
                    UnityEditor.Editor editor = editors[i];
                    string editorName = editor != null ? editor.GetType().Name : "null";
                    string targetName = editor != null && editor.target != null ? editor.target.name : "NULL";
                    Debug.Log($"[EditorSelectionSanitizer]   editor[{i}]={editorName}, target={targetName}");
                }
            }

            if (InspectorWindowType != null)
            {
                UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(InspectorWindowType);
                Debug.Log($"[EditorSelectionSanitizer] InspectorWindow count={windows.Length}");
            }
        }

        public static void ClearSelectionPublic() => ResetInspectorState();

        static void ResetInspectorState(bool log = false, bool closeDuplicateInspectors = false)
        {
            ClearSelection();

            if (closeDuplicateInspectors)
                CloseDuplicateInspectorWindows();

            RebuildAllInspectorTrackers();
            RepaintAllInspectors();

            if (log)
                Debug.Log("[EditorSelectionSanitizer] Reset Inspector selection and trackers.");
        }

        static void ClearSelection()
        {
#pragma warning disable CS0618
            Selection.activeInstanceID = 0;
#pragma warning restore CS0618
            Selection.activeObject = null;
            Selection.objects = Array.Empty<UnityEngine.Object>();
        }

        static void RebuildAllInspectorTrackers()
        {
            try
            {
                ActiveEditorTracker shared = ActiveEditorTracker.sharedTracker;
                shared.isLocked = false;
                shared.ForceRebuild();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EditorSelectionSanitizer] Failed to rebuild shared tracker: {ex.Message}");
            }

            if (InspectorWindowType == null)
                return;

            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(InspectorWindowType);
            FieldInfo trackerField = InspectorWindowType.GetField(
                "m_Tracker",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (trackerField == null)
                return;

            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] is not EditorWindow)
                    continue;

                if (trackerField.GetValue(windows[i]) is not ActiveEditorTracker tracker)
                    continue;

                tracker.isLocked = false;
                try
                {
                    tracker.ForceRebuild();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[EditorSelectionSanitizer] Failed to rebuild inspector tracker: {ex.Message}");
                }
            }
        }

        static void CloseDuplicateInspectorWindows()
        {
            if (InspectorWindowType == null)
                return;

            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(InspectorWindowType);
            if (windows.Length <= 1)
                return;

            for (int i = 1; i < windows.Length; i++)
            {
                if (windows[i] is EditorWindow extra)
                    extra.Close();
            }

            EditorApplication.delayCall += () => EditorWindow.GetWindow(InspectorWindowType);
        }

        static void RepaintAllInspectors()
        {
            if (RepaintAllInspectorsMethod == null)
                return;

            try
            {
                RepaintAllInspectorsMethod.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EditorSelectionSanitizer] RepaintAllInspectors failed: {ex.Message}");
            }
        }
    }
}
#endif
