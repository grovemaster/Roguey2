using JRogue.Actors.Components;
using UnityEngine;

namespace JRogue.World.Rift
{
    /// <summary>Marks the rift boss; notifies <see cref="RiftService"/> on death.</summary>
    public sealed class RiftBossHost : MonoBehaviour
    {
        RiftDefinition _rift;
        HealthComponent _health;
        bool _notified;

        public void Initialize(RiftDefinition rift)
        {
            _rift = rift;
            _health = GetComponent<HealthComponent>();
            if (_health != null)
                _health.Died += OnDied;
        }

        void OnDestroy()
        {
            if (_health != null)
                _health.Died -= OnDied;
        }

        void OnDied() => Notify();

        void Notify()
        {
            if (_notified || _rift == null)
                return;
            _notified = true;
            RiftService.NotifyBossDied(_rift);
        }
    }
}
