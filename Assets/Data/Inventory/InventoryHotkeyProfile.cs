using UnityEngine;
using UnityEngine.InputSystem;

namespace JRogue.UI.Inventory
{
    /// <summary>Keyboard bindings for in-inventory actions (toggle scope, filters, sort, marks). Assign in inspector or override via <see cref="InventoryHotkeyRuntimeOverrides"/>.</summary>
    [CreateAssetMenu(fileName = "InventoryHotkeys", menuName = "JRogue/Inventory/Hotkey Profile")]
    public sealed class InventoryHotkeyProfile : ScriptableObject
    {
        public Key toggleBrowseScope = Key.Semicolon;
        public Key categoryPrevious = Key.LeftBracket;
        public Key categoryNext = Key.RightBracket;
        public Key toggleUsableFilter = Key.F;
        public Key cycleSortMode = Key.Digit0;
        public Key toggleFavoriteMark = Key.Digit1;
        public Key toggleProtectedMark = Key.Digit2;
        public Key toggleJunkMark = Key.Digit3;
        public Key markModifierRequired = Key.LeftCtrl;
    }
}
