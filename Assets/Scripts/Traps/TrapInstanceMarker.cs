using UnityEngine;

namespace JRogue.Traps
{
    /// <summary>Scene marker that registers a trap on play (alternative to bootstrap lists).</summary>
    public sealed class TrapInstanceMarker : MonoBehaviour
    {
        [SerializeField] TrapDefinition definition;
        [SerializeField] Vector3Int hostCell;

        void Start()
        {
            if (TrapService.Instance == null || definition == null)
                return;

            Vector3Int cell = hostCell;
            if (cell == Vector3Int.zero)
            {
                var grid = FindAnyObjectByType<UnityEngine.Grid>();
                if (grid != null)
                    cell = grid.WorldToCell(transform.position);
            }

            TrapService.Instance.Register(cell, definition);
        }
    }
}
