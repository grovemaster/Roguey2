using UnityEngine;

namespace JRogue.World.Lighting
{
    /// <summary>Scene marker: registers emitter and/or receiver data at this grid cell on play.</summary>
    [DefaultExecutionOrder(0)]
    public sealed class LightingCellMarker : MonoBehaviour
    {
        [SerializeField] bool isEmitter;
        [SerializeField] LightEmitterDefinition emitterDefinition;

        [SerializeField]
        [Range(LightLevel.Min, LightLevel.Max)]
        int initialEmission = LightLevel.TorchEmission;

        [SerializeField] bool isReceiver = true;

        [SerializeField]
        [Min(0)]
        int ambientRegionId;

        [SerializeField] Vector3Int cellOffset;
        LightingService _service;

        void Awake()
        {
            _service = ResolveService();
            if (_service == null)
                return;

            Vector3Int cell = ResolveCell();
            LightCellData data;

            if (isEmitter)
            {
                if (emitterDefinition == null)
                {
                    Debug.LogWarning($"[Lighting] {name} is emitter but has no definition.");
                    return;
                }

                data = LightCellData.Emitter(
                    emitterDefinition,
                    initialEmission,
                    ambientRegionId,
                    isReceiver);
            }
            else if (isReceiver)
            {
                data = LightCellData.Receiver(
                    ambientRegionId,
                    LightLevel.PitchDark);
            }
            else
            {
                return;
            }

            _service.RegisterPending(cell, data);
        }

        Vector3Int ResolveCell()
        {
            Vector3Int baseCell = Vector3Int.FloorToInt(transform.position) + cellOffset;
            return new Vector3Int(baseCell.x, baseCell.y, 0);
        }

        LightingService ResolveService()
        {
            if (LightingService.Instance != null)
                return LightingService.Instance;

            LightingService onSelf = GetComponent<LightingService>();
            if (onSelf != null)
                return onSelf;

            return FindAnyObjectByType<LightingService>();
        }
    }
}
