using JRogue.Interactables;
using JRogue.Manager.Map;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JRogue.World.Generation.Phases
{
    /// <summary>Registers mutual-exclusive town time levers on town_main.</summary>
    public sealed class TownTimeLeverSetupPhase : IDungeonGenerationPhase
    {
        public const string TownFloorId = "town_main";

        const string LeverAResourcesPath = "Interactables/LeverSwitch_TownTime_A";
        const string LeverBResourcesPath = "Interactables/LeverSwitch_TownTime_B";
        const string LeverAEditorPath = "Assets/Data/Interactables/LeverSwitch_TownTime_A.asset";
        const string LeverBEditorPath = "Assets/Data/Interactables/LeverSwitch_TownTime_B.asset";

        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def == null || def.FloorId != TownFloorId)
                return;

            InteractableTileService interactables = InteractableTileService.Instance;
            MapManager map = MapManager.Instance;
            if (interactables == null || map == null)
            {
                DungeonGenerationLog.Warn($"{nameof(TownTimeLeverSetupPhase)} missing InteractableTileService or MapManager.");
                return;
            }

            interactables.SetOverlayMap(context.Instance.Tilemaps.InteractableOverlayMap);
            TownTimeService.EnsureRunService();

            if (!TryResolveMarkerCell(context, StampMarkerIds.TownTimeLeverA, out Vector3Int cellA)
                || !TryResolveMarkerCell(context, StampMarkerIds.TownTimeLeverB, out Vector3Int cellB))
            {
                DungeonGenerationLog.Warn($"{nameof(TownTimeLeverSetupPhase)} missing time lever markers.");
                return;
            }

            InteractableTileDefinition leverA = LoadLeverDefinition(LeverAResourcesPath, LeverAEditorPath);
            InteractableTileDefinition leverB = LoadLeverDefinition(LeverBResourcesPath, LeverBEditorPath);
            if (leverA == null || leverB == null)
            {
                TownTimeLeverContent.LeverPair runtime = TownTimeLeverContent.CreateRuntimeDefinitions();
                leverA = runtime.LeverA;
                leverB = runtime.LeverB;
                DungeonGenerationLog.Warn($"{nameof(TownTimeLeverSetupPhase)} using runtime lever definitions.");
            }

            interactables.Register(cellA, leverA);
            interactables.Register(cellB, leverB);
            TownTimeService.Instance?.SyncTimeLeverVisuals(interactables);

            DungeonGenerationLog.Phase(
                nameof(TownTimeLeverSetupPhase),
                $"time levers at {cellA} and {cellB}");
        }

        static InteractableTileDefinition LoadLeverDefinition(string resourcesPath, string editorPath)
        {
            InteractableTileDefinition def = Resources.Load<InteractableTileDefinition>(resourcesPath);
#if UNITY_EDITOR
            if (def == null && !string.IsNullOrEmpty(editorPath))
                def = AssetDatabase.LoadAssetAtPath<InteractableTileDefinition>(editorPath);
#endif
            return def;
        }

        static bool TryResolveMarkerCell(DungeonGenerationContext context, string markerId, out Vector3Int cell)
        {
            cell = default;
            DungeonLayoutStamp stamp = context.Definition?.LayoutStamp;
            return stamp != null && stamp.TryGetMarker(markerId, out cell);
        }
    }
}
