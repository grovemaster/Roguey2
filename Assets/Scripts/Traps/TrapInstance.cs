using UnityEngine;

namespace JRogue.Traps
{
    public sealed class TrapInstance
    {
        public TrapDefinition Definition { get; }
        public Vector3Int HostCell { get; }

        public int ChargesRemaining { get; set; }
        public bool IsDetected { get; set; }
        public bool HasTriggered { get; set; }

        public TrapInstance(TrapDefinition definition, Vector3Int hostCell)
        {
            Definition = definition;
            HostCell = hostCell;
            ChargesRemaining = definition != null && definition.triggerLimit == TrapTriggerLimit.Finite
                ? definition.finiteCharges
                : 0;
        }

        public bool IsRevealed =>
            Definition != null
            && (Definition.initialVisibility == TrapVisibility.Visible
                || IsDetected
                || HasTriggered);

        public bool IsVisibleToPlayer => IsRevealed;

        public bool CanFire()
        {
            if (Definition == null)
                return false;

            return Definition.triggerLimit switch
            {
                TrapTriggerLimit.Once => !HasTriggered,
                TrapTriggerLimit.Finite => ChargesRemaining > 0,
                TrapTriggerLimit.Infinite => true,
                _ => false,
            };
        }
    }
}
