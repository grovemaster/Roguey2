using UnityEngine;

namespace JRogue.Racial
{
    public static class SoulBeastRegistryService
    {
        static SoulBeastRegistry _registry;

        public static SoulBeastRegistry Registry
        {
            get
            {
                if (_registry == null)
                    _registry = Resources.Load<SoulBeastRegistry>("Racial/Beastman/SoulBeastRegistry");

                return _registry;
            }
        }

        public static bool TryGetDefinition(string soulBeastId, out SoulBeastDefinition beast)
        {
            beast = null;
            SoulBeastRegistry registry = Registry;
            return registry != null && registry.TryGetById(soulBeastId, out beast);
        }

#if UNITY_EDITOR
        public static void SetRegistryForTests(SoulBeastRegistry registry) => _registry = registry;

        public static void ResetRegistryForTests() => _registry = null;
#endif
    }
}
