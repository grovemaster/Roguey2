using JRogue.Interactables;
using JRogue.Manager.Map;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JRogue.World.Generation.Phases
{
    /// <summary>Registers the town meditation shrine interactable on town_main.</summary>
    public sealed class TownMeditationShrineSetupPhase : IDungeonGenerationPhase
    {
        public const string TownFloorId = "town_main";

        const string ShrineResourcesPath = "Interactables/MeditationShrine_Town";
        const string ShrineEditorPath = "Assets/Resources/Interactables/MeditationShrine_Town.asset";

        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def == null || def.FloorId != TownFloorId)
                return;

            InteractableTileService interactables = InteractableTileService.Instance;
            if (interactables == null)
            {
                DungeonGenerationLog.Warn($"{nameof(TownMeditationShrineSetupPhase)} missing InteractableTileService.");
                return;
            }

            interactables.SetOverlayMap(context.Instance.Tilemaps.InteractableOverlayMap);

            if (!TryResolveMarkerCell(context, StampMarkerIds.MeditationShrine, out Vector3Int cell))
            {
                DungeonGenerationLog.Warn($"{nameof(TownMeditationShrineSetupPhase)} missing meditation shrine marker.");
                return;
            }

            InteractableTileDefinition shrine = LoadShrineDefinition();
            if (shrine == null)
            {
                DungeonGenerationLog.Warn($"{nameof(TownMeditationShrineSetupPhase)} missing shrine definition.");
                return;
            }

            interactables.Register(cell, shrine);
            DungeonGenerationLog.Phase(nameof(TownMeditationShrineSetupPhase), $"meditation shrine at {cell}");
        }

        static InteractableTileDefinition LoadShrineDefinition()
        {
            InteractableTileDefinition def = Resources.Load<InteractableTileDefinition>(ShrineResourcesPath);
#if UNITY_EDITOR
            if (def == null)
                def = AssetDatabase.LoadAssetAtPath<InteractableTileDefinition>(ShrineEditorPath);
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
