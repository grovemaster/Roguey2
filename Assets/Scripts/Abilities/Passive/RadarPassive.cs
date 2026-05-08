using JRogue.Actors;
using JRogue.Core.Actor;
using JRogue.Service.Sensing;
using UnityEngine;

namespace JRogue.Ability.Passive
{
    /// <summary>
    /// Always-on radar that pulses every turn while the essence is equipped.
    /// Spiritual analog to DCSS's Ashenzari "Sense Surroundings" piety perk.
    /// </summary>
    [CreateAssetMenu(fileName = "RadarPassive", menuName = "JRogue/Passives/Radar")]
    public class RadarPassive : PassiveEffect
    {
        [Header("Radar Settings")]
        [Min(1)] public int pulseRadius = 10;

        [Tooltip("Combine flags to detect multiple categories per pulse.")]
        public EssenceType filter = EssenceType.Undead;

        public override void OnApply(GameObject user) => Pulse(user);
        public override void OnRemove(GameObject user) { }
        public override void OnTurnStart(GameObject user) => Pulse(user);

        private void Pulse(GameObject user)
        {
            if (user == null) return;
            if (!user.TryGetComponent<BaseActor>(out var actor)) return;
            RadarUtility.Pulse(actor, pulseRadius, filter);
        }
    }
}
