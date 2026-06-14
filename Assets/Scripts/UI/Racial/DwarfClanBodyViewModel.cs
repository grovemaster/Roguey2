using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.UI.Racial
{
    public sealed class DwarfCommonSlotRowModel
    {
        public int SlotIndex;
        public string Title = string.Empty;
        public string Subtitle = string.Empty;
        public string Description = string.Empty;
        public bool IsEmpty;
    }

    public sealed class DwarfClanBodyViewModel
    {
        public const string CannotDisplayMessage =
            "This character cannot walk a dwarf ancestor path.";

        public const string UnaffiliatedBanner =
            "Swear allegiance at a clan hall to walk your patron's path.";

        public const string UnaffiliatedBody =
            "Visit a clan steward at a dwarf clan hall in town\n" +
            "to swear allegiance and begin your patron ancestor's path.";

        public const string MemberBannerTemplate =
            "View only — learn new clan techniques at the Hall of Ancestors altar in {0}.";

        public const string ClanGhostLearnHint =
            "Learn new techniques at the Hall of Ancestors altar.";

        public const string CommonAbilitiesFootnote =
            "Assign unlocked actives on the ability hotbar to use them in combat.";

        public const string EmptyCommonSlotTitle = "Empty slot";

        public string BannerText = string.Empty;
        public string SummaryLine = string.Empty;
        public string PatronLine = string.Empty;
        public string UnaffiliatedMessage = string.Empty;
        public bool IsUnaffiliated;
        public bool CanDisplay;
        public List<SpiritImprintCardViewModel> ClanCards = new();
        public List<DwarfCommonSlotRowModel> CommonSlots = new();

        public static string FormatMemberBanner(string clanShortName)
        {
            string label = string.IsNullOrWhiteSpace(clanShortName) ? "your clan hall" : clanShortName.Trim();
            return string.Format(MemberBannerTemplate, label);
        }

        public static DwarfClanBodyViewModel Build(BaseActor member)
        {
            var vm = new DwarfClanBodyViewModel();
            if (member?.stats == null ||
                member.stats.race != Race.Dwarf ||
                member.stats.racialSubsystem != RacialSubsystemKind.DwarfAncestry)
            {
                return vm;
            }

            vm.CanDisplay = true;
            vm.CommonSlots = BuildCommonSlots(member);

            DwarfClanMembershipRuntime membership = member.GetComponent<DwarfClanMembershipRuntime>();
            if (membership == null || !membership.IsAffiliated)
            {
                vm.IsUnaffiliated = true;
                vm.BannerText = UnaffiliatedBanner;
                vm.UnaffiliatedMessage = UnaffiliatedBody;
                return vm;
            }

            DwarfClanDefinition clan = TryLoadClan(membership.ClanId);
            DwarfAncestorPathRuntime pathRuntime = member.GetComponent<DwarfAncestorPathRuntime>();
            int prestige = ResolveClanPrestige(clan, membership.ClanId);

            vm.BannerText = FormatMemberBanner(clan?.shortName ?? membership.ClanId);
            vm.SummaryLine = BuildSummaryLine(clan, membership, prestige);
            vm.PatronLine = BuildPatronLine(clan, pathRuntime);

            SpiritImprintGraph graph = ResolveAbilityTree(clan, pathRuntime);
            if (graph != null)
            {
                vm.ClanCards = BarbarianSpiritImprintViewModel.BuildCardsFromLearnedSet(
                    graph,
                    pathRuntime?.ChosenPathNodeIds,
                    ClanGhostLearnHint);
            }

            return vm;
        }

        static List<DwarfCommonSlotRowModel> BuildCommonSlots(BaseActor member)
        {
            var rows = new List<DwarfCommonSlotRowModel>(DwarfCommonAbilitiesRuntime.SlotCount);
            var installed = new Dictionary<int, DwarfCommonAbilityDefinition>();

            DwarfCommonAbilitiesRuntime runtime = member.GetComponent<DwarfCommonAbilitiesRuntime>();
            if (runtime?.InstalledSnapshot != null)
            {
                foreach (KeyValuePair<int, DwarfCommonAbilityDefinition> entry in runtime.InstalledSnapshot)
                    installed[entry.Key] = entry.Value;
            }

            for (int slot = 0; slot < DwarfCommonAbilitiesRuntime.SlotCount; slot++)
            {
                if (installed.TryGetValue(slot, out DwarfCommonAbilityDefinition ability) && ability != null)
                {
                    rows.Add(new DwarfCommonSlotRowModel
                    {
                        SlotIndex = slot,
                        Title = ResolveAbilityTitle(ability),
                        Subtitle = $"Slot {slot + 1} · ACTIVE",
                        Description = string.IsNullOrWhiteSpace(ability.description)
                            ? "—"
                            : ability.description.Trim(),
                        IsEmpty = false,
                    });
                    continue;
                }

                rows.Add(new DwarfCommonSlotRowModel
                {
                    SlotIndex = slot,
                    Title = EmptyCommonSlotTitle,
                    Subtitle = $"Slot {slot + 1} · LOCKED",
                    Description = "Unlocked by character level (coming soon).",
                    IsEmpty = true,
                });
            }

            return rows;
        }

        static string BuildSummaryLine(
            DwarfClanDefinition clan,
            DwarfClanMembershipRuntime membership,
            int prestige)
        {
            string clanName = clan?.displayName ?? membership.ClanId;
            return $"Clan · {clanName} · Rank {membership.ClanMemberRank} · Prestige {prestige}";
        }

        static string BuildPatronLine(DwarfClanDefinition clan, DwarfAncestorPathRuntime pathRuntime)
        {
            AncestorDefinition patron = clan?.patronAncestor ?? pathRuntime?.PatronAncestor;
            if (patron == null)
                return string.Empty;

            string name = string.IsNullOrWhiteSpace(patron.displayName)
                ? patron.ancestorId
                : patron.displayName.Trim();
            return $"Patron · {name}";
        }

        static SpiritImprintGraph ResolveAbilityTree(DwarfClanDefinition clan, DwarfAncestorPathRuntime pathRuntime)
        {
            if (clan?.patronAncestor?.abilityTree != null)
                return clan.patronAncestor.abilityTree;

            return pathRuntime?.PatronAncestor?.abilityTree;
        }

        static int ResolveClanPrestige(DwarfClanDefinition clan, string clanId)
        {
            if (string.IsNullOrWhiteSpace(clanId))
                return 0;

            int prestige = DwarfClanWorldState.Instance?.GetPrestige(clanId.Trim()) ?? 0;
            if (prestige > 0)
                return prestige;

            return clan?.startingPrestige ?? 0;
        }

        static string ResolveAbilityTitle(DwarfCommonAbilityDefinition ability)
        {
            if (ability == null)
                return EmptyCommonSlotTitle;

            return string.IsNullOrWhiteSpace(ability.displayName)
                ? ability.abilityId
                : ability.displayName.Trim();
        }

        public static DwarfClanDefinition TryLoadClan(string clanId)
        {
            if (string.IsNullOrWhiteSpace(clanId))
                return null;

            string trimmed = clanId.Trim();
            DwarfClanDefinition[] clans = Resources.LoadAll<DwarfClanDefinition>("Racial/Dwarf/Clans");
            if (clans == null || clans.Length == 0)
                return null;

            foreach (DwarfClanDefinition clan in clans)
            {
                if (clan != null && string.Equals(clan.clanId, trimmed, StringComparison.Ordinal))
                    return clan;
            }

            return null;
        }
    }
}
