using JRogue.Actors;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.World.Generation;

namespace JRogue.Racial
{
    public static class DwarfClanJoinLogic
    {
        public const string RaceDenyMessage = "Only dwarves may swear clan allegiance here.";
        public const string SubsystemDenyMessage = "This dwarf cannot walk the Ancestor path.";
        public const string AlreadyMemberMessage = "You already owe allegiance to a clan.";
        public const string WrongClanMessage = "You belong to another clan.";

        public static bool IsSpeakerDwarf(
            BaseActor speaker,
            out CharacterStats stats,
            out string rejectLine)
        {
            stats = null;
            rejectLine = null;
            if (speaker == null)
            {
                rejectLine = "No speaker.";
                return false;
            }

            stats = speaker.GetComponent<CharacterStats>();
            if (stats == null || stats.race != Race.Dwarf)
            {
                rejectLine = RaceDenyMessage;
                return false;
            }

            if (stats.racialSubsystem != RacialSubsystemKind.DwarfAncestry)
            {
                rejectLine = SubsystemDenyMessage;
                return false;
            }

            return true;
        }

        public static bool CanJoin(DwarfClanMembershipRuntime membership, out string denyReason)
        {
            denyReason = null;
            if (membership != null && membership.IsAffiliated)
            {
                denyReason = AlreadyMemberMessage;
                return false;
            }

            return true;
        }

        public static bool IsMemberOfClan(DwarfClanMembershipRuntime membership, DwarfClanDefinition clan)
        {
            if (membership == null || clan == null)
                return false;

            return membership.MatchesClan(clan);
        }

        public static string BuildOfferBodyText(DwarfClanDefinition clan)
        {
            if (clan == null)
                return "Will you swear allegiance to this clan?";

            string name = string.IsNullOrWhiteSpace(clan.displayName) ? clan.clanId : clan.displayName.Trim();
            string lore = string.IsNullOrWhiteSpace(clan.description)
                ? string.Empty
                : $"\n\n{clan.description.Trim()}";

            return
                $"The {name} offer you a place among their kin. Swear allegiance and walk the path of "
                + $"their patron Ancestor.{lore}";
        }

        public static string BuildSuccessLine(DwarfClanDefinition clan)
        {
            string name = clan == null || string.IsNullOrWhiteSpace(clan.shortName)
                ? clan?.displayName ?? "clan"
                : clan.shortName.Trim();
            return $"You swear allegiance to the {name}. Visit the Hall of Ancestors altar to learn your first technique.";
        }

        public static bool CanBeginJoinCeremony(out string denyReason) =>
            SafeZonePolicyService.TryAllowDwarfClanCeremony(out denyReason);
    }
}
