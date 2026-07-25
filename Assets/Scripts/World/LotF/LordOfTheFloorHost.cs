using UnityEngine;

namespace JRogue.World.LotF
{
    /// <summary>Marks a spawned LotF host so death/despawn consumes the run slot.</summary>
    public sealed class LordOfTheFloorHost : MonoBehaviour
    {
        [SerializeField] string lotfId;

        bool _ended;

        public string LotfId => lotfId;

        public void Initialize(string id)
        {
            lotfId = id;
        }

        void OnDestroy()
        {
            NotifyEnded();
        }

        public void NotifyEnded()
        {
            if (_ended || string.IsNullOrEmpty(lotfId))
                return;

            _ended = true;
            LordOfTheFloorService.NotifyHostEnded(lotfId);
        }
    }
}
