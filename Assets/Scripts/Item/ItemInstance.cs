using System;
using UnityEngine;

namespace JRogue.Item
{
    /// <summary>
    /// Runtime handle for a physical item (bag, equipped, or world). Distinct <see cref="Id"/> even when
    /// <see cref="Definition"/> matches another instance (e.g. two "Giant's Blade" drops).
    /// World drops use <see cref="WorldItem"/> until pickup; a future explicit Location/OnGround model can wrap this type.
    /// </summary>
    [Serializable]
    public sealed class ItemInstance
    {
        [SerializeField] string id;

        [SerializeField] ItemData definition;

        [SerializeField] int quantity = 1;

        [SerializeField] ItemStorageLocation storageLocation = ItemStorageLocation.Unknown;

        [SerializeField] ItemUserMark userMarks;

        [SerializeField] string userInscription = string.Empty;

        [SerializeField] bool isAppraised;

        public const int MaxInscriptionLength = 280;

        public ItemInstance(ItemData def, int qty = 1)
        {
            id = Guid.NewGuid().ToString("N");
            definition = def;
            quantity = Mathf.Max(1, qty);
        }

        /// <summary>For tools/tests—prefer the constructor that generates a new id.</summary>
        public ItemInstance(string existingId, ItemData def, int qty = 1)
        {
            id = string.IsNullOrEmpty(existingId) ? Guid.NewGuid().ToString("N") : existingId;
            definition = def;
            quantity = Mathf.Max(1, qty);
        }

        public string Id => id;

        public ItemData Definition
        {
            get => definition;
            set => definition = value;
        }

        public int Quantity
        {
            get => quantity;
            set => quantity = Mathf.Max(1, value);
        }

        public float TotalWeight => definition != null ? definition.weight * quantity : 0f;

        public bool IsCurrency => definition != null && definition.category == ItemCategory.Currency;

        public ItemStorageLocation StorageLocation
        {
            get => storageLocation;
            set => storageLocation = value;
        }

        public ItemUserMark UserMarks
        {
            get => userMarks;
            set => userMarks = value;
        }

        public string UserInscription
        {
            get => userInscription ?? string.Empty;
            set => userInscription = ClampInscription(value);
        }

        /// <summary>When false and definition <see cref="ItemData.requiresAppraisal"/>, UI shows ? for value.</summary>
        public bool IsAppraised
        {
            get => isAppraised;
            set => isAppraised = value;
        }

        public bool ToggleMark(ItemUserMark mark)
        {
            bool had = (userMarks & mark) != 0;
            userMarks ^= mark;
            return !had;
        }

        static string ClampInscription(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            string v = value.Replace('\r', ' ').Replace('\n', ' ');
            return v.Length <= MaxInscriptionLength ? v : v.Substring(0, MaxInscriptionLength);
        }
    }
}

