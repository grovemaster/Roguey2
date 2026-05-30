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

        [SerializeField] string manaStoneSourceSpeciesId = string.Empty;

        [SerializeField] int currentCharges;

        [SerializeField] int maxCharges;

        [SerializeField] int rechargePhasesAccumulated;

        public const int MaxInscriptionLength = 280;

        public ItemInstance(ItemData def, int qty = 1)
        {
            id = Guid.NewGuid().ToString("N");
            definition = def;
            quantity = ResolveQuantity(def, qty);
            ApplyDefinitionDefaults(def);
        }

        /// <summary>For tools/tests—prefer the constructor that generates a new id.</summary>
        public ItemInstance(string existingId, ItemData def, int qty = 1)
        {
            id = string.IsNullOrEmpty(existingId) ? Guid.NewGuid().ToString("N") : existingId;
            definition = def;
            quantity = ResolveQuantity(def, qty);
            ApplyDefinitionDefaults(def);
        }

        static int ResolveQuantity(ItemData def, int qty)
        {
            if (def != null && def.category == ItemCategory.Evocable)
                return 1;
            return Mathf.Max(1, qty);
        }

        void ApplyDefinitionDefaults(ItemData def)
        {
            if (def is EvocableItemData evocable)
                EvocableChargeRules.InitializeCharges(this, evocable);
        }

        /// <summary>Creates a runtime instance with evocable charge state initialized.</summary>
        public static ItemInstance CreateFromDefinition(ItemData def, int? startingChargesOverride = null)
        {
            var inst = new ItemInstance(def, 1);
            if (def is EvocableItemData evocable)
                EvocableChargeRules.InitializeCharges(inst, evocable, startingChargesOverride);
            return inst;
        }

        /// <summary>Creates an evocable with explicit starting charges (clamped to max).</summary>
        public static ItemInstance CreateEvocable(EvocableItemData def, int? startingChargesOverride = null) =>
            CreateFromDefinition(def, startingChargesOverride);

        public string Id => id;

        public ItemData Definition
        {
            get => definition;
            set => definition = value;
        }

        public int Quantity
        {
            get => quantity;
            set => quantity = definition != null && definition.category == ItemCategory.Evocable
                ? 1
                : Mathf.Max(1, value);
        }

        public int CurrentCharges
        {
            get => currentCharges;
            set => currentCharges = value;
        }

        public int MaxCharges
        {
            get => maxCharges;
            set => maxCharges = value;
        }

        public int RechargePhasesAccumulated
        {
            get => rechargePhasesAccumulated;
            set => rechargePhasesAccumulated = Mathf.Max(0, value);
        }

        public void SetCharges(int current, int max)
        {
            maxCharges = Mathf.Max(1, max);
            currentCharges = Mathf.Clamp(current, 0, maxCharges);
        }

        public bool IsEvocable => definition != null && definition.category == ItemCategory.Evocable;

        public float TotalWeight => definition != null ? definition.weight * quantity : 0f;

        public bool IsCurrency => definition != null && definition.category == ItemCategory.Currency;

        public bool IsManaStone => definition is ManaStoneItemData;

        public string ManaStoneSourceSpeciesId
        {
            get => manaStoneSourceSpeciesId ?? string.Empty;
            set => manaStoneSourceSpeciesId = value ?? string.Empty;
        }

        public static ItemInstance CreateManaStone(ManaStoneItemData definition, string sourceSpeciesId, int qty = 1)
        {
            var inst = new ItemInstance(definition, qty);
            inst.ManaStoneSourceSpeciesId = string.IsNullOrEmpty(sourceSpeciesId) ? "unknown" : sourceSpeciesId;
            inst.StorageLocation = ItemStorageLocation.OnGround;
            return inst;
        }

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

