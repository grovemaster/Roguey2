using System;
using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Hotbar;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.UI.Racial
{
    public enum HumanMageSpellLoadoutEditMode
    {
        Edit,
        ViewOnlyDungeon,
        ViewOnlyCombat,
    }

    public sealed class HumanMageSpellRowModel
    {
        public MageSpellDefinition Spell;
        public string SpellId = string.Empty;
        public string Title = string.Empty;
        public string Subtitle = string.Empty;
        public bool ShowPreparedBadge;
        public bool IsPrepared;
    }

    public sealed class HumanMageSpellDetailModel
    {
        public string SpellId = string.Empty;
        public string Title = string.Empty;
        public string Description = string.Empty;
        public string CostLine = string.Empty;
        public string AbilityLine = string.Empty;
        public string HotbarFootnote =
            "Assign prepared spells on the <b>ability hotbar</b> to cast in combat.";
        public bool ShowPrepareButton;
        public bool ShowUnprepareButton;
        public bool PrepareEnabled;
        public string PrepareDisabledReason = string.Empty;
        public bool ShowAddToHotbarButton;
        public bool AddToHotbarEnabled;
        public string AddToHotbarDisabledReason = string.Empty;
    }

    public sealed class HumanMageSpellBodyViewModel
    {
        public const string MissingRuntimeMessage =
            "Arcane spell data is missing for this character.";

        public const string NotMageMessage =
            "This character has not committed to the Mage path.";

        public const string PermanentClassMessage =
            "This character has committed to another path. Class commitment is permanent.";

        public const string EditModeBannerText =
            "Prepare arcane spells here. Learn new spells from <b>spellbooks</b> — buy them from the "
            + "<b>Arcane Vendor</b> after training with the <b>Mage Tutor</b>.";

        public const string ViewOnlyDungeonBannerText =
            "View only — you can only adjust prepared spells in town.";

        public const string ViewOnlyCombatBannerText =
            "View only — finish combat before adjusting prepared spells.";

        public const string BudgetFootnote =
            "Preparing spells spends capacity only. Casting spends current Magic Power.";

        public const string PreparedEmptyMessage =
            "No spells prepared. Select a known spell from your grimoire and prepare it.";

        public const string KnownEmptyMessage =
            "Your grimoire is empty. Complete <b>Arcane Apprenticeship</b> with the <b>Mage Tutor</b>, "
            + "then study <b>spellbooks</b> from the <b>Arcane Vendor</b>.";

        public HumanMageSpellLoadoutEditMode EditMode = HumanMageSpellLoadoutEditMode.ViewOnlyDungeon;
        public string BannerText = string.Empty;
        public string BudgetLine = string.Empty;
        public List<HumanMageSpellRowModel> PreparedRows = new();
        public List<HumanMageSpellRowModel> KnownRows = new();
        public string SelectedSpellId = string.Empty;
        public HumanMageSpellDetailModel Detail = new();

        public static HumanMageSpellBodyViewModel Build(
            BaseActor mage,
            string selectedSpellId = null)
        {
            var vm = new HumanMageSpellBodyViewModel();
            if (mage == null)
                return vm;

            HumanMageSpellsRuntime runtime = mage.GetComponent<HumanMageSpellsRuntime>();
            CharacterStats stats = mage.stats;
            if (runtime == null || stats == null)
                return vm;

            vm.EditMode = ResolveEditMode();
            vm.BannerText = ResolveBannerText(vm.EditMode);
            vm.BudgetLine = BuildBudgetLine(stats, runtime);

            HashSet<string> preparedIds = BuildPreparedIdSet(runtime);
            vm.PreparedRows = BuildRows(runtime.EquippedSpells, preparedIds, showPreparedBadge: false);
            vm.KnownRows = BuildRows(runtime.KnownSpells, preparedIds, showPreparedBadge: true);
            vm.SelectedSpellId = ResolveSelectedSpellId(
                selectedSpellId,
                vm.PreparedRows,
                vm.KnownRows);
            vm.Detail = BuildDetail(
                vm.SelectedSpellId,
                runtime,
                mage,
                vm.EditMode == HumanMageSpellLoadoutEditMode.Edit,
                preparedIds);
            return vm;
        }

        public static HumanMageSpellLoadoutEditMode ResolveEditMode()
        {
            if (SafeZonePolicyService.TryAllowHumanMageEquipChange(out _, logDeny: false))
                return HumanMageSpellLoadoutEditMode.Edit;

            if (Manager.Combat.CombatThreatCoordinator.Instance != null
                && Manager.Combat.CombatThreatCoordinator.Instance.IsInCombat)
            {
                return HumanMageSpellLoadoutEditMode.ViewOnlyCombat;
            }

            return HumanMageSpellLoadoutEditMode.ViewOnlyDungeon;
        }

        public static string ResolveBannerText(HumanMageSpellLoadoutEditMode mode) =>
            mode switch
            {
                HumanMageSpellLoadoutEditMode.Edit => EditModeBannerText,
                HumanMageSpellLoadoutEditMode.ViewOnlyCombat => ViewOnlyCombatBannerText,
                _ => ViewOnlyDungeonBannerText,
            };

        public static string ResolveSelectedSpellId(
            string requestedSpellId,
            IReadOnlyList<HumanMageSpellRowModel> preparedRows,
            IReadOnlyList<HumanMageSpellRowModel> knownRows)
        {
            if (!string.IsNullOrWhiteSpace(requestedSpellId))
            {
                string trimmed = requestedSpellId.Trim();
                if (ContainsSpellId(preparedRows, trimmed) || ContainsSpellId(knownRows, trimmed))
                    return trimmed;
            }

            if (preparedRows != null && preparedRows.Count > 0)
                return preparedRows[0].SpellId;

            if (knownRows != null && knownRows.Count > 0)
                return knownRows[0].SpellId;

            return string.Empty;
        }

        static bool ContainsSpellId(IReadOnlyList<HumanMageSpellRowModel> rows, string spellId)
        {
            if (rows == null || string.IsNullOrWhiteSpace(spellId))
                return false;

            for (int i = 0; i < rows.Count; i++)
            {
                if (string.Equals(rows[i]?.SpellId, spellId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        static string BuildBudgetLine(CharacterStats stats, HumanMageSpellsRuntime runtime)
        {
            int maxMagicPower = stats != null ? stats.MaxMagicPower : 0;
            int currentMagicPower = stats != null ? stats.currentMagicPower : 0;
            int preparedCost = SumEquipCost(runtime.EquippedSpells);
            int remaining = runtime != null ? runtime.RemainingEquipCapacity : 0;

            return
                $"MAGIC POWER · Max {maxMagicPower} · Prepared {preparedCost}/{maxMagicPower} · "
                + $"Free {remaining} · Current MP {currentMagicPower}";
        }

        static int SumEquipCost(IReadOnlyList<MageSpellDefinition> spells)
        {
            int total = 0;
            if (spells == null)
                return total;

            for (int i = 0; i < spells.Count; i++)
            {
                MageSpellDefinition spell = spells[i];
                if (spell != null)
                    total += spell.EquipCost;
            }

            return total;
        }

        static HashSet<string> BuildPreparedIdSet(HumanMageSpellsRuntime runtime)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (runtime?.EquippedSpells == null)
                return ids;

            for (int i = 0; i < runtime.EquippedSpells.Count; i++)
            {
                MageSpellDefinition spell = runtime.EquippedSpells[i];
                if (spell != null && !string.IsNullOrWhiteSpace(spell.spellId))
                    ids.Add(spell.spellId.Trim());
            }

            return ids;
        }

        static List<HumanMageSpellRowModel> BuildRows(
            IReadOnlyList<MageSpellDefinition> spells,
            HashSet<string> preparedIds,
            bool showPreparedBadge)
        {
            var sorted = SortSpells(spells);
            var rows = new List<HumanMageSpellRowModel>(sorted.Count);
            for (int i = 0; i < sorted.Count; i++)
            {
                MageSpellDefinition spell = sorted[i];
                string spellId = spell.spellId?.Trim() ?? string.Empty;
                bool isPrepared = preparedIds.Contains(spellId);
                rows.Add(new HumanMageSpellRowModel
                {
                    Spell = spell,
                    SpellId = spellId,
                    Title = ResolveTitle(spell),
                    Subtitle = BuildSubtitle(spell),
                    ShowPreparedBadge = showPreparedBadge && isPrepared,
                    IsPrepared = isPrepared,
                });
            }

            return rows;
        }

        public static List<MageSpellDefinition> SortSpells(IReadOnlyList<MageSpellDefinition> spells)
        {
            var sorted = new List<MageSpellDefinition>();
            if (spells == null)
                return sorted;

            for (int i = 0; i < spells.Count; i++)
            {
                if (spells[i] != null)
                    sorted.Add(spells[i]);
            }

            sorted.Sort(CompareSpells);
            return sorted;
        }

        public static int CompareSpells(MageSpellDefinition left, MageSpellDefinition right)
        {
            int titleCompare = string.Compare(
                ResolveTitle(left),
                ResolveTitle(right),
                StringComparison.OrdinalIgnoreCase);
            if (titleCompare != 0)
                return titleCompare;

            return string.Compare(
                left?.spellId,
                right?.spellId,
                StringComparison.OrdinalIgnoreCase);
        }

        static HumanMageSpellDetailModel BuildDetail(
            string spellId,
            HumanMageSpellsRuntime runtime,
            BaseActor actor,
            bool editMode,
            HashSet<string> preparedIds)
        {
            var detail = new HumanMageSpellDetailModel();
            if (string.IsNullOrWhiteSpace(spellId) || runtime == null)
                return detail;

            MageSpellDefinition spell = FindSpell(runtime.KnownSpells, spellId)
                ?? FindSpell(runtime.EquippedSpells, spellId);
            if (spell == null)
                return detail;

            detail.SpellId = spell.spellId?.Trim() ?? string.Empty;
            detail.Title = ResolveTitle(spell);
            detail.Description = string.IsNullOrWhiteSpace(spell.description)
                ? "—"
                : spell.description.Trim();
            detail.CostLine =
                $"Prepare cost: {spell.EquipCost} · Cast cost: {spell.magicPowerCost} Magic Power · Tier {spell.tier}";
            detail.AbilityLine = BuildAbilityLine(spell.ability);

            bool isPrepared = preparedIds.Contains(detail.SpellId);
            if (!editMode)
                return detail;

            if (isPrepared)
            {
                detail.ShowUnprepareButton = true;
                PopulateHotbarAddState(detail, runtime, actor, detail.SpellId);
                return detail;
            }

            detail.ShowPrepareButton = true;
            if (runtime.RemainingEquipCapacity >= spell.EquipCost)
            {
                detail.PrepareEnabled = true;
                return detail;
            }

            detail.PrepareEnabled = false;
            detail.PrepareDisabledReason =
                $"Need {spell.EquipCost} free capacity; have {runtime.RemainingEquipCapacity}.";
            return detail;
        }

        static void PopulateHotbarAddState(
            HumanMageSpellDetailModel detail,
            HumanMageSpellsRuntime runtime,
            BaseActor actor,
            string spellId)
        {
            detail.ShowAddToHotbarButton = true;
            if (actor == null)
            {
                detail.AddToHotbarEnabled = false;
                detail.AddToHotbarDisabledReason = "No actor.";
                return;
            }

            if (!TryResolveEquippedIndex(runtime, spellId, out int equippedIndex))
            {
                detail.AddToHotbarEnabled = false;
                detail.AddToHotbarDisabledReason = "Spell is not prepared.";
                return;
            }

            HotbarLayout layout = actor.GetComponent<HotbarLayout>();
            if (layout == null)
            {
                detail.AddToHotbarEnabled = true;
                return;
            }

            if (IsEquippedIndexOnHotbar(layout, equippedIndex))
            {
                detail.AddToHotbarEnabled = false;
                detail.AddToHotbarDisabledReason = "Already on hotbar.";
                return;
            }

            if (HasEmptyMainHotbarSlot(layout))
            {
                detail.AddToHotbarEnabled = true;
                return;
            }

            detail.AddToHotbarEnabled = false;
            detail.AddToHotbarDisabledReason = "Hotbar full — open ability hotbar to rearrange.";
        }

        static bool TryResolveEquippedIndex(HumanMageSpellsRuntime runtime, string spellId, out int equippedIndex)
        {
            equippedIndex = -1;
            if (runtime?.EquippedSpells == null || string.IsNullOrWhiteSpace(spellId))
                return false;

            for (int i = 0; i < runtime.EquippedSpells.Count; i++)
            {
                MageSpellDefinition spell = runtime.EquippedSpells[i];
                if (spell != null
                    && string.Equals(spell.spellId, spellId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    equippedIndex = i;
                    return true;
                }
            }

            return false;
        }

        static bool IsEquippedIndexOnHotbar(HotbarLayout layout, int equippedIndex)
        {
            for (int slot = 0; slot < HotbarLayout.HotbarMainSlotCount; slot++)
            {
                HotbarEntry entry = layout.GetSlot(slot);
                if (entry.Kind == HotbarEntryKind.HumanMageSpell && entry.abilityIndex == equippedIndex)
                    return true;
            }

            return false;
        }

        static bool HasEmptyMainHotbarSlot(HotbarLayout layout)
        {
            for (int slot = 0; slot < HotbarLayout.HotbarMainSlotCount; slot++)
            {
                if (layout.GetSlot(slot).IsEmpty())
                    return true;
            }

            return false;
        }

        static MageSpellDefinition FindSpell(IReadOnlyList<MageSpellDefinition> spells, string spellId)
        {
            if (spells == null || string.IsNullOrWhiteSpace(spellId))
                return null;

            for (int i = 0; i < spells.Count; i++)
            {
                MageSpellDefinition spell = spells[i];
                if (spell != null
                    && string.Equals(spell.spellId, spellId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return spell;
                }
            }

            return null;
        }

        public static string ResolveTitle(MageSpellDefinition spell)
        {
            if (spell == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(spell.displayName))
                return spell.displayName.Trim();

            return string.IsNullOrWhiteSpace(spell.spellId) ? "Spell" : spell.spellId.Trim();
        }

        static string BuildSubtitle(MageSpellDefinition spell) =>
            $"Prepare {spell.EquipCost} MP · Cast {spell.magicPowerCost} MP";

        static string BuildAbilityLine(AbilityAction ability)
        {
            if (ability == null || string.IsNullOrWhiteSpace(ability.abilityName))
                return string.Empty;

            return ability.abilityName.Trim();
        }
    }
}
