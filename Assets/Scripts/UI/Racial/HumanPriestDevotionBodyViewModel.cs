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
    public enum HumanPriestDevotionEditMode
    {
        Edit,
        ViewOnlyDungeon,
        ViewOnlyCombat,
    }

    public sealed class HumanPriestDevotionRowModel
    {
        public PriestInvocationDefinition Invocation;
        public string InvocationId = string.Empty;
        public string Title = string.Empty;
        public string Subtitle = string.Empty;
        public bool IsPrepared;
        public bool IsLocked;
        public bool ShowPreparedBadge;
    }

    public sealed class HumanPriestConductRowModel
    {
        public string Message = string.Empty;
        public int PietyDelta;
    }

    public sealed class HumanPriestDevotionDetailModel
    {
        public string InvocationId = string.Empty;
        public string Title = string.Empty;
        public string Description = string.Empty;
        public string CostLine = string.Empty;
        public bool ShowPrepareButton;
        public bool ShowUnprepareButton;
        public bool PrepareEnabled;
        public string PrepareDisabledReason = string.Empty;
        public bool ShowAddToHotbarButton;
        public bool AddToHotbarEnabled;
        public string AddToHotbarDisabledReason = string.Empty;
        public string HotbarFootnote =
            "Assign prepared devotions on the <b>ability hotbar</b> to invoke in combat.";
    }

    public sealed class HumanPriestDevotionBodyViewModel
    {
        public const string MissingRuntimeMessage =
            "Covenant data is missing for this character.";

        public const string EditModeBannerText =
            "Choose which invocations to prepare for your devotion loadout.";

        public const string ViewOnlyDungeonBannerText =
            "View only — adjust prepared devotions at the Argent Vigil shrine in town.";

        public const string ViewOnlyCombatBannerText =
            "View only — finish combat before adjusting devotions.";

        public const string PreparedEmptyMessage =
            "No devotions prepared. Select an unlocked invocation and prepare it.";

        public const string LibraryEmptyMessage =
            "No invocations unlocked for your patron yet.";

        public HumanPriestDevotionEditMode EditMode = HumanPriestDevotionEditMode.ViewOnlyDungeon;
        public string BannerText = string.Empty;
        public string StatusLine = string.Empty;
        public string PenanceLine = string.Empty;
        public List<HumanPriestDevotionRowModel> PreparedRows = new();
        public List<HumanPriestDevotionRowModel> LibraryRows = new();
        public List<HumanPriestConductRowModel> ConductRows = new();
        public string SelectedInvocationId = string.Empty;
        public HumanPriestDevotionDetailModel Detail = new();

        public static HumanPriestDevotionBodyViewModel Build(
            BaseActor priest,
            string selectedInvocationId = null)
        {
            var vm = new HumanPriestDevotionBodyViewModel();
            if (priest == null)
                return vm;

            HumanPriestCovenantRuntime covenant = priest.GetComponent<HumanPriestCovenantRuntime>();
            HumanPriestDevotionRuntime devotion = priest.GetComponent<HumanPriestDevotionRuntime>();
            CharacterStats stats = priest.stats;
            if (covenant == null || devotion == null || stats == null)
                return vm;

            vm.EditMode = ResolveEditMode();
            vm.BannerText = ResolveBannerText(vm.EditMode);
            vm.StatusLine = BuildStatusLine(stats, covenant, devotion);
            vm.PenanceLine = BuildPenanceLine(covenant);
            vm.ConductRows = BuildConductRows(covenant);

            HashSet<string> preparedIds = BuildPreparedIdSet(devotion);
            vm.PreparedRows = BuildRows(
                devotion.EquippedInvocations,
                stats,
                covenant,
                preparedIds,
                showPreparedBadge: false);
            vm.LibraryRows = BuildLibraryRows(stats, covenant, devotion, preparedIds);
            vm.SelectedInvocationId = ResolveSelectedInvocationId(
                selectedInvocationId,
                vm.PreparedRows,
                vm.LibraryRows);
            vm.Detail = BuildDetail(
                vm.SelectedInvocationId,
                devotion,
                priest,
                stats,
                covenant,
                vm.EditMode == HumanPriestDevotionEditMode.Edit,
                preparedIds);
            return vm;
        }

        public static HumanPriestDevotionEditMode ResolveEditMode()
        {
            if (SafeZonePolicyService.TryAllowHumanPriestDevotionChange(out _, logDeny: false))
                return HumanPriestDevotionEditMode.Edit;

            if (Manager.Combat.CombatThreatCoordinator.Instance != null
                && Manager.Combat.CombatThreatCoordinator.Instance.IsInCombat)
            {
                return HumanPriestDevotionEditMode.ViewOnlyCombat;
            }

            return HumanPriestDevotionEditMode.ViewOnlyDungeon;
        }

        public static string ResolveBannerText(HumanPriestDevotionEditMode mode) =>
            mode switch
            {
                HumanPriestDevotionEditMode.Edit => EditModeBannerText,
                HumanPriestDevotionEditMode.ViewOnlyCombat => ViewOnlyCombatBannerText,
                _ => ViewOnlyDungeonBannerText,
            };

        static string BuildStatusLine(
            CharacterStats stats,
            HumanPriestCovenantRuntime covenant,
            HumanPriestDevotionRuntime devotion)
        {
            PriestPietyBandData band = PriestPietyLogic.ResolveCurrentBand(covenant);
            string stars = band != null ? band.starLabel : "★☆☆☆☆";
            int cap = devotion.GetDevotionSlotCap();
            int prepared = devotion.EquippedInvocations.Count;
            int maxDp = stats.MaxDivinePower;
            int currentDp = stats.currentDivinePower;

            return
                $"PIETY {covenant.Piety}/{PriestPietyLogic.ResolveMaxPiety()} {stars} · "
                + $"Prepared {prepared}/{cap} · Divine Power {currentDp}/{maxDp}";
        }

        static string BuildPenanceLine(HumanPriestCovenantRuntime covenant)
        {
            if (covenant == null || covenant.PenanceDebt <= 0)
                return string.Empty;

            return $"Penance debt: {covenant.PenanceDebt} — repent at the shrine before high-tier invocations.";
        }

        static List<HumanPriestConductRowModel> BuildConductRows(HumanPriestCovenantRuntime covenant)
        {
            var rows = new List<HumanPriestConductRowModel>();
            if (covenant?.RecentConduct == null)
                return rows;

            for (int i = 0; i < covenant.RecentConduct.Count; i++)
            {
                DivineConductLogEntry entry = covenant.RecentConduct[i];
                if (entry == null)
                    continue;

                rows.Add(new HumanPriestConductRowModel
                {
                    Message = entry.message ?? string.Empty,
                    PietyDelta = entry.pietyDelta,
                });
            }

            return rows;
        }

        static List<HumanPriestDevotionRowModel> BuildLibraryRows(
            CharacterStats stats,
            HumanPriestCovenantRuntime covenant,
            HumanPriestDevotionRuntime devotion,
            HashSet<string> preparedIds)
        {
            var rows = new List<HumanPriestDevotionRowModel>();
            if (!PatronGodCatalogService.TryGetGod(covenant.PatronGodId, out PatronGodDefinition god)
                || god.invocationIds == null)
            {
                return rows;
            }

            for (int i = 0; i < god.invocationIds.Count; i++)
            {
                if (!PriestInvocationCatalogService.TryGetInvocation(god.invocationIds[i], out PriestInvocationDefinition invocation)
                    || invocation == null)
                {
                    continue;
                }

                bool unlocked = PriestPietyLogic.IsInvocationUnlocked(stats, covenant, invocation);
                rows.Add(new HumanPriestDevotionRowModel
                {
                    Invocation = invocation,
                    InvocationId = invocation.invocationId,
                    Title = invocation.displayName,
                    Subtitle = unlocked
                        ? BuildInvocationSubtitle(invocation)
                        : PriestPietyLogic.BuildLockedReason(stats, covenant, invocation),
                    IsPrepared = preparedIds.Contains(invocation.invocationId),
                    IsLocked = !unlocked,
                    ShowPreparedBadge = true,
                });
            }

            rows.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.Ordinal));
            return rows;
        }

        static List<HumanPriestDevotionRowModel> BuildRows(
            IReadOnlyList<PriestInvocationDefinition> invocations,
            CharacterStats stats,
            HumanPriestCovenantRuntime covenant,
            HashSet<string> preparedIds,
            bool showPreparedBadge)
        {
            var rows = new List<HumanPriestDevotionRowModel>();
            if (invocations == null)
                return rows;

            for (int i = 0; i < invocations.Count; i++)
            {
                PriestInvocationDefinition invocation = invocations[i];
                if (invocation == null)
                    continue;

                bool unlocked = PriestPietyLogic.IsInvocationUnlocked(stats, covenant, invocation);
                rows.Add(new HumanPriestDevotionRowModel
                {
                    Invocation = invocation,
                    InvocationId = invocation.invocationId,
                    Title = invocation.displayName,
                    Subtitle = BuildInvocationSubtitle(invocation),
                    IsPrepared = preparedIds.Contains(invocation.invocationId),
                    IsLocked = !unlocked,
                    ShowPreparedBadge = showPreparedBadge,
                });
            }

            return rows;
        }

        static string BuildInvocationSubtitle(PriestInvocationDefinition invocation)
        {
            if (invocation == null)
                return string.Empty;

            return $"DP {invocation.divinePowerCost}"
                + (invocation.pietyInvokeCost > 0 ? $" · Piety {invocation.pietyInvokeCost}" : string.Empty);
        }

        static HashSet<string> BuildPreparedIdSet(HumanPriestDevotionRuntime devotion)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IReadOnlyList<PriestInvocationDefinition> equipped = devotion?.EquippedInvocations;
            if (equipped == null)
                return ids;

            for (int i = 0; i < equipped.Count; i++)
            {
                PriestInvocationDefinition invocation = equipped[i];
                if (invocation != null && !string.IsNullOrWhiteSpace(invocation.invocationId))
                    ids.Add(invocation.invocationId.Trim());
            }

            return ids;
        }

        static string ResolveSelectedInvocationId(
            string requestedId,
            IReadOnlyList<HumanPriestDevotionRowModel> preparedRows,
            IReadOnlyList<HumanPriestDevotionRowModel> libraryRows)
        {
            if (!string.IsNullOrWhiteSpace(requestedId))
            {
                string trimmed = requestedId.Trim();
                if (ContainsInvocationId(preparedRows, trimmed) || ContainsInvocationId(libraryRows, trimmed))
                    return trimmed;
            }

            if (preparedRows != null && preparedRows.Count > 0)
                return preparedRows[0].InvocationId;

            if (libraryRows != null && libraryRows.Count > 0)
                return libraryRows[0].InvocationId;

            return string.Empty;
        }

        static bool ContainsInvocationId(IReadOnlyList<HumanPriestDevotionRowModel> rows, string id)
        {
            if (rows == null || string.IsNullOrWhiteSpace(id))
                return false;

            for (int i = 0; i < rows.Count; i++)
            {
                if (string.Equals(rows[i]?.InvocationId, id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        static HumanPriestDevotionDetailModel BuildDetail(
            string invocationId,
            HumanPriestDevotionRuntime devotion,
            BaseActor actor,
            CharacterStats stats,
            HumanPriestCovenantRuntime covenant,
            bool editMode,
            HashSet<string> preparedIds)
        {
            var detail = new HumanPriestDevotionDetailModel { InvocationId = invocationId ?? string.Empty };
            if (string.IsNullOrWhiteSpace(invocationId)
                || !PriestInvocationCatalogService.TryGetInvocation(invocationId, out PriestInvocationDefinition invocation)
                || invocation == null)
            {
                return detail;
            }

            detail.Title = invocation.displayName;
            detail.Description = invocation.description ?? string.Empty;
            detail.CostLine =
                $"Divine Power {invocation.divinePowerCost}"
                + (invocation.pietyInvokeCost > 0 ? $" · Piety invoke {invocation.pietyInvokeCost}" : string.Empty)
                + $" · Requires piety {invocation.requiredPiety}";

            bool isPrepared = preparedIds.Contains(invocation.invocationId);
            bool unlocked = PriestPietyLogic.IsInvocationUnlocked(stats, covenant, invocation);
            if (!editMode || !unlocked)
                return detail;

            if (isPrepared)
            {
                detail.ShowUnprepareButton = true;
                PopulateHotbarAddState(detail, devotion, actor, invocation.invocationId);
                return detail;
            }

            detail.ShowPrepareButton = true;
            if (devotion.EquippedInvocations.Count < devotion.GetDevotionSlotCap())
            {
                detail.PrepareEnabled = true;
                return detail;
            }

            detail.PrepareEnabled = false;
            detail.PrepareDisabledReason =
                $"Devotion slots full ({devotion.EquippedInvocations.Count}/{devotion.GetDevotionSlotCap()}).";
            return detail;
        }

        static void PopulateHotbarAddState(
            HumanPriestDevotionDetailModel detail,
            HumanPriestDevotionRuntime devotion,
            BaseActor actor,
            string invocationId)
        {
            detail.ShowAddToHotbarButton = true;
            if (actor == null || devotion == null)
            {
                detail.AddToHotbarEnabled = false;
                detail.AddToHotbarDisabledReason = "No actor.";
                return;
            }

            if (!TryResolveEquippedIndex(devotion, invocationId, out int equippedIndex))
            {
                detail.AddToHotbarEnabled = false;
                detail.AddToHotbarDisabledReason = "Invocation is not prepared.";
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

            detail.AddToHotbarEnabled = HasEmptyMainHotbarSlot(layout)
                ? true
                : false;
            if (!detail.AddToHotbarEnabled)
                detail.AddToHotbarDisabledReason = "Hotbar full — open ability hotbar to rearrange.";
        }

        static bool TryResolveEquippedIndex(
            HumanPriestDevotionRuntime devotion,
            string invocationId,
            out int equippedIndex)
        {
            equippedIndex = -1;
            if (devotion?.EquippedInvocations == null || string.IsNullOrWhiteSpace(invocationId))
                return false;

            for (int i = 0; i < devotion.EquippedInvocations.Count; i++)
            {
                PriestInvocationDefinition invocation = devotion.EquippedInvocations[i];
                if (invocation != null
                    && string.Equals(invocation.invocationId, invocationId, StringComparison.OrdinalIgnoreCase))
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
                if (entry.Kind == HotbarEntryKind.HumanPriestInvocation
                    && entry.abilityIndex == equippedIndex)
                {
                    return true;
                }
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
    }
}
