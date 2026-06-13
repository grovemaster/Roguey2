using JRogue.Interactables;
using JRogue.Manager.Map;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JRogue.World.Generation.Phases
{
    public sealed class TownSoulBeastRitualSetupPhase : IDungeonGenerationPhase
    {
        public const string TownFloorId = "town_main";

        const string RitualResourcesPath = "Interactables/SoulBeastRitualCircle_Town";
        const string RitualEditorPath = "Assets/Resources/Interactables/SoulBeastRitualCircle_Town.asset";

        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def == null || def.FloorId != TownFloorId)
                return;

            InteractableTileService interactables = InteractableTileService.Instance;
            if (interactables == null)
            {
                DungeonGenerationLog.Warn($"{nameof(TownSoulBeastRitualSetupPhase)} missing InteractableTileService.");
                return;
            }

            interactables.SetOverlayMap(context.Instance.Tilemaps.InteractableOverlayMap);

            if (!TryResolveMarkerCell(context, StampMarkerIds.SoulBeastRitualCircle, out Vector3Int cell))
            {
                DungeonGenerationLog.Warn($"{nameof(TownSoulBeastRitualSetupPhase)} missing ritual circle marker.");
                return;
            }

            InteractableTileDefinition ritual = LoadRitualDefinition();
            if (ritual == null)
            {
                DungeonGenerationLog.Warn($"{nameof(TownSoulBeastRitualSetupPhase)} missing ritual definition.");
                return;
            }

            interactables.Register(cell, ritual);
            DungeonGenerationLog.Phase(nameof(TownSoulBeastRitualSetupPhase), $"ritual circle at {cell}");
        }

        static InteractableTileDefinition LoadRitualDefinition()
        {
            InteractableTileDefinition def = Resources.Load<InteractableTileDefinition>(RitualResourcesPath);
#if UNITY_EDITOR
            if (def == null)
                def = AssetDatabase.LoadAssetAtPath<InteractableTileDefinition>(RitualEditorPath);
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
