using JRogue.Interactables;
using JRogue.World.Town;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    /// <summary>Registers inn beds (bump → sleep prompt) on residential inn interior.</summary>
    public sealed class TownInnBedSetupPhase : IDungeonGenerationPhase
    {
        const string InnBedResourcesPath = "Interactables/InnBed_Town";
        const string InnBedEditorPath = "Assets/Resources/Interactables/InnBed_Town.asset";

        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context?.Definition;
            if (def == null || def.FloorId != ResidentialInnLayout.InteriorFloorId)
                return;

            InteractableTileDefinition bedDefinition = LoadBedDefinition();
            if (bedDefinition == null)
            {
                DungeonGenerationLog.Warn($"{nameof(TownInnBedSetupPhase)} missing inn bed definition.");
                return;
            }

            InteractableTileService service = InteractableTileService.Instance;
            if (service == null)
            {
                DungeonGenerationLog.Warn($"{nameof(TownInnBedSetupPhase)} missing {nameof(InteractableTileService)}.");
                return;
            }

            int registered = 0;
            foreach (Vector3Int cell in ResidentialInnLayout.EnumerateBedCells())
            {
                service.Register(cell, bedDefinition);
                registered++;
            }

            DungeonGenerationLog.Phase(nameof(TownInnBedSetupPhase), $"registered {registered} inn bed(s).");
        }

        static InteractableTileDefinition LoadBedDefinition()
        {
            InteractableTileDefinition def = Resources.Load<InteractableTileDefinition>(InnBedResourcesPath);
#if UNITY_EDITOR
            if (def == null)
            {
                def = UnityEditor.AssetDatabase.LoadAssetAtPath<InteractableTileDefinition>(InnBedEditorPath);
            }
#endif
            return def;
        }
    }
}
