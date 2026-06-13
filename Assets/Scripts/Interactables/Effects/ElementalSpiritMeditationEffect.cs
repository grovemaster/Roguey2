using JRogue.Actors;
using JRogue.Racial;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(
        fileName = "ElementalSpiritMeditation",
        menuName = "JRogue/Interactables/Effects/Elemental Spirit Meditation")]
    public sealed class ElementalSpiritMeditationEffect : InteractableEffect
    {
        public ElementalSpiritMeditationGateDefinition gate;

        public override void Execute(
            InteractableTileService service,
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source)
        {
            if (gate == null)
            {
                Debug.LogWarning("[SpiritMeditation] Meditation effect missing gate definition.");
                return;
            }

            ElementalSpiritMeditationService.TryBeginMeditation(gate);
        }
    }
}
