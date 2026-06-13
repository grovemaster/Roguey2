using System.Text;
using JRogue.Actors;
using JRogue.Quest;
using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Racial
{
    public static class HumanMageTutorIds
    {
        public const string TutorNpcId = "human_mage_tutor";
        public const string ArcaneVendorNpcId = "human_arcane_vendor";
        public const string ApprenticeshipQuestId = "quest_mage_tutor_apprenticeship";
    }

    public static class HumanMageTutorQuestLogic
    {
        public const string LogPrefix = "[MageTutor]";

        public static bool IsSpeakerHuman(BaseActor speaker, out CharacterStats stats, out string rejectLine)
        {
            stats = null;
            rejectLine = null;

            if (speaker == null)
            {
                rejectLine = "No speaker.";
                return false;
            }

            stats = speaker.GetComponent<CharacterStats>();
            if (stats == null || stats.race != Race.Human)
            {
                rejectLine = HumanMageClassCommitService.RaceDenyMessage;
                return false;
            }

            return true;
        }

        public static bool IsAlreadyMage(CharacterStats stats) =>
            stats != null && stats.humanClass == HumanClass.Mage;

        public static bool HasCommittedElsewhere(CharacterStats stats) =>
            stats != null
            && stats.humanClass != HumanClass.None
            && stats.humanClass != HumanClass.Mage;

        public static bool TryGetActiveApprenticeship(
            BaseActor speaker,
            QuestService quests,
            out QuestInstance instance,
            out QuestDefinition definition)
        {
            instance = null;
            definition = null;
            if (speaker == null || quests == null)
                return false;

            definition = quests.GetDefinition(HumanMageTutorIds.ApprenticeshipQuestId);
            if (definition == null)
                return false;

            string memberId = QuestLogic.ResolveMemberId(speaker);
            string storageKey = QuestInstanceKey.StorageKey(definition.ResolvedQuestId, memberId);
            if (!quests.ActiveInstances.TryGetValue(storageKey, out instance))
                return false;

            return instance.state != QuestRuntimeState.Completed
                && instance.state != QuestRuntimeState.Failed;
        }

        public static bool IsApprenticeshipCompleted(BaseActor speaker, QuestService quests)
        {
            if (speaker == null || quests == null)
                return false;

            QuestDefinition definition = quests.GetDefinition(HumanMageTutorIds.ApprenticeshipQuestId);
            if (definition == null)
                return false;

            string memberId = QuestLogic.ResolveMemberId(speaker);
            return QuestLogic.IsQuestCompletedForMember(quests.ActiveInstances, definition.ResolvedQuestId, memberId);
        }

        public static string BuildOfferBodyText()
        {
            var sb = new StringBuilder();
            sb.Append("Arcane study demands you relinquish essence consumption. ");
            sb.Append("Pay ");
            sb.Append(HumanMageClassCommitService.ApprenticeshipGoldCost);
            sb.Append(" gold when you are ready to commit to the Mage path.");
            return sb.ToString();
        }

        public static string BuildTurnInBodyText(BaseActor speaker)
        {
            string name = speaker != null ? speaker.DisplayName : "You";
            var sb = new StringBuilder();
            sb.Append(name);
            sb.Append(", surrender mortal attunements and accept the burden of arcana for ");
            sb.Append(HumanMageClassCommitService.ApprenticeshipGoldCost);
            sb.Append(" gold?");
            return sb.ToString();
        }

        public static string BuildCompletionLine(BaseActor speaker)
        {
            string name = speaker != null ? speaker.DisplayName : "You";
            return
                $"{name} surrenders mortal attunements and accepts the burden of arcana. "
                + "Magic Power flows where Soul Power once lived. Seek spellbooks to fill your grimoire.";
        }
    }
}
