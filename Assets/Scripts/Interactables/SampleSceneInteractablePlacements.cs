using UnityEngine;

namespace JRogue.Interactables
{
    /// <summary>
    /// Legacy runtime registration. Prefer <see cref="InteractableTileBootstrap"/> +
    /// <see cref="InteractablePlacementSet"/> assets (Option B).
    /// </summary>
    [System.Obsolete("Use InteractableTileBootstrap with an InteractablePlacementSet asset.")]
    public sealed class SampleSceneInteractablePlacements : MonoBehaviour
    {
        void Awake()
        {
            if (InteractableTileService.Instance == null)
            {
                var svcGo = new GameObject("InteractableTileService");
                svcGo.transform.SetParent(transform);
                svcGo.AddComponent<InteractableTileService>();
            }
        }

        void Start()
        {
            if (InteractableTileService.Instance == null)
                return;

            InteractableLeverContent.LeverSet levers = InteractableLeverContent.CreateRuntimeDefinitions();

            Register(new Vector3Int(4, -6, 0), levers.First);
            Register(new Vector3Int(5, -6, 0), levers.Second);
            Register(new Vector3Int(6, -6, 0), levers.Third);
            Register(new Vector3Int(7, -6, 0), levers.Fourth);
        }

        static void Register(Vector3Int cell, InteractableTileDefinition definition)
        {
            InteractableTileService.Instance.Register(cell, definition);
        }
    }
}
