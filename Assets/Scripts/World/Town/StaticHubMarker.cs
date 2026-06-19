using UnityEngine;

namespace JRogue.World.Town
{
    /// <summary>Authoring marker for scene-painted hub floors (portal, spawn, NPC slots).</summary>
    [DisallowMultipleComponent]
    public sealed class StaticHubMarker : MonoBehaviour
    {
        [SerializeField] StaticHubMarkerKind kind;
        [SerializeField] string markerId;
        [SerializeField] Vector3Int cell;

        public StaticHubMarkerKind Kind => kind;
        public string MarkerId => markerId;
        public Vector3Int Cell => cell;

#if UNITY_EDITOR
        public void EditorConfigure(StaticHubMarkerKind markerKind, Vector3Int gridCell, string id = null)
        {
            kind = markerKind;
            cell = gridCell;
            markerId = id;
        }
#endif
    }
}
