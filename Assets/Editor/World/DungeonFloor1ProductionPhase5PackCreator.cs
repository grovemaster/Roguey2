#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    /// <summary>
    /// Phase 5: enemies, traps, essences, spawn schedule wiring on production floor assets.
    /// </summary>
    public static class DungeonFloor1ProductionPhase5PackCreator
    {
        const string MenuPath = "JRogue/Dungeon/Phase 5 — Setup Production Content";

        [MenuItem(MenuPath, false, 55)]
        public static void SetupProductionContentPhase5()
        {
            DungeonFloor1ProductionContentPackCreator.CreateFloor1ProductionContentPack();
            DungeonFloor1ProductionPhase2PackCreator.RefreshProductionContentWiring();
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[Floor1Production] Phase 5 complete: enemies, essences, schedule, trap profiles wired. " +
                "Run from DimensionSquareTest → dungeon portal to playtest Floor 1 production content.");
        }
    }
}
#endif
