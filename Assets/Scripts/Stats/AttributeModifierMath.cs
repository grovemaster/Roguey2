using UnityEngine;

namespace JRogue.Stats
{
    /// <summary>
    /// Optional internal D&amp;D-style modifier. Not shown in UI in v0.
    /// </summary>
    public static class AttributeModifierMath
    {
        public static int Modifier(int score) => Mathf.FloorToInt((score - 10) / 2f);
    }
}
