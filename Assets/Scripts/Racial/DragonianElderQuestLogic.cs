using System;
using System.Collections.Generic;
using System.Text;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Quest;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    public static class DragonianElderIds
    {
        public const string VolscaleNpcId = "dragonian_elder_volscale";
    }

    public static class DragonianElderQuestLogic
    {
        public const string LogPrefix = "[DragonianElder]";

        public static bool IsSpeakerEligible(
            BaseActor speaker,
            out DragonianSpellsRuntime runtime,
            out string rejectLine)
        {
            runtime = null;
            rejectLine = null;

            if (speaker == null)
            {
                rejectLine = "No speaker.";
                return false;
            }

            CharacterStats stats = speaker.GetComponent<CharacterStats>();
            if (stats == null || stats.race != Race.Dragonian)
            {
                rejectLine = "This elder teaches draconic word-forms to Dragonians only.";
                return false;
            }

            runtime = speaker.GetComponent<DragonianSpellsRuntime>();
            if (runtime == null || stats.racialSubsystem != RacialSubsystemKind.DragonianSpells)
            {
                rejectLine = "Your spirit cannot receive draconic teachings.";
                return false;
            }

            return true;
        }

        public static bool ElderIsUnlocked(DragonianElderDefinition elder, GameStoryFlagService flags)
        {
            if (elder?.unlockStoryFlags == null || elder.unlockStoryFlags.Length == 0)
                return true;

            if (flags == null)
                return false;

            for (int i = 0; i < elder.unlockStoryFlags.Length; i++)
            {
                if (!flags.IsSet(elder.unlockStoryFlags[i]))
                    return false;
            }

            return true;
        }

        public static bool IsChainCompleteForMember(
            DragonianElderDefinition elder,
            IReadOnlyDictionary<string, QuestInstance> instances,
            string memberId)
        {
            if (elder?.chainQuestIds == null || elder.chainQuestIds.Length == 0
                || string.IsNullOrWhiteSpace(memberId))
            {
                return false;
            }

            for (int i = 0; i < elder.chainQuestIds.Length; i++)
            {
                string questId = elder.chainQuestIds[i]?.Trim();
                if (string.IsNullOrEmpty(questId))
                    continue;

                if (!QuestLogic.IsQuestCompletedForMember(instances, questId, memberId))
                    return false;
            }

            return true;
        }

        public static bool HasActiveQuestInChain(
            DragonianElderDefinition elder,
            IReadOnlyDictionary<string, QuestInstance> instances,
            string memberId)
        {
            if (elder?.chainQuestIds == null || string.IsNullOrWhiteSpace(memberId))
                return false;

            for (int i = 0; i < elder.chainQuestIds.Length; i++)
            {
                string questId = elder.chainQuestIds[i]?.Trim();
                if (string.IsNullOrEmpty(questId))
                    continue;

                string storageKey = QuestInstanceKey.StorageKey(questId, memberId);
                if (instances != null
                    && instances.TryGetValue(storageKey, out QuestInstance instance)
                    && instance.state != QuestRuntimeState.Completed
                    && instance.state != QuestRuntimeState.Failed)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetReadyTurnInQuest(
            DragonianElderDefinition elder,
            BaseActor speaker,
            QuestService questService,
            out QuestDefinition definition,
            out QuestInstance instance)
        {
            definition = null;
            instance = null;
            if (elder?.chainQuestIds == null || speaker == null || questService == null)
                return false;

            string memberId = QuestLogic.ResolveMemberId(speaker);
            for (int i = 0; i < elder.chainQuestIds.Length; i++)
            {
                string questId = elder.chainQuestIds[i]?.Trim();
                if (string.IsNullOrEmpty(questId))
                    continue;

                if (!questService.TryGetInstance(questId, memberId, out QuestInstance candidate))
                    continue;

                if (candidate.state != QuestRuntimeState.ReadyToTurnIn)
                    continue;

                QuestDefinition questDefinition = questService.GetDefinition(questId);
                if (questDefinition == null)
                    continue;

                definition = questDefinition;
                instance = candidate;
                return true;
            }

            return false;
        }

        public static bool TryResolveNextOffer(
            DragonianElderDefinition elder,
            BaseActor speaker,
            QuestService questService,
            out QuestDefinition nextQuest,
            out string denyReason)
        {
            nextQuest = null;
            denyReason = null;
            if (elder?.chainQuestIds == null || elder.chainQuestIds.Length == 0)
            {
                denyReason = "This elder has no lessons.";
                return false;
            }

            if (speaker == null || questService == null)
            {
                denyReason = "No speaker.";
                return false;
            }

            string memberId = QuestLogic.ResolveMemberId(speaker);
            if (HasActiveQuestInChain(elder, questService.ActiveInstances, memberId))
            {
                denyReason = "Finish your current lesson with me before the next.";
                return false;
            }

            if (IsChainCompleteForMember(elder, questService.ActiveInstances, memberId))
            {
                denyReason = "You have learned all word-forms I can teach.";
                return false;
            }

            if (!ElderIsUnlocked(elder, GameStoryFlagService.Instance))
            {
                denyReason = "This elder is not ready to teach you yet.";
                return false;
            }

            for (int i = 0; i < elder.chainQuestIds.Length; i++)
            {
                string questId = elder.chainQuestIds[i]?.Trim();
                if (string.IsNullOrEmpty(questId))
                    continue;

                if (QuestLogic.IsQuestCompletedForMember(
                        questService.ActiveInstances,
                        questId,
                        memberId))
                {
                    continue;
                }

                if (i > 0)
                {
                    string priorQuestId = elder.chainQuestIds[i - 1]?.Trim();
                    if (!string.IsNullOrEmpty(priorQuestId)
                        && !QuestLogic.IsQuestCompletedForMember(
                            questService.ActiveInstances,
                            priorQuestId,
                            memberId))
                    {
                        denyReason = "Complete your prior lesson first.";
                        return false;
                    }
                }

                QuestDefinition definition = questService.GetDefinition(questId);
                if (definition == null)
                {
                    denyReason = $"Unknown quest '{questId}'.";
                    return false;
                }

                if (QuestLogic.HasActiveOrCompletedQuestForMember(
                        questService.ActiveInstances,
                        questId,
                        memberId))
                {
                    continue;
                }

                if (!QuestLogic.ValidateMemberAcceptRequirements(definition, speaker, out denyReason))
                    return false;

                if (!questService.TryOfferForMember(questId, speaker, out denyReason))
                    return false;

                nextQuest = definition;
                return true;
            }

            denyReason = "You have learned all word-forms I can teach.";
            return false;
        }

        public static string BuildOfferBodyText(QuestDefinition quest)
        {
            if (quest == null)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append("The next word-form is ");
            if (DragonianSpellCatalogService.TryGetSpell(quest.learnDragonianSpellId, out DragonianSpellDefinition spell)
                && !string.IsNullOrWhiteSpace(spell.displayName))
            {
                sb.Append(spell.displayName);
            }
            else
            {
                sb.Append(quest.learnDragonianSpellId);
            }

            sb.Append(". Will you undertake ").Append(quest.displayTitle).Append('?');
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(BuildObjectiveSummary(quest));
            sb.AppendLine();
            sb.Append(BuildRewardSummary(quest));
            return sb.ToString();
        }

        public static string BuildTurnInBodyText(QuestDefinition quest, BaseActor speaker)
        {
            string spellName = quest?.learnDragonianSpellId;
            if (DragonianSpellCatalogService.TryGetSpell(quest?.learnDragonianSpellId, out DragonianSpellDefinition spell)
                && !string.IsNullOrWhiteSpace(spell.displayName))
            {
                spellName = spell.displayName;
            }

            string speakerName = speaker != null ? speaker.DisplayName : "You";
            return $"You have fulfilled the trial. Shall I seal {spellName} into {speakerName}'s spirit?";
        }

        public static string BuildSuccessLine(QuestDefinition quest, BaseActor speaker)
        {
            string spellName = quest?.learnDragonianSpellId;
            if (DragonianSpellCatalogService.TryGetSpell(quest?.learnDragonianSpellId, out DragonianSpellDefinition spell)
                && !string.IsNullOrWhiteSpace(spell.displayName))
            {
                spellName = spell.displayName;
            }

            string speakerName = speaker != null ? speaker.DisplayName : "You";
            return $"{speakerName} has internalized {spellName}. "
                   + "Visit a safe haven to memorize draconic word-forms before battle.";
        }

        static string BuildObjectiveSummary(QuestDefinition quest)
        {
            if (quest?.objectives == null || quest.objectives.Length == 0)
                return "Trial: none";

            var sb = new StringBuilder("Trial:");
            for (int i = 0; i < quest.objectives.Length; i++)
            {
                QuestObjectiveDefinition objective = quest.objectives[i];
                sb.Append(' ');
                sb.Append(QuestLogic.FormatJournalObjectiveLine(
                    objective,
                    new QuestObjectiveProgress
                    {
                        objectiveId = QuestLogic.ResolveObjectiveId(objective, i),
                        current = 0,
                        required = QuestLogic.ResolveRequiredCount(objective),
                        completed = false,
                    },
                    i));
                if (i < quest.objectives.Length - 1)
                    sb.Append(';');
            }

            return sb.ToString();
        }

        static string BuildRewardSummary(QuestDefinition quest)
        {
            if (quest == null || string.IsNullOrWhiteSpace(quest.learnDragonianSpellId))
                return "Reward: none";

            if (!DragonianSpellCatalogService.TryGetSpell(quest.learnDragonianSpellId, out DragonianSpellDefinition spell))
                return $"Reward: Learn {quest.learnDragonianSpellId}";

            return $"Reward: Learn {spell.displayName} "
                   + $"(memorize {spell.memorizeCost} SP · cast {spell.soulPowerCastCost} SP)";
        }
    }

    public static class DragonianElderRegistry
    {
        const string ResourceFolder = "Racial/Dragonian";

        static Dictionary<string, DragonianElderDefinition> _byNpcId;

        public static DragonianElderDefinition Resolve(DragonianElderDefinition assigned, string npcId)
        {
            if (assigned != null)
                return assigned;

            if (string.IsNullOrWhiteSpace(npcId))
                return null;

            EnsureLoaded();
            _byNpcId.TryGetValue(npcId.Trim(), out DragonianElderDefinition elder);
            return elder;
        }

        static void EnsureLoaded()
        {
            if (_byNpcId != null)
                return;

            _byNpcId = new Dictionary<string, DragonianElderDefinition>(StringComparer.OrdinalIgnoreCase);
            DragonianElderDefinition[] elders = Resources.LoadAll<DragonianElderDefinition>(ResourceFolder);
            for (int i = 0; i < elders.Length; i++)
            {
                DragonianElderDefinition elder = elders[i];
                if (elder == null || string.IsNullOrWhiteSpace(elder.npcId))
                    continue;

                _byNpcId[elder.npcId.Trim()] = elder;
            }
        }

        public static void ResetCacheForTests() => _byNpcId = null;
    }
}
