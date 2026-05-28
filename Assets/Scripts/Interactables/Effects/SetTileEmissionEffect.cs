using JRogue.Actors;
using JRogue.World.Lighting;
using UnityEngine;

namespace JRogue.Interactables
{
    [CreateAssetMenu(
        fileName = "SetTileEmission",
        menuName = "JRogue/Interactables/Effects/Set Tile Emission")]
    public sealed class SetTileEmissionEffect : InteractableEffect
    {
        [SerializeField] bool applyToActivatedCell = true;
        [SerializeField] Vector3Int targetCell;
        [SerializeField] int emissionLevel = LightLevel.TorchEmission;
        [SerializeField] string reasonId = "interactable";

        public override void Execute(
            InteractableTileService service,
            InteractableTileInstance instance,
            BaseActor bumper,
            InteractableActivationSource source)
        {
            if (LightingService.Instance == null || instance == null)
                return;

            Vector3Int cell = applyToActivatedCell ? instance.Cell : targetCell;
            LightingService.Instance.SetEmission(cell, emissionLevel, reasonId);
        }
    }
}
