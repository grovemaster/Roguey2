using JRogue.Actors;
using JRogue.Racial;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(
        fileName = "SoulBeastRitual",
        menuName = "JRogue/Interactables/Effects/Soul Beast Ritual")]
    public sealed class SoulBeastRitualEffect : InteractableEffect
    {
        public SoulBeastRitualGateDefinition gate;

        public override void Execute(
            InteractableTileService service,
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source)
        {
            if (gate == null)
            {
                Debug.LogWarning("[SoulBeastRitual] Ritual effect missing gate definition.");
                return;
            }

            SoulBeastRitualService.TryBeginRitual(gate);
        }
    }
}
