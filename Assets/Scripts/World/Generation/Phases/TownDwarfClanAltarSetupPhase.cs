using JRogue.Interactables;
using JRogue.Manager.Map;
using JRogue.Racial;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JRogue.World.Generation.Phases
{
    /// <summary>Registers dwarf clan Hall of Ancestors altars on town_main.</summary>
    public sealed class TownDwarfClanAltarSetupPhase : IDungeonGenerationPhase
    {
        public const string TownFloorId = "town_main";

        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def == null || def.FloorId != TownFloorId)
                return;

            InteractableTileService interactables = InteractableTileService.Instance;
            if (interactables == null)
            {
                DungeonGenerationLog.Warn($"{nameof(TownDwarfClanAltarSetupPhase)} missing InteractableTileService.");
                return;
            }

            interactables.SetOverlayMap(context.Instance.Tilemaps.InteractableOverlayMap);

            int registered = 0;
            for (int i = 0; i < DwarfClanTownEntries.Altars.Length; i++)
            {
                if (TryRegisterAltar(context, interactables, DwarfClanTownEntries.Altars[i]))
                    registered++;
            }

            DungeonGenerationLog.Phase(
                nameof(TownDwarfClanAltarSetupPhase),
                $"registered {registered} Hall altar(s).");
        }

        static bool TryRegisterAltar(
            DungeonGenerationContext context,
            InteractableTileService interactables,
            DwarfClanTownEntries.AltarEntry entry)
        {
            if (!TryResolveMarkerCell(context, entry.MarkerId, out Vector3Int cell))
            {
                DungeonGenerationLog.Warn(
                    $"{nameof(TownDwarfClanAltarSetupPhase)} missing altar marker '{entry.MarkerId}'.");
                return false;
            }

            InteractableTileDefinition altar = LoadAltarDefinition(entry);
            if (altar == null)
            {
                DungeonGenerationLog.Warn(
                    $"{nameof(TownDwarfClanAltarSetupPhase)} missing altar definition '{entry.AltarResourcesPath}'.");
                return false;
            }

            interactables.Register(cell, altar);
            DungeonGenerationLog.Phase(nameof(TownDwarfClanAltarSetupPhase), $"Hall altar at {cell} ({entry.MarkerId})");
            return true;
        }

        static InteractableTileDefinition LoadAltarDefinition(DwarfClanTownEntries.AltarEntry entry)
        {
            InteractableTileDefinition def = Resources.Load<InteractableTileDefinition>(entry.AltarResourcesPath);
#if UNITY_EDITOR
            if (def == null)
                def = AssetDatabase.LoadAssetAtPath<InteractableTileDefinition>(entry.AltarEditorPath);
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
