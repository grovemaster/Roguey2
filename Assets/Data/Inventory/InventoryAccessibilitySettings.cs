using UnityEngine;

namespace JRogue.UI.Inventory
{
    /// <summary>Phase 3 accessibility: font scale + optional higher-contrast row tints. Tooltip delay reserved for future hover UI.</summary>
    [CreateAssetMenu(fileName = "InventoryAccessibility", menuName = "JRogue/Inventory/Accessibility Settings")]
    public sealed class InventoryAccessibilitySettings : ScriptableObject
    {
        [Range(0.75f, 1.6f)] public float listAndFooterFontScale = 1f;

        [Range(0.75f, 1.6f)] public float detailPaneFontScale = 1f;

        [Tooltip("When on, row tints use stronger separation (works with common CVD presets).")]
        public bool highContrastRows;

        public Color highContrastRowNormal = new Color(0.12f, 0.14f, 0.18f, 0.96f);

        public Color highContrastRowSelected = new Color(0.18f, 0.32f, 0.42f, 0.98f);

        [Min(0f)] public float tooltipShowDelaySeconds = 0.35f;
    }
}
