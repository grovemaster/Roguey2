using JRogue.Actors;
using UnityEngine;

namespace JRogue.UI.Hotbar
{
    public sealed class HotbarLayout : MonoBehaviour
    {
        public const int HotbarMainSlotCount = 10;

        [SerializeField] HotbarEntry[] mainSlots = new HotbarEntry[HotbarMainSlotCount];

        public static HotbarLayout EnsureOn(BaseActor actor)
        {
            if (actor == null)
                return null;

            HotbarLayout layout = actor.GetComponent<HotbarLayout>();
            if (layout == null)
                layout = actor.gameObject.AddComponent<HotbarLayout>();

            layout.EnsureSlotArray();
            layout.SeedDefaultsIfEmpty();
            return layout;
        }

        public HotbarEntry GetSlot(int index)
        {
            EnsureSlotArray();
            if (index < 0 || index >= mainSlots.Length)
                return EmptyEntry();

            return mainSlots[index]?.Clone() ?? EmptyEntry();
        }

        public void SetSlot(int index, HotbarEntry entry)
        {
            EnsureSlotArray();
            if (index < 0 || index >= mainSlots.Length)
                return;

            mainSlots[index] = entry?.Clone() ?? EmptyEntry();
        }

        public void SwapSlots(int indexA, int indexB)
        {
            EnsureSlotArray();
            if (indexA < 0 || indexA >= mainSlots.Length || indexB < 0 || indexB >= mainSlots.Length)
                return;

            HotbarEntry temp = mainSlots[indexA];
            mainSlots[indexA] = mainSlots[indexB];
            mainSlots[indexB] = temp;
        }

        public void SeedDefaultsIfEmpty()
        {
            EnsureSlotArray();
            if (!IsCompletelyEmpty())
                return;

            for (int slot = 0; slot < 3 && slot < mainSlots.Length; slot++)
            {
                mainSlots[slot] = new HotbarEntry
                {
                    Kind = HotbarEntryKind.EssenceActive,
                    essenceSlotIndex = slot,
                    abilityIndex = 0,
                };
            }
        }

        void EnsureSlotArray()
        {
            if (mainSlots == null || mainSlots.Length != HotbarMainSlotCount)
                mainSlots = new HotbarEntry[HotbarMainSlotCount];

            for (int i = 0; i < mainSlots.Length; i++)
            {
                if (mainSlots[i] == null)
                    mainSlots[i] = EmptyEntry();
            }
        }

        bool IsCompletelyEmpty()
        {
            for (int i = 0; i < mainSlots.Length; i++)
            {
                if (mainSlots[i] != null && !mainSlots[i].IsEmpty())
                    return false;
            }

            return true;
        }

        static HotbarEntry EmptyEntry() =>
            new HotbarEntry { Kind = HotbarEntryKind.Empty };
    }
}
