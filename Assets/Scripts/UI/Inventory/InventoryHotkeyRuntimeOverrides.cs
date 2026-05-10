using UnityEngine;
using UnityEngine.InputSystem;

namespace JRogue.UI.Inventory
{
    /// <summary>Optional PlayerPrefs overrides for <see cref="InventoryHotkeyProfile"/> (Phase 3). Keys: JRogue.Inv.Key.&lt;fieldName&gt; storing <see cref="Key"/> int.</summary>
    public static class InventoryHotkeyRuntimeOverrides
    {
        const string Prefix = "JRogue.Inv.Key.";

        public static Key Resolve(string id, Key fallback)
        {
            string k = Prefix + id;
            if (!PlayerPrefs.HasKey(k))
                return fallback;
            int v = PlayerPrefs.GetInt(k, (int)fallback);
            return System.Enum.IsDefined(typeof(Key), v) ? (Key)v : fallback;
        }

        public static void SetOverride(string id, Key value)
        {
            PlayerPrefs.SetInt(Prefix + id, (int)value);
            PlayerPrefs.Save();
        }

        public static void ClearOverride(string id) => PlayerPrefs.DeleteKey(Prefix + id);

        public static Key ToggleBrowseScope(InventoryHotkeyProfile profile) =>
            Resolve(nameof(InventoryHotkeyProfile.toggleBrowseScope),
                profile != null ? profile.toggleBrowseScope : Key.Semicolon);

        public static Key CategoryPrevious(InventoryHotkeyProfile profile) =>
            Resolve(nameof(InventoryHotkeyProfile.categoryPrevious),
                profile != null ? profile.categoryPrevious : Key.LeftBracket);

        public static Key CategoryNext(InventoryHotkeyProfile profile) =>
            Resolve(nameof(InventoryHotkeyProfile.categoryNext),
                profile != null ? profile.categoryNext : Key.RightBracket);

        public static Key ToggleUsableFilter(InventoryHotkeyProfile profile) =>
            Resolve(nameof(InventoryHotkeyProfile.toggleUsableFilter),
                profile != null ? profile.toggleUsableFilter : Key.F);

        public static Key CycleSortMode(InventoryHotkeyProfile profile) =>
            Resolve(nameof(InventoryHotkeyProfile.cycleSortMode),
                profile != null ? profile.cycleSortMode : Key.Digit0);

        public static Key ToggleFavoriteMark(InventoryHotkeyProfile profile) =>
            Resolve(nameof(InventoryHotkeyProfile.toggleFavoriteMark),
                profile != null ? profile.toggleFavoriteMark : Key.Digit1);

        public static Key ToggleProtectedMark(InventoryHotkeyProfile profile) =>
            Resolve(nameof(InventoryHotkeyProfile.toggleProtectedMark),
                profile != null ? profile.toggleProtectedMark : Key.Digit2);

        public static Key ToggleJunkMark(InventoryHotkeyProfile profile) =>
            Resolve(nameof(InventoryHotkeyProfile.toggleJunkMark),
                profile != null ? profile.toggleJunkMark : Key.Digit3);

        public static Key MarkModifier(InventoryHotkeyProfile profile) =>
            Resolve(nameof(InventoryHotkeyProfile.markModifierRequired),
                profile != null ? profile.markModifierRequired : Key.LeftCtrl);
    }
}
