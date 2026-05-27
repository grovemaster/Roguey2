using UnityEngine;

namespace JRogue.Interactables
{
    /// <summary>Registers an interactable at this object's grid cell on play.</summary>
    public sealed class InteractableTileMarker : MonoBehaviour
    {
        [SerializeField] InteractableTileDefinition definition;

        void Start()
        {
            if (definition == null || InteractableTileService.Instance == null)
                return;

            Vector3Int cell = Vector3Int.FloorToInt(transform.position);
            InteractableTileService.Instance.Register(cell, definition);
        }
    }
}
