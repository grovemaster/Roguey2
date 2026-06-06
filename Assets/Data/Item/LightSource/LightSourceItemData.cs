using JRogue.World.Lighting;
using UnityEngine;

namespace JRogue.Item
{
    [CreateAssetMenu(fileName = "LightSource", menuName = "JRogue/Item/Light Source")]
    public class LightSourceItemData : ItemData
    {
        [Header("Light source")]
        public LightEmitterDefinition emitterDefinition;

        [Tooltip("When true, equipping emits light continuously (Handheld Torch).")]
        public bool emitsWhenEquipped;

        [Tooltip("When emitsWhenEquipped, start lit (v1 Handheld Torch).")]
        public bool startsLit = true;

        [Tooltip("Future: satisfy wall-torch ignite preconditions.")]
        public bool canIgniteWallTorches;

        public bool IsPassiveEquippedEmitter => emitsWhenEquipped && startsLit;
    }
}
