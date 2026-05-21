using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Data-driven racial prohibition (item category ban, eligibility block, etc.).
    /// Used on <see cref="IRacialProgressionPayload"/> sources (Tiefling implants, Undead skill nodes)—not on folk baseline loadouts.
    /// </summary>
    public abstract class RacialRestrictionDefinition : ScriptableObject
    {
        public virtual void OnApply(GameObject target) { }

        public virtual void OnRemove(GameObject target) { }
    }
}
