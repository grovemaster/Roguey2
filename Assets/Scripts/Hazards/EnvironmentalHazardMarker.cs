using UnityEngine;

namespace JRogue.Hazards
{
    /// <summary>Registers a hazard at this object's grid cell on play.</summary>
    public sealed class EnvironmentalHazardMarker : MonoBehaviour
    {
        [SerializeField] EnvironmentalHazardDefinition definition;
        [SerializeField] bool startHidden;

        void Start()
        {
            if (definition == null || HazardService.Instance == null)
                return;

            Vector3Int cell = Vector3Int.FloorToInt(transform.position);
            HazardService.Instance.Register(cell, definition, startHidden);
        }
    }
}
