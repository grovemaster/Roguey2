using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Data-driven racial advantage (immunity, alternate healing channel, etc.).
    /// Used on <see cref="IRacialProgressionPayload"/> sources (Tiefling implants, Undead skill nodes)—not on folk baseline loadouts.
    /// </summary>
    public abstract class RacialBenefitDefinition : ScriptableObject
    {
        public virtual void OnApply(GameObject target) { }

        public virtual void OnRemove(GameObject target) { }

        public virtual void Refresh(GameObject target) { }

        public virtual void OnTurnStart(GameObject target) { }
    }
}
