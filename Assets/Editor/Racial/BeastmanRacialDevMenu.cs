#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.Racial
{
    public static class BeastmanRacialDevMenu
    {
        [MenuItem("JRogue/Racial/Test Soul Beast Ritual")]
        public static void TestSoulBeastRitual()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[SoulBeastRitual] Enter Play mode to test the ritual flow.");
                return;
            }

            JRogue.Racial.SoulBeastRitualService.TryBeginRitualDev();
        }
    }
}
#endif
