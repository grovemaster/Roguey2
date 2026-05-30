using JRogue.Interactables;
using UnityEngine;

namespace JRogue.World.Lighting
{
    /// <summary>
    /// Phase 3 QA content: unlit wall torch + interactable that applies <see cref="SetTileEmissionEffect"/>.
    /// </summary>
    public sealed class LightingPhase3SampleContent : MonoBehaviour
    {
        [SerializeField] bool wallTorchConfigured;
        [SerializeField] Vector3Int wallTorchCell;
        [SerializeField] LightEmitterDefinition torchDefinition;
        [SerializeField] InteractableTileDefinition wallTorchInteractable;

        public Vector3Int WallTorchCell => wallTorchCell;

        void OnEnable() => RegisterTorch();

        void Start() => RegisterTorch();

        void RegisterTorch()
        {
            if (!wallTorchConfigured)
            {
                Debug.LogWarning(
                    $"[Lighting:QA] {name}: wall torch not configured. "
                    + "Run JRogue/Lighting/Place Wall Torch Near Tiefling Mage.");
                return;
            }

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
