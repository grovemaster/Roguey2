using JRogue.Interactables;
using UnityEngine;

namespace JRogue.World.Lighting
{
    /// <summary>
    /// Phase 3 QA content: unlit wall torch + interactable that applies <see cref="SetTileEmissionEffect"/>.
    /// </summary>
    public sealed class LightingPhase3SampleContent : MonoBehaviour
    {
        [SerializeField] Vector3Int wallTorchCell = new Vector3Int(4, -2, 0);
        [SerializeField] LightEmitterDefinition torchDefinition;
        [SerializeField] InteractableTileDefinition wallTorchInteractable;

        void OnEnable()
        {
            if (LightingService.Instance != null && torchDefinition != null)
            {
                LightingService.Instance.EnableEmitter(
                    wallTorchCell,
                    torchDefinition,
                    initialEmission: 0,
                    reason: "phase3-unlit");
            }

            if (wallTorchInteractable != null && InteractableTileService.Instance != null)
                InteractableTileService.Instance.Register(wallTorchCell, wallTorchInteractable);
        }
    }
}
