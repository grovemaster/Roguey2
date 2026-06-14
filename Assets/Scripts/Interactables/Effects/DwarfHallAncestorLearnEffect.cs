using JRogue.Actors;
using JRogue.Racial;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(
        fileName = "DwarfHallAncestorLearn",
        menuName = "JRogue/Interactables/Effects/Dwarf Hall Ancestor Learn")]
    public sealed class DwarfHallAncestorLearnEffect : InteractableEffect
    {
        public DwarfClanDefinition clan;

        public override void Execute(
            InteractableTileService service,
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source)
        {
            if (clan == null)
            {
                Debug.LogWarning("[DwarfClan] Hall altar effect missing clan definition.");
                return;
            }

            if (bumper == null)
            {
                Debug.LogWarning("[DwarfClan] Hall altar activated with no actor.");
                return;
            }

            DwarfAncestorAltarService.TryBeginPayRespects(bumper, clan);
        }
    }
}
