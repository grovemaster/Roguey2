using UnityEngine;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Ability;
using JRogue.Stats;
using JRogue.Racial;
using JRogue.Stats.Racial;

namespace JRogue.Manager.Essence
{
    public class EssenceSlotManager : MonoBehaviour
    {
        [Header("Configuration")]
        public int totalSlots = 3;

        // This array holds the actual ScriptableObjects currently equipped
        [SerializeField] private EssenceData[] equippedEssences;

        CharacterStats _stats;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            ApplyMaxSlotsFromClass();

            if (equippedEssences == null)
                return;

            for (int i = 0; i < equippedEssences.Length; i++)
            {
                EssenceData essence = equippedEssences[i];
                if (essence != null)
                {
                    essence.Apply(gameObject);
                    RegisterEssenceBodyContribution(i, essence);
                }
            }
        }

        public void ApplyMaxSlotsFromClass()
        {
            if (_stats == null)
                _stats = GetComponent<CharacterStats>();

            int max = _stats != null
                ? HumanClassRules.GetMaxEssenceSlots(_stats.humanClass)
                : HumanClassRules.DefaultEssenceSlotCount;

            if (max < totalSlots && equippedEssences != null)
            {
                for (int i = max; i < totalSlots; i++)
                    UnequipEssence(i);
            }

            totalSlots = max;
            EnsureEquippedArraySize();
        }

        public bool EquipEssence(EssenceData newEssence, int slotIndex)
        {
            if (_stats == null)
                _stats = GetComponent<CharacterStats>();

            if (_stats != null && newEssence != null && !HumanClassRules.CanGainEssences(_stats.humanClass))
            {
                Debug.LogWarning(
                    $"[{gameObject.name}] Cannot equip essences as {_stats.humanClass}.");
                return false;
            }

            if (totalSlots <= 0 || slotIndex < 0 || slotIndex >= totalSlots)
                return false;

            EnsureEquippedArraySize();
            if (slotIndex >= equippedEssences.Length)
                return false;

            // 1. Remove bonuses from the old essence if one exists in this slot
            if (equippedEssences[slotIndex] != null)
            {
                UnregisterEssenceBodyContribution(slotIndex);
                equippedEssences[slotIndex].Remove(gameObject);
            }

            // 2. Place the new essence and apply its bonuses
            equippedEssences[slotIndex] = newEssence;

            if (newEssence != null)
            {
                newEssence.Apply(gameObject);
                RegisterEssenceBodyContribution(slotIndex, newEssence);
                Debug.Log($"{gameObject.name} equipped {newEssence.essenceName}!");
                HumanPriestVowLogic.NotifyEssenceEquipped(GetComponent<BaseActor>());
            }

            return true;
        }

        public void UnequipEssence(int slotIndex)
        {
            EquipEssence(null, slotIndex);
        }

        public EssenceData GetEssenceInSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= totalSlots || slotIndex >= equippedEssences.Length) return null;
            return equippedEssences[slotIndex];
        }

        public bool HasEssence(EssenceData data)
        {
            if (data == null || equippedEssences == null)
                return false;

            for (int i = 0; i < equippedEssences.Length; i++)
            {
                if (equippedEssences[i] == data)
                    return true;
            }

            return false;
        }

        public int CountOccupiedSlots()
        {
            int count = 0;
            if (equippedEssences == null)
                return count;

            int limit = Mathf.Min(totalSlots, equippedEssences.Length);
            for (int i = 0; i < limit; i++)
            {
                if (equippedEssences[i] != null)
                    count++;
            }

            return count;
        }

        public bool HasFreeSlot() => CountOccupiedSlots() < totalSlots;

        public int GetFirstFreeSlotIndex()
        {
            if (equippedEssences == null)
                return -1;

            int limit = Mathf.Min(totalSlots, equippedEssences.Length);
            for (int i = 0; i < limit; i++)
            {
                if (equippedEssences[i] == null)
                    return i;
            }

            return -1;
        }

        public bool TryAcquireEssence(EssenceData data)
        {
            if (data == null || HasEssence(data))
                return false;

            int slot = GetFirstFreeSlotIndex();
            if (slot < 0)
                return false;

            return EquipEssence(data, slot);
        }

        // Inside EssenceSlotManager.cs
        public AbilityAction GetAbility(int slotIndex, int subIndex)
        {
            // Ensure the slot exists and has an essence
            if (slotIndex >= 0 && slotIndex < equippedEssences.Length)
            {
                var essence = equippedEssences[slotIndex];
                if (essence != null && subIndex < essence.activeAbilities.Count)
                {
                    return essence.activeAbilities[subIndex];
                }
            }
            return null;
        }

        public void TriggerEssenceAbility(int slotIndex, int abilityIndex)
        {
            EssenceData essence = GetEssenceInSlot(slotIndex);
            if (essence == null || abilityIndex >= essence.activeAbilities.Count) return;

            AbilityAction ability = essence.activeAbilities[abilityIndex];
            var stats = GetComponent<CharacterStats>();

            if (HumanClassAbilityResources.CanAfford(stats, ability))
            {
                if (ability.Execute(gameObject))
                {
                    HumanClassAbilityResources.TrySpend(stats, ability);
                    Debug.Log($"Executed {ability.abilityName} from {essence.essenceName}");
                }
            }
        }

        /// <summary>
        /// Call this whenever a major state change occurs (e.g., HP changes, new turn starts).
        /// This updates conditional passives like "Heroic Spirit."
        /// </summary>
        public void RefreshConditionalPassives()
        {
            foreach (var essence in equippedEssences)
            {
                if (essence == null) continue;

                foreach (var passive in essence.complexPassives)
                {
                    passive.Refresh(gameObject);
                }
            }
        }

        /// <summary>
        /// Fire <see cref="PassiveEffect.OnTurnStart"/> for every equipped passive.
        /// Called by the TurnManager (player) and EnemyController (enemy) at the
        /// boundary of each actor's turn.
        /// </summary>
        public void NotifyTurnStart()
        {
            foreach (var essence in equippedEssences)
            {
                if (essence == null) continue;

                foreach (var passive in essence.complexPassives)
                {
                    if (passive != null) passive.OnTurnStart(gameObject);
                }
            }

            gameObject.SendMessage("OnPlayerPhaseStart", SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// Returns true if the user has enough Soul Power for the ability in
        /// this slot. Lets callers (e.g., the input handler) gate UX such as
        /// "don't open the targeting reticle if the player can't afford it."
        /// </summary>
        public bool CanAfford(int slotIndex, int abilityIndex)
        {
            AbilityAction ability = GetAbility(slotIndex, abilityIndex);
            if (ability == null) return false;

            var stats = GetComponent<CharacterStats>();
            return stats != null && HumanClassAbilityResources.CanAfford(stats, ability);
        }

        /// <summary>
        /// Attempts to execute an untargeted ability from a specific essence slot.
        /// On success, deducts Soul Power and returns true.
        /// </summary>
        public bool TryExecuteAbility(int slotIndex, int abilityIndex)
        {
            return TryExecuteInternal(slotIndex, abilityIndex, useTarget: false, targetTile: default);
        }

        /// <summary>
        /// Attempts to execute a targeted ability from a specific essence slot.
        /// On success, deducts Soul Power and returns true.
        /// </summary>
        public bool TryExecuteAbility(int slotIndex, int abilityIndex, Vector3Int targetTile)
        {
            return TryExecuteInternal(slotIndex, abilityIndex, useTarget: true, targetTile);
        }

        private bool TryExecuteInternal(int slotIndex, int abilityIndex, bool useTarget, Vector3Int targetTile)
        {
            EssenceData essence = GetEssenceInSlot(slotIndex);
            if (essence == null || abilityIndex >= essence.activeAbilities.Count) return false;

            AbilityAction ability = essence.activeAbilities[abilityIndex];
            var stats = GetComponent<CharacterStats>();

            if (!JRogue.World.MapPresence.MistOfTheAbyssService.TryAllowEssenceActive(
                    gameObject,
                    out string mistDeny,
                    logDeny: true))
            {
                return false;
            }

            // 1. Resource check
            if (stats == null || !HumanClassAbilityResources.CanAfford(stats, ability))
            {
                Debug.Log(HumanClassAbilityResources.InsufficientResourceMessage(stats?.humanClass ?? HumanClass.None));
                return false;
            }

            // 2. Condition check (e.g. "must have a status effect")
            if (!ability.CanExecute(gameObject))
            {
                Debug.Log($"{ability.abilityName} conditions not met!");
                return false;
            }

            // 3. Execute, then deduct on success
            bool executed = useTarget
                ? ability.Execute(gameObject, targetTile)
                : ability.Execute(gameObject);

            if (executed)
            {
                HumanClassAbilityResources.TrySpend(stats, ability);
                return true;
            }
            return false;
        }

        static string BodyContributionKey(int slotIndex) => $"EssenceSlot:{slotIndex}";

        void RegisterEssenceBodyContribution(int slotIndex, EssenceData essence)
        {
            CharacterStats stats = GetComponent<CharacterStats>();
            if (stats == null || essence == null)
                return;
            stats.RegisterBodyEquipmentContribution(
                BodyContributionKey(slotIndex),
                essence.bodyCapabilityOrWhileEquipped,
                essence.bodyExclusionBypassMaskWhileEquipped);
        }

        void UnregisterEssenceBodyContribution(int slotIndex)
        {
            CharacterStats stats = GetComponent<CharacterStats>();
            stats?.UnregisterBodyEquipmentContribution(BodyContributionKey(slotIndex));
        }

        void EnsureEquippedArraySize()
        {
            int size = Mathf.Max(totalSlots, HumanClassRules.DefaultEssenceSlotCount);
            if (equippedEssences == null || equippedEssences.Length != size)
            {
                var next = new EssenceData[size];
                if (equippedEssences != null)
                {
                    int copy = Mathf.Min(equippedEssences.Length, next.Length);
                    for (int i = 0; i < copy; i++)
                        next[i] = equippedEssences[i];
                }

                equippedEssences = next;
            }
        }

        private void OnDestroy()
        {
            if (equippedEssences == null)
                return;

            for (int i = 0; i < equippedEssences.Length; i++)
                UnequipEssence(i);
        }
    }
}