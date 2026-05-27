using UnityEngine;

namespace JRogue.Manager.Door
{
    /// <summary>Stub door registry for interactable effects (v0).</summary>
    public sealed class DoorService : MonoBehaviour
    {
        public static DoorService Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
                Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Open(string doorId)
        {
            Debug.Log($"[Door] Open requested for '{doorId}' (stub — not implemented).");
        }
    }
}
