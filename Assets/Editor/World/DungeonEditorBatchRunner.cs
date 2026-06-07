#if UNITY_EDITOR
using UnityEditor;

namespace JRogue.Editor.World
{
    public static class DungeonEditorBatchRunner
    {
        public static void CreateTilePalettesAndZonePacks()
        {
            DungeonTilePalettePackCreator.CreateTilePalettes();
            DungeonZonePackCreator.CreateFloor1ZonePack();
            DungeonZonePackCreator.CreateFloor3ZonePack();
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }
    }
}
#endif
