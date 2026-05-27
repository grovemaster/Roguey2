using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Traps
{
    [CreateAssetMenu(fileName = "TrapDefinition", menuName = "JRogue/Traps/Trap Definition")]
    public sealed class TrapDefinition : ScriptableObject
    {
        public TrapId trapId = TrapId.None;
        public string displayName = "Trap";
        public TrapPlacement placement = TrapPlacement.Floor;
        public TrapVisibility initialVisibility = TrapVisibility.Visible;

        [Min(0)]
        public int detectionThreshold = 12;

        public TrapTriggerLimit triggerLimit = TrapTriggerLimit.Infinite;

        [Min(1)]
        public int finiteCharges = 3;

        [Min(1)]
        public int triggerRange = 1;

        [Min(0)]
        public int piercingDamage = 8;

        public Tile disguiseFloorTile;
        public Tile disguiseWallTile;
        public Sprite revealedSprite;

        [Tooltip("Optional sprite shown after the trap has fired (e.g. dart launch frame).")]
        public Sprite revealedTriggeredSprite;

        [Tooltip("Reserved for status/debuff hooks when that system exists.")]
        public Object[] futureEffects = System.Array.Empty<Object>();
    }
}
