using JRogue.Interactables;
using JRogue.Manager.Map;
using JRogue.Racial;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JRogue.World.Generation.Phases
{
    /// <summary>Registers the Forge Brothers Hall of Ancestors altar on town_main.</summary>
    public sealed class TownDwarfClanAltarSetupPhase : IDungeonGenerationPhase
    {
        public const string TownFloorId = "town_main";

        const string AltarResourcesPath = "Interactables/HallOfAncestorsAltar_ForgeBrothers";
        const string AltarEditorPath = "Assets/Resources/Interactables/HallOfAncestorsAltar_ForgeBrothers.asset";
        const string ClanResourcesPath = "Racial/Dwarf/Clans/DwarfClan_ForgeBrothers";
        const string ClanEditorPath = "Assets/Resources/Racial/Dwarf/Clans/DwarfClan_ForgeBrothers.asset";

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

            if (!TryResolveMarkerCell(context, StampMarkerIds.ForgeBrothersAltar, out Vector3Int cell))
            {
                DungeonGenerationLog.Warn($"{nameof(TownDwarfClanAltarSetupPhase)} missing altar marker.");
                return;
            }

            InteractableTileDefinition altar = LoadAltarDefinition();
            if (altar == null)
            {
                DungeonGenerationLog.Warn($"{nameof(TownDwarfClanAltarSetupPhase)} missing altar definition.");
                return;
            }

            BindClanToEffect(altar);

            interactables.Register(cell, altar);
            DungeonGenerationLog.Phase(nameof(TownDwarfClanAltarSetupPhase), $"Hall altar at {cell}");
        }

        static void BindClanToEffect(InteractableTileDefinition altar)
        {
            if (altar?.onActivateEffects == null)
                return;

            DwarfClanDefinition clan = LoadClanDefinition();
            if (clan == null)
                return;

            for (int i = 0; i < altar.onActivateEffects.Length; i++)
            {
                if (altar.onActivateEffects[i] is DwarfHallAncestorLearnEffect effect)
                    effect.clan = clan;
            }
        }

        static InteractableTileDefinition LoadAltarDefinition()
        {
            InteractableTileDefinition def = Resources.Load<InteractableTileDefinition>(AltarResourcesPath);
#if UNITY_EDITOR
            if (def == null)
                def = AssetDatabase.LoadAssetAtPath<InteractableTileDefinition>(AltarEditorPath);
#endif
            return def;
        }

        static DwarfClanDefinition LoadClanDefinition()
        {
            DwarfClanDefinition def = Resources.Load<DwarfClanDefinition>(ClanResourcesPath);
#if UNITY_EDITOR
            if (def == null)
                def = AssetDatabase.LoadAssetAtPath<DwarfClanDefinition>(ClanEditorPath);
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
