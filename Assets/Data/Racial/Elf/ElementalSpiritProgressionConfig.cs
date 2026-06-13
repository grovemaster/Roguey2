using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(
        fileName = "ElementalSpiritProgressionConfig",
        menuName = "JRogue/Racial/Elemental Spirit Progression Config")]
    public sealed class ElementalSpiritProgressionConfig : ScriptableObject
    {
        public ElementalSpiritLevelCurve defaultLevelCurve;

        static ElementalSpiritProgressionConfig _cached;

        public static ElementalSpiritProgressionConfig Load()
        {
            if (_cached != null)
                return _cached;

            _cached = Resources.Load<ElementalSpiritProgressionConfig>("Racial/Elf/ElementalSpiritProgressionConfig");
            return _cached;
        }

#if UNITY_EDITOR
        public static void SetForTests(ElementalSpiritProgressionConfig config) => _cached = config;

        public static void ResetForTests() => _cached = null;
#endif
    }
}
