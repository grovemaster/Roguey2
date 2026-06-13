using JRogue.Stats;
using UnityEngine;

namespace JRogue.Quest
{
    [CreateAssetMenu(fileName = "Quest", menuName = "JRogue/Quest/Quest Definition")]
    public sealed class QuestDefinition : ScriptableObject
    {
        public string questId;
        public string displayTitle;
        [TextArea(2, 6)] public string journalDescription;
        public string giverNpcId;
        public string giverDisplayName;
        public QuestOwnership ownership;
        [Min(0)] public int requiredMinLevel;
        public Race requiredRace = Race.Unset;
        public string learnDragonianSpellId;
        public QuestPrerequisite[] acceptPrerequisites = System.Array.Empty<QuestPrerequisite>();
        public QuestObjectiveDefinition[] objectives = System.Array.Empty<QuestObjectiveDefinition>();
        public QuestRewardBundle rewards;
        public bool autoCompleteOnObjectives;
        public string[] setFlagsOnAccept = System.Array.Empty<string>();
        public string[] setFlagsOnComplete = System.Array.Empty<string>();
        public int sortOrder;

        public string ResolvedQuestId =>
            string.IsNullOrWhiteSpace(questId) ? name : questId.Trim();
    }
}
