using System;
using UnityEngine;

namespace JRogue.UI.Inventory
{
    /// <summary>Remembers inventory UI state between opens (PlayerPrefs + JsonUtility).</summary>
    public static class InventorySessionPersistence
    {
        const string PrefsKey = "JRogue.Inventory.Session.v2";

        [Serializable]
        sealed class Dto
        {
            public int browseMode;
            public int memberCarouselIndex;
            public int categoryCycleIndex;
            public int usableOnly;
            public string searchNeedle = string.Empty;
            public int selection;
            public int sortMode;
        }

        public static void Save(
            int browseMode,
            int memberCarouselIndex,
            int categoryCycleIndex,
            bool usableOnly,
            string searchNeedle,
            int selection,
            InventorySortMode sort)
        {
            var dto = new Dto
            {
                browseMode = browseMode,
                memberCarouselIndex = memberCarouselIndex,
                categoryCycleIndex = categoryCycleIndex,
                usableOnly = usableOnly ? 1 : 0,
                searchNeedle = searchNeedle ?? string.Empty,
                selection = selection,
                sortMode = (int)sort
            };

            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(dto));
            PlayerPrefs.Save();
        }

        public static void Load(
            out int browseMode,
            out int memberCarouselIndex,
            out int categoryCycleIndex,
            out bool usableOnly,
            out string searchNeedle,
            out int selection,
            out InventorySortMode sort)
        {
            browseMode = 0;
            memberCarouselIndex = 0;
            categoryCycleIndex = 0;
            usableOnly = false;
            searchNeedle = string.Empty;
            selection = 0;
            sort = InventorySortMode.CategoryThenName;

            if (!PlayerPrefs.HasKey(PrefsKey))
                return;

            try
            {
                Dto dto = JsonUtility.FromJson<Dto>(PlayerPrefs.GetString(PrefsKey, string.Empty));
                if (dto == null)
                    return;

                browseMode = Enum.IsDefined(typeof(InventoryUI.BrowseMode), dto.browseMode)
                    ? dto.browseMode
                    : 0;

                memberCarouselIndex = Mathf.Max(0, dto.memberCarouselIndex);
                int maxCategoryIndex = ItemCategoryRegistry.CategoriesForFilterCycle().Count;
                categoryCycleIndex = Mathf.Clamp(dto.categoryCycleIndex, 0, maxCategoryIndex);
                usableOnly = dto.usableOnly != 0;
                searchNeedle = dto.searchNeedle ?? string.Empty;
                selection = Mathf.Max(0, dto.selection);
                sort = Enum.IsDefined(typeof(InventorySortMode), dto.sortMode)
                    ? (InventorySortMode)dto.sortMode
                    : InventorySortMode.CategoryThenName;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Inventory] Session prefs ignored: {e.Message}");
            }
        }
    }
}
