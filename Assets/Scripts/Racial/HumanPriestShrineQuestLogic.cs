using JRogue.Actors;
using JRogue.Quest;
using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Racial
{
    public static class HumanPriestShrineQuestLogic
    {
        public const string LogPrefix = "[PriestShrine]";

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
                rejectLine = HumanPriestClassCommitService.RaceDenyMessage;
                return false;
            }

            return true;
        }

        public static bool IsAlreadyPriest(CharacterStats stats) =>
            stats != null && stats.humanClass == HumanClass.Priest;

        public static bool HasCommittedElsewhere(CharacterStats stats) =>
            stats != null
            && stats.humanClass != HumanClass.None
            && stats.humanClass != HumanClass.Priest;

        public static bool TryGetActiveInitiation(
            BaseActor speaker,
            QuestService quests,
            out QuestInstance instance,
            out QuestDefinition definition)
        {
            instance = null;
            definition = null;
            if (speaker == null || quests == null)
                return false;

            definition = quests.GetDefinition(HumanPriestShrineIds.InitiationQuestId);
            if (definition == null)
                return false;

            string memberId = QuestLogic.ResolveMemberId(speaker);
            string storageKey = QuestInstanceKey.StorageKey(definition.ResolvedQuestId, memberId);
            if (!quests.ActiveInstances.TryGetValue(storageKey, out instance))
                return false;

            return instance.state != QuestRuntimeState.Completed
                && instance.state != QuestRuntimeState.Failed;
        }

        public static bool IsInitiationCompleted(BaseActor speaker, QuestService quests)
        {
            if (speaker == null || quests == null)
                return false;

            QuestDefinition definition = quests.GetDefinition(HumanPriestShrineIds.InitiationQuestId);
            if (definition == null)
                return false;

            string memberId = QuestLogic.ResolveMemberId(speaker);
            return QuestLogic.IsQuestCompletedForMember(
                quests.ActiveInstances,
                definition.ResolvedQuestId,
                memberId);
        }
    }
}
