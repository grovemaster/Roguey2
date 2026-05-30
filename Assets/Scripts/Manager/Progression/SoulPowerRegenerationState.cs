using UnityEngine;

namespace JRogue.Manager.Progression
{
    /// <summary>Per-actor fractional Soul Power regen accumulator (DCSS-style).</summary>
    public sealed class SoulPowerRegenerationState : MonoBehaviour
    {
        public float Accumulator;
    }
}
