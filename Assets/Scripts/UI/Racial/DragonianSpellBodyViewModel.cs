using System;
using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.UI.Racial
{
    public enum DragonianSpellLoadoutEditMode
    {
        Edit,
        ViewOnlyDungeon,
        ViewOnlyCombat,
    }

    public sealed class DragonianSpellRowModel
    {
        public DragonianSpellDefinition Spell;
        public string SpellId = string.Empty;
        public string Title = string.Empty;
        public string Subtitle = string.Empty;
        public bool ShowEquippedBadge;
        public bool IsMemorized;
    }

    public sealed class DragonianSpellDetailModel
    {
        public string SpellId = string.Empty;
        public string Title = string.Empty;
        public string Description = string.Empty;
        public string CostLine = string.Empty;
        public string AbilityLine = string.Empty;
        public string HotbarFootnote =
            "Assign equipped word-forms on the <b>ability hotbar</b> to cast in combat.";
        public bool ShowEquipButton;
        public bool ShowUnequipButton;
        public bool EquipEnabled;
        public string EquipDisabledReason = string.Empty;
    }

    public sealed class DragonianSpellBodyViewModel
    {
        public const string CannotUseWordFormsMessage =
            "This character cannot use draconic word-forms.";

        public const string EditModeBannerText =
            "Adjust equipped word-forms here. Learn new word-forms from Dragonian Elders in town.";

        public const string ViewOnlyDungeonBannerText =
            "View only — you can only adjust equipped word-forms in town.";

        public const string ViewOnlyCombatBannerText =
            "View only — finish combat before adjusting equipped word-forms.";

        public const string BudgetFootnote =
            "Memorizing spends capacity only. Casting spends current Soul Power.";

        public const string MemorizedEmptyMessage =
            "No word-forms equipped. Select a learned spell and equip it.";

        public const string KnownEmptyMessage =
            "No word-forms learned yet. Complete trials with a Dragonian Elder in town.";

        public DragonianSpellLoadoutEditMode EditMode = DragonianSpellLoadoutEditMode.ViewOnlyDungeon;
        public string BannerText = string.Empty;
        public string BudgetLine = string.Empty;
        public List<DragonianSpellRowModel> MemorizedRows = new();
        public List<DragonianSpellRowModel> KnownRows = new();
        public string SelectedSpellId = string.Empty;
        public DragonianSpellDetailModel Detail = new();

        public static DragonianSpellBodyViewModel Build(
            BaseActor dragonian,
            string selectedSpellId = null)
        {
            var vm = new DragonianSpellBodyViewModel();
            if (dragonian == null)
                return vm;

            DragonianSpellsRuntime runtime = dragonian.GetComponent<DragonianSpellsRuntime>();
            CharacterStats stats = dragonian.stats;
            if (runtime == null || stats == null)
                return vm;

            vm.EditMode = ResolveEditMode();
            vm.BannerText = ResolveBannerText(vm.EditMode);
            vm.BudgetLine = BuildBudgetLine(stats, runtime);

            HashSet<string> memorizedIds = BuildMemorizedIdSet(runtime);
            vm.MemorizedRows = BuildRows(runtime.MemorizedSpells, memorizedIds, showEquippedBadge: false);
            vm.KnownRows = BuildRows(runtime.KnownSpells, memorizedIds, showEquippedBadge: true);
            vm.SelectedSpellId = ResolveSelectedSpellId(
                selectedSpellId,
                vm.MemorizedRows,
                vm.KnownRows);
            vm.Detail = BuildDetail(
                vm.SelectedSpellId,
                runtime,
                vm.EditMode == DragonianSpellLoadoutEditMode.Edit,
                memorizedIds);
            return vm;
        }

        public static DragonianSpellLoadoutEditMode ResolveEditMode()
        {
            if (SafeZonePolicyService.TryAllowDragonianMemorizeChange(out _, logDeny: false))
                return DragonianSpellLoadoutEditMode.Edit;

            if (Manager.Combat.CombatThreatCoordinator.Instance != null
                && Manager.Combat.CombatThreatCoordinator.Instance.IsInCombat)
            {
                return DragonianSpellLoadoutEditMode.ViewOnlyCombat;
            }

            return DragonianSpellLoadoutEditMode.ViewOnlyDungeon;
        }

        public static string ResolveBannerText(DragonianSpellLoadoutEditMode mode) =>
            mode switch
            {
                DragonianSpellLoadoutEditMode.Edit => EditModeBannerText,
                DragonianSpellLoadoutEditMode.ViewOnlyCombat => ViewOnlyCombatBannerText,
                _ => ViewOnlyDungeonBannerText,
            };

        public static string ResolveSelectedSpellId(
            string requestedSpellId,
            IReadOnlyList<DragonianSpellRowModel> memorizedRows,
            IReadOnlyList<DragonianSpellRowModel> knownRows)
        {
            if (!string.IsNullOrWhiteSpace(requestedSpellId))
            {
                string trimmed = requestedSpellId.Trim();
                if (ContainsSpellId(memorizedRows, trimmed) || ContainsSpellId(knownRows, trimmed))
                    return trimmed;
            }

            if (memorizedRows != null && memorizedRows.Count > 0)
                return memorizedRows[0].SpellId;

            if (knownRows != null && knownRows.Count > 0)
                return knownRows[0].SpellId;

            return string.Empty;
        }

        static bool ContainsSpellId(IReadOnlyList<DragonianSpellRowModel> rows, string spellId)
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

        static string BuildBudgetLine(CharacterStats stats, DragonianSpellsRuntime runtime)
        {
            int maxSoulPower = stats != null ? stats.MaxSoulPower : 0;
            int currentSoulPower = stats != null ? stats.currentSoulPower : 0;
            int equippedCost = SumMemorizeCost(runtime.MemorizedSpells);
            int remaining = runtime != null ? runtime.RemainingMemoryCapacity : 0;

            return
                $"SOUL POWER · Max {maxSoulPower} · Equipped {equippedCost}/{maxSoulPower} · "
                + $"Free {remaining} · Current SP {currentSoulPower}";
        }

        static int SumMemorizeCost(IReadOnlyList<DragonianSpellDefinition> spells)
        {
            int total = 0;
            if (spells == null)
                return total;

            for (int i = 0; i < spells.Count; i++)
            {
                DragonianSpellDefinition spell = spells[i];
                if (spell != null)
                    total += spell.memorizeCost;
            }

            return total;
        }

        static HashSet<string> BuildMemorizedIdSet(DragonianSpellsRuntime runtime)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (runtime?.MemorizedSpells == null)
                return ids;

            for (int i = 0; i < runtime.MemorizedSpells.Count; i++)
            {
                DragonianSpellDefinition spell = runtime.MemorizedSpells[i];
                if (spell != null && !string.IsNullOrWhiteSpace(spell.spellId))
                    ids.Add(spell.spellId.Trim());
            }

            return ids;
        }

        static List<DragonianSpellRowModel> BuildRows(
            IReadOnlyList<DragonianSpellDefinition> spells,
            HashSet<string> memorizedIds,
            bool showEquippedBadge)
        {
            var sorted = SortSpells(spells);
            var rows = new List<DragonianSpellRowModel>(sorted.Count);
            for (int i = 0; i < sorted.Count; i++)
            {
                DragonianSpellDefinition spell = sorted[i];
                string spellId = spell.spellId?.Trim() ?? string.Empty;
                bool isMemorized = memorizedIds.Contains(spellId);
                rows.Add(new DragonianSpellRowModel
                {
                    Spell = spell,
                    SpellId = spellId,
                    Title = ResolveTitle(spell),
                    Subtitle = BuildSubtitle(spell),
                    ShowEquippedBadge = showEquippedBadge && isMemorized,
                    IsMemorized = isMemorized,
                });
            }

            return rows;
        }

        public static List<DragonianSpellDefinition> SortSpells(IReadOnlyList<DragonianSpellDefinition> spells)
        {
            var sorted = new List<DragonianSpellDefinition>();
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

        public static int CompareSpells(DragonianSpellDefinition left, DragonianSpellDefinition right)
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

        static DragonianSpellDetailModel BuildDetail(
            string spellId,
            DragonianSpellsRuntime runtime,
            bool editMode,
            HashSet<string> memorizedIds)
        {
            var detail = new DragonianSpellDetailModel();
            if (string.IsNullOrWhiteSpace(spellId) || runtime == null)
                return detail;

            DragonianSpellDefinition spell = FindSpell(runtime.KnownSpells, spellId)
                ?? FindSpell(runtime.MemorizedSpells, spellId);
            if (spell == null)
                return detail;

            detail.SpellId = spell.spellId?.Trim() ?? string.Empty;
            detail.Title = ResolveTitle(spell);
            detail.Description = string.IsNullOrWhiteSpace(spell.description)
                ? "—"
                : spell.description.Trim();
            detail.CostLine =
                $"Memorize cost: {spell.memorizeCost} · Cast cost: {spell.soulPowerCastCost} Soul Power";
            detail.AbilityLine = BuildAbilityLine(spell.ability);

            bool isMemorized = memorizedIds.Contains(detail.SpellId);
            if (!editMode)
                return detail;

            if (isMemorized)
            {
                detail.ShowUnequipButton = true;
                return detail;
            }

            detail.ShowEquipButton = true;
            if (runtime.RemainingMemoryCapacity >= spell.memorizeCost)
            {
                detail.EquipEnabled = true;
                return detail;
            }

            detail.EquipEnabled = false;
            detail.EquipDisabledReason =
                $"Need {spell.memorizeCost} free capacity; have {runtime.RemainingMemoryCapacity}.";
            return detail;
        }

        static DragonianSpellDefinition FindSpell(
            IReadOnlyList<DragonianSpellDefinition> spells,
            string spellId)
        {
            if (spells == null || string.IsNullOrWhiteSpace(spellId))
                return null;

            for (int i = 0; i < spells.Count; i++)
            {
                DragonianSpellDefinition spell = spells[i];
                if (spell != null
                    && string.Equals(spell.spellId, spellId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return spell;
                }
            }

            return null;
        }

        public static string ResolveTitle(DragonianSpellDefinition spell)
        {
            if (spell == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(spell.displayName))
                return spell.displayName.Trim();

            return string.IsNullOrWhiteSpace(spell.spellId) ? "Spell" : spell.spellId.Trim();
        }

        static string BuildSubtitle(DragonianSpellDefinition spell) =>
            $"Memorize {spell.memorizeCost} SP · Cast {spell.soulPowerCastCost} SP";

        static string BuildAbilityLine(AbilityAction ability)
        {
            if (ability == null || string.IsNullOrWhiteSpace(ability.abilityName))
                return string.Empty;

            return ability.abilityName.Trim();
        }
    }
}
