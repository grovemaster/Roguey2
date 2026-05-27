using UnityEngine;

namespace JRogue.Status
{
    [DisallowMultipleComponent]
    public sealed class StatusImmunity : MonoBehaviour
    {
        [SerializeField] bool immunePoisoned;

        public bool IsImmuneTo(StatusEffectId id)
        {
            return id switch
            {
                StatusEffectId.Poisoned => immunePoisoned,
                _ => false
            };
        }
    }
}
