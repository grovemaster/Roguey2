using System;
using System.Collections.Generic;
using System.Linq;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using JRogue.Manager.Progression;
using JRogue.Racial;
using JRogue.Shop;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Hotbar;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.Quest
{
    public sealed class QuestService : MonoBehaviour
    {
        public static QuestService Instance { get; private set; }

        readonly Dictionary<string, QuestDefinition> _definitions = new Dictionary<string, QuestDefinition>();
        readonly Dictionary<string, QuestInstance> _instances = new Dictionary<string, QuestInstance>();

        int _acceptSequence;
        long _pinSequence;

        public event Action Changed;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadDefinitionsFromResources();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static QuestService EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject(nameof(QuestService));
            return go.AddComponent<QuestService>();
        }

        public static void EnsureRunService() => EnsureInstance();

        public void RegisterDefinition(QuestDefinition definition)
        {
            if (definition == null)
                return;

            string id = definition.ResolvedQuestId;
            if (string.IsNullOrEmpty(id))
                return;

            _definitions[id] = definition;
        }

        public QuestDefinition GetDefinition(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return null;

            _definitions.TryGetValue(questId.Trim(), out QuestDefinition definition);
            return definition;
        }

        public bool TryGetInstance(string questId, out QuestInstance instance) =>
            TryGetInstance(questId, null, out instance);

        public bool TryGetInstance(string questId, string ownerPartyMemberId, out QuestInstance instance)
        {
            instance = null;
            if (string.IsNullOrWhiteSpace(questId))
                return false;

            QuestDefinition definition = GetDefinition(questId);
            string storageKey = ResolveStorageKey(definition, questId.Trim(), ownerPartyMemberId);
            return _instances.TryGetValue(storageKey, out instance);
        }

        public bool TryGetInstanceByStorageKey(string storageKey, out QuestInstance instance)
        {
            instance = null;
            if (string.IsNullOrWhiteSpace(storageKey))
                return false;

            return _instances.TryGetValue(storageKey.Trim(), out instance);
        }

        public static string ResolveStorageKey(
            QuestDefinition definition,
            string questId,
            string ownerPartyMemberId)
        {
            if (definition?.ownership == QuestOwnership.PerPartyMember)
                return QuestInstanceKey.StorageKey(questId, ownerPartyMemberId);

            return questId?.Trim() ?? string.Empty;
        }

        public string ResolveStorageKey(QuestInstance instance)
        {
            if (instance == null)
                return string.Empty;

            QuestDefinition definition = GetDefinition(instance.questId);
            return ResolveStorageKey(definition, instance.questId, instance.ownerPartyMemberId);
        }

        public QuestConditionState GetConditionState(string questId) =>
            QuestLogic.ResolveConditionState(_instances, questId);

        public IReadOnlyList<QuestInstance> GetQuestsInState(QuestRuntimeState state)
        {
            return _instances.Values
                .Where(instance => instance.state == state)
                .OrderBy(instance => GetSortKey(instance))
                .ToList();
        }

        public IReadOnlyList<QuestInstance> GetActiveQuests() =>
            _instances.Values
                .Where(instance => instance.state == QuestRuntimeState.Active
                    || instance.state == QuestRuntimeState.ReadyToTurnIn)
                .OrderBy(instance => GetSortKey(instance))
                .ToList();

        public IReadOnlyList<QuestInstance> GetCompletedQuests() =>
            GetQuestsInState(QuestRuntimeState.Completed);

        public IReadOnlyList<QuestInstance> GetFailedQuests() =>
            GetQuestsInState(QuestRuntimeState.Failed);

        public IReadOnlyDictionary<string, QuestInstance> ActiveInstances => _instances;

        public QuestObjectiveProgress GetProgress(string questId, string objectiveId)
        {
            if (!TryGetInstance(questId, out QuestInstance instance) || instance.progress == null)
                return default;

            return QuestLogic.FindProgress(instance.progress, objectiveId);
        }

        public bool TryOffer(string questId, out string denyReason) =>
            TryOfferForMember(questId, null, out denyReason);

        public bool TryOfferForMember(string questId, BaseActor speaker, out string denyReason)
        {
            denyReason = null;
            QuestDefinition definition = GetDefinition(questId);
            if (definition == null)
            {
                denyReason = $"Unknown quest '{questId}'.";
                return false;
            }

            string resolvedId = definition.ResolvedQuestId;
            string memberId = speaker != null ? QuestLogic.ResolveMemberId(speaker) : null;
            string storageKey = ResolveStorageKey(definition, resolvedId, memberId);
            if (_instances.TryGetValue(storageKey, out QuestInstance existing)
                && existing.state != QuestRuntimeState.Failed)
            {
                denyReason = "Quest already accepted or completed.";
                return false;
            }

            if (speaker != null
                && !QuestLogic.ValidateMemberAcceptRequirements(definition, speaker, out denyReason))
            {
                return false;
            }

            if (!QuestLogic.EvaluatePrerequisites(
                    definition,
                    _instances,
                    GameStoryFlagService.Instance,
                    GetPartyMembers(),
                    memberId,
                    out denyReason))
            {
                return false;
            }

            return true;
        }

        public bool TryAccept(string questId, out string denyReason) =>
            TryAcceptForMember(questId, ResolveRewardRecipient(), out denyReason);

        public bool TryAcceptForMember(string questId, BaseActor speaker, out string denyReason)
        {
            denyReason = null;
            if (speaker == null)
            {
                denyReason = "No quest owner.";
                return false;
            }

            QuestDefinition definition = GetDefinition(questId);
            if (definition?.ownership == QuestOwnership.PerPartyMember
                && !QuestLogic.TryAllowPerPartyMemberQuestChange(definition, out denyReason))
            {
                return false;
            }

            if (!TryOfferForMember(questId, speaker, out denyReason))
                return false;

            definition = GetDefinition(questId);
            string resolvedId = definition.ResolvedQuestId;
            string memberId = QuestLogic.ResolveMemberId(speaker);
            string storageKey = ResolveStorageKey(definition, resolvedId, memberId);
            var instance = new QuestInstance
            {
                questId = resolvedId,
                ownerPartyMemberId = definition.ownership == QuestOwnership.PerPartyMember
                    ? memberId
                    : null,
                state = QuestRuntimeState.Active,
                progress = QuestLogic.CreateInitialProgress(definition),
                acceptOrder = ++_acceptSequence,
                isNew = true,
            };

            _instances[storageKey] = instance;
            ApplyFlags(definition.setFlagsOnAccept);
            RefreshQuest(definition, instance, logObjectiveUpdates: false);
            Debug.Log($"{QuestLogic.LogPrefix} accepted '{definition.displayTitle}' for {memberId}.");
            NotifyChanged();
            return true;
        }

        public bool TryTurnIn(string questId, string npcId, out string denyReason) =>
            TryTurnInForMember(questId, ResolveRewardRecipient(), npcId, out denyReason);

        public bool TryTurnInForMember(string questId, BaseActor speaker, string npcId, out string denyReason)
        {
            denyReason = null;
            QuestDefinition definition = GetDefinition(questId);
            if (definition == null)
            {
                denyReason = $"Unknown quest '{questId}'.";
                return false;
            }

            if (speaker == null)
            {
                denyReason = "No quest owner.";
                return false;
            }

            if (definition.ownership == QuestOwnership.PerPartyMember
                && !QuestLogic.TryAllowPerPartyMemberQuestChange(definition, out denyReason))
            {
                return false;
            }

            string resolvedId = definition.ResolvedQuestId;
            string memberId = QuestLogic.ResolveMemberId(speaker);
            string storageKey = ResolveStorageKey(definition, resolvedId, memberId);
            if (!_instances.TryGetValue(storageKey, out QuestInstance instance))
            {
                denyReason = "Quest is not active.";
                return false;
            }

            if (instance.state == QuestRuntimeState.Completed)
            {
                denyReason = "Quest already completed.";
                return false;
            }

            if (instance.state == QuestRuntimeState.Failed)
            {
                denyReason = "Quest has failed.";
                return false;
            }

            if (definition.ownership == QuestOwnership.PerPartyMember
                && !QuestLogic.ActorIsQuestOwner(definition, instance, speaker))
            {
                BaseActor owner = QuestLogic.FindMemberById(GetPartyMembers(), instance.ownerPartyMemberId);
                string ownerName = owner != null ? owner.DisplayName : instance.ownerPartyMemberId;
                denyReason = $"This trial was sworn by {ownerName}. Let them speak.";
                return false;
            }

            if (!QuestLogic.ValidateMemberAcceptRequirements(definition, speaker, out denyReason))
                return false;

            if (!string.IsNullOrWhiteSpace(definition.giverNpcId)
                && !string.Equals(definition.giverNpcId.Trim(), npcId?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                string giverName = string.IsNullOrWhiteSpace(definition.giverDisplayName)
                    ? definition.giverNpcId
                    : definition.giverDisplayName;
                denyReason = $"You should speak to {giverName}.";
                return false;
            }

            RefreshQuest(definition, instance, logObjectiveUpdates: false);
            if (!QuestLogic.AreRequiredObjectivesComplete(definition, instance.progress))
            {
                denyReason = "Quest objectives are not complete.";
                return false;
            }

            BaseActor rewardRecipient = definition.ownership == QuestOwnership.PerPartyMember
                ? speaker
                : ResolveRewardRecipient();
            if (!QuestLogic.CanReceiveRewardItems(definition.rewards, rewardRecipient, out denyReason))
                return false;

            if (!QuestLogic.TryRemoveDeliverItems(definition, instance, GetPartyMembers(), out denyReason))
                return false;

            if (!TryDeductTurnInGoldCost(definition, out denyReason))
                return false;

            if (!TryGrantCommitHumanClassReward(definition, instance, speaker, out denyReason))
            {
                RefundTurnInGoldCost(definition);
                return false;
            }

            if (!GrantRewards(definition.rewards, rewardRecipient, out denyReason))
            {
                RefundTurnInGoldCost(definition);
                return false;
            }

            if (!TryGrantLearnSpellReward(definition, instance, out denyReason))
            {
                RefundTurnInGoldCost(definition);
                return false;
            }

            ApplyFlags(definition.setFlagsOnComplete);
            instance.state = QuestRuntimeState.Completed;
            instance.isNew = false;
            Debug.Log($"{QuestLogic.LogPrefix} completed '{definition.displayTitle}'.");
            NotifyChanged();
            return true;
        }

        public void PinQuest(string questId) =>
            PinQuest(questId, null);

        public void PinQuest(string questId, string ownerPartyMemberId)
        {
            if (!TryGetInstance(questId, ownerPartyMemberId, out QuestInstance instance))
                return;

            if (instance.state != QuestRuntimeState.Active && instance.state != QuestRuntimeState.ReadyToTurnIn)
                return;

            instance.isPinned = true;
            instance.pinSequence = ++_pinSequence;
            NotifyChanged();
        }

        public void PinQuestInstance(QuestInstance instance)
        {
            if (instance == null)
                return;

            PinQuest(instance.questId, instance.ownerPartyMemberId);
        }

        public void UnpinQuest(string questId) =>
            UnpinQuest(questId, null);

        public void UnpinQuest(string questId, string ownerPartyMemberId)
        {
            if (!TryGetInstance(questId, ownerPartyMemberId, out QuestInstance instance))
                return;

            instance.isPinned = false;
            instance.pinSequence = 0;
            NotifyChanged();
        }

        public void UnpinQuestInstance(QuestInstance instance)
        {
            if (instance == null)
                return;

            UnpinQuest(instance.questId, instance.ownerPartyMemberId);
        }

        public void ClearNewMarker(string questId) =>
            ClearNewMarker(questId, null);

        public void ClearNewMarker(string questId, string ownerPartyMemberId)
        {
            if (!TryGetInstance(questId, ownerPartyMemberId, out QuestInstance instance))
                return;

            if (!instance.isNew)
                return;

            instance.isNew = false;
            NotifyChanged();
        }

        public void ClearNewMarkerForInstance(QuestInstance instance)
        {
            if (instance == null)
                return;

            ClearNewMarker(instance.questId, instance.ownerPartyMemberId);
        }

        public void NotifyEnemyKilled(string speciesId, BaseActor killer)
        {
            if (string.IsNullOrWhiteSpace(speciesId))
                return;

            PartyManager party = PartyManager.Instance;
            bool changed = false;
            foreach (KeyValuePair<string, QuestInstance> pair in _instances)
            {
                if (!QuestInstanceKey.TryParseStorageKey(pair.Key, out string questId, out _))
                    continue;

                QuestDefinition definition = GetDefinition(questId);
                QuestInstance instance = pair.Value;
                if (definition == null
                    || instance.state == QuestRuntimeState.Completed
                    || instance.state == QuestRuntimeState.Failed)
                {
                    continue;
                }

                QuestObjectiveProgress[] before = CloneProgress(instance.progress);
                QuestLogic.NotifyEnemyKilled(definition, instance, speciesId.Trim(), killer, party);
                changed |= RefreshQuest(definition, instance, before);
            }

            if (changed)
                NotifyChanged();

            DivineConductService.NotifyEnemyKilled(speciesId.Trim(), killer);
        }

        public void NotifyInventoryChanged()
        {
            bool changed = false;
            foreach (KeyValuePair<string, QuestInstance> pair in _instances)
            {
                if (!QuestInstanceKey.TryParseStorageKey(pair.Key, out string questId, out _))
                    continue;

                QuestDefinition definition = GetDefinition(questId);
                QuestInstance instance = pair.Value;
                if (definition == null
                    || instance.state == QuestRuntimeState.Completed
                    || instance.state == QuestRuntimeState.Failed)
                {
                    continue;
                }

                QuestObjectiveProgress[] before = CloneProgress(instance.progress);
                QuestLogic.RefreshScanObjectives(
                    definition,
                    instance,
                    GameStoryFlagService.Instance,
                    GetPartyMembers(),
                    PartyManager.Instance);
                changed |= RefreshQuest(definition, instance, before);
            }

            if (changed)
                NotifyChanged();
        }

        public void NotifyNpcTalked(string npcId, BaseActor speaker)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return;

            PartyManager party = PartyManager.Instance;
            bool changed = false;
            foreach (KeyValuePair<string, QuestInstance> pair in _instances)
            {
                if (!QuestInstanceKey.TryParseStorageKey(pair.Key, out string questId, out _))
                    continue;

                QuestDefinition definition = GetDefinition(questId);
                QuestInstance instance = pair.Value;
                if (definition == null
                    || instance.state == QuestRuntimeState.Completed
                    || instance.state == QuestRuntimeState.Failed)
                {
                    continue;
                }

                QuestObjectiveProgress[] before = CloneProgress(instance.progress);
                QuestLogic.NotifyNpcTalked(definition, instance, npcId.Trim(), speaker, party);
                changed |= RefreshQuest(definition, instance, before);
            }

            if (changed)
                NotifyChanged();
        }

        public void NotifyZoneEntered(string zoneId)
        {
            if (string.IsNullOrWhiteSpace(zoneId))
                return;

            PartyManager party = PartyManager.Instance;
            bool changed = false;
            foreach (KeyValuePair<string, QuestInstance> pair in _instances)
            {
                if (!QuestInstanceKey.TryParseStorageKey(pair.Key, out string questId, out _))
                    continue;

                QuestDefinition definition = GetDefinition(questId);
                QuestInstance instance = pair.Value;
                if (definition == null
                    || instance.state == QuestRuntimeState.Completed
                    || instance.state == QuestRuntimeState.Failed)
                {
                    continue;
                }

                QuestObjectiveProgress[] before = CloneProgress(instance.progress);
                QuestLogic.NotifyZoneEntered(definition, instance, zoneId.Trim(), party);
                changed |= RefreshQuest(definition, instance, before);
            }

            if (changed)
                NotifyChanged();
        }

        public void NotifyItemEquipped(BaseActor actor, ItemInstance item, EquipmentSlot slot)
        {
            PartyManager party = PartyManager.Instance;
            bool changed = false;
            foreach (KeyValuePair<string, QuestInstance> pair in _instances)
            {
                if (!QuestInstanceKey.TryParseStorageKey(pair.Key, out string questId, out _))
                    continue;

                QuestDefinition definition = GetDefinition(questId);
                QuestInstance instance = pair.Value;
                if (definition == null
                    || instance.state == QuestRuntimeState.Completed
                    || instance.state == QuestRuntimeState.Failed)
                {
                    continue;
                }

                QuestObjectiveProgress[] before = CloneProgress(instance.progress);
                QuestLogic.NotifyItemEquipped(definition, instance, actor, item, slot, party);
                changed |= RefreshQuest(definition, instance, before);
            }

            if (changed)
                NotifyChanged();
        }

        public void ResetForNewRun()
        {
            _instances.Clear();
            _acceptSequence = 0;
            _pinSequence = 0;
            NotifyChanged();
        }

        void LoadDefinitionsFromResources()
        {
            QuestDefinition[] loaded = Resources.LoadAll<QuestDefinition>("Quest");
            for (int i = 0; i < loaded.Length; i++)
                RegisterDefinition(loaded[i]);
        }

        bool RefreshQuest(
            QuestDefinition definition,
            QuestInstance instance,
            QuestObjectiveProgress[] previousProgress,
            bool logObjectiveUpdates = true)
        {
            QuestLogic.RefreshScanObjectives(
                definition,
                instance,
                GameStoryFlagService.Instance,
                GetPartyMembers(),
                PartyManager.Instance);

            QuestRuntimeState nextState = QuestLogic.ResolveRuntimeState(
                definition,
                instance.progress,
                instance.state);

            if (logObjectiveUpdates && previousProgress != null)
                LogNewlyCompletedObjectives(definition, previousProgress, instance.progress);

            bool changed = instance.state != nextState;
            instance.state = nextState;

            if (definition.autoCompleteOnObjectives
                && instance.state == QuestRuntimeState.ReadyToTurnIn
                && TryAutoComplete(definition, instance))
            {
                changed = true;
            }

            return changed || ProgressChanged(previousProgress, instance.progress);
        }

        bool RefreshQuest(QuestDefinition definition, QuestInstance instance, bool logObjectiveUpdates)
        {
            return RefreshQuest(definition, instance, logObjectiveUpdates ? CloneProgress(instance.progress) : null, logObjectiveUpdates);
        }

        bool TryAutoComplete(QuestDefinition definition, QuestInstance instance)
        {
            BaseActor rewardRecipient = ResolveRewardRecipient();
            if (!QuestLogic.CanReceiveRewardItems(definition.rewards, rewardRecipient, out _))
                return false;

            if (!QuestLogic.TryRemoveDeliverItems(definition, instance, GetPartyMembers(), out _))
                return false;

            if (!GrantRewards(definition.rewards, rewardRecipient, out _))
                return false;

            if (!TryGrantLearnSpellReward(definition, instance, out _))
                return false;

            ApplyFlags(definition.setFlagsOnComplete);
            instance.state = QuestRuntimeState.Completed;
            instance.isNew = false;
            Debug.Log($"{QuestLogic.LogPrefix} auto-completed '{definition.displayTitle}'.");
            return true;
        }

        bool GrantRewards(QuestRewardBundle rewards, BaseActor recipient, out string denyReason)
        {
            denyReason = null;
            if (rewards.gold > 0)
            {
                ItemData gold = Resources.Load<ItemData>("Item/Currency/GoldCoin");
                if (gold == null)
                {
                    denyReason = "Gold reward could not be granted.";
                    return false;
                }

                PartyCurrencyLedger.Instance?.Add(gold, rewards.gold);
            }

            InventoryManager inventory = recipient?.GetComponent<InventoryManager>();
            if (rewards.items != null && inventory != null)
            {
                for (int i = 0; i < rewards.items.Length; i++)
                {
                    ItemData item = rewards.items[i];
                    if (item == null)
                        continue;

                    int quantity = QuestLogic.ResolveRewardQuantity(rewards.itemQuantities, i);
                    if (quantity <= 0)
                        continue;

                    var rewardItem = new ItemInstance(item, quantity);
                    if (!inventory.CanCarry(rewardItem))
                    {
                        denyReason = $"Not enough room for {item.itemName}.";
                        return false;
                    }

                    if (!inventory.AddItem(rewardItem))
                    {
                        denyReason = $"Could not add {item.itemName}.";
                        return false;
                    }
                }
            }

            if (rewards.partyExperience > 0)
                PartyExperienceService.Instance?.AwardPartyExperience(rewards.partyExperience, "QuestReward");

            if (!DwarfClanPrestigeService.TryApplyQuestReward(rewards, out denyReason))
                return false;

            ApplyFlags(rewards.setFlagsOnComplete);
            return true;
        }

        bool TryDeductTurnInGoldCost(QuestDefinition definition, out string denyReason)
        {
            denyReason = null;
            if (definition == null || definition.turnInGoldCost <= 0)
                return true;

            if (ShopGoldUtility.GetPartyGoldTotal() < definition.turnInGoldCost)
            {
                denyReason = HumanMageClassCommitService.GoldDenyMessage;
                return false;
            }

            if (!ShopGoldUtility.TrySpendPartyGold(definition.turnInGoldCost))
            {
                denyReason = HumanMageClassCommitService.GoldDenyMessage;
                return false;
            }

            return true;
        }

        void RefundTurnInGoldCost(QuestDefinition definition)
        {
            if (definition == null || definition.turnInGoldCost <= 0)
                return;

            ShopGoldUtility.AddPartyGold(definition.turnInGoldCost);
        }

        bool TryGrantCommitHumanClassReward(
            QuestDefinition definition,
            QuestInstance instance,
            BaseActor speaker,
            out string denyReason)
        {
            denyReason = null;
            if (definition == null || definition.commitHumanClass == HumanClass.None)
                return true;

            BaseActor owner = definition.ownership == QuestOwnership.PerPartyMember
                ? QuestLogic.FindMemberById(GetPartyMembers(), instance?.ownerPartyMemberId) ?? speaker
                : ResolveRewardRecipient();
            if (owner == null)
            {
                denyReason = "Quest owner is not in the party.";
                return false;
            }

            if (!HumanClassCommitment.TryCommit(owner.gameObject, definition.commitHumanClass, out denyReason))
                return false;

            AbilityHotbarUI.Instance?.RefreshAll();
            return true;
        }

        bool TryGrantLearnSpellReward(QuestDefinition definition, QuestInstance instance, out string denyReason)
        {
            denyReason = null;
            if (definition == null || string.IsNullOrWhiteSpace(definition.learnDragonianSpellId))
                return true;

            BaseActor owner = definition.ownership == QuestOwnership.PerPartyMember
                ? QuestLogic.FindMemberById(GetPartyMembers(), instance?.ownerPartyMemberId)
                : ResolveRewardRecipient();
            if (owner == null)
            {
                denyReason = "Quest owner is not in the party.";
                return false;
            }

            DragonianSpellsRuntime runtime = owner.GetComponent<DragonianSpellsRuntime>();
            if (runtime == null)
            {
                denyReason = "Quest owner cannot learn draconic word-forms.";
                return false;
            }

            if (!runtime.TryLearnSpell(definition.learnDragonianSpellId.Trim(), out denyReason))
                return false;

            AbilityHotbarUI.Instance?.RefreshAll();
            return true;
        }

        void ApplyFlags(string[] flagIds)
        {
            if (flagIds == null || flagIds.Length == 0)
                return;

            GameStoryFlagService flags = GameStoryFlagService.Instance;
            if (flags == null)
                return;

            for (int i = 0; i < flagIds.Length; i++)
                flags.Set(flagIds[i]);
        }

        void LogNewlyCompletedObjectives(
            QuestDefinition definition,
            QuestObjectiveProgress[] before,
            QuestObjectiveProgress[] after)
        {
            if (definition?.objectives == null || before == null || after == null)
                return;

            for (int i = 0; i < definition.objectives.Length; i++)
            {
                string objectiveId = QuestLogic.ResolveObjectiveId(definition.objectives[i], i);
                QuestObjectiveProgress previous = QuestLogic.FindProgress(before, objectiveId);
                QuestObjectiveProgress current = QuestLogic.FindProgress(after, objectiveId);
                if (previous.completed || !current.completed)
                    continue;

                string label = QuestLogic.FormatJournalObjectiveLine(definition.objectives[i], current, i);
                Debug.Log($"{QuestLogic.LogPrefix} {definition.displayTitle}: {label}");
            }
        }

        static bool ProgressChanged(QuestObjectiveProgress[] before, QuestObjectiveProgress[] after)
        {
            if (before == null || after == null)
                return false;

            if (before.Length != after.Length)
                return true;

            for (int i = 0; i < before.Length; i++)
            {
                if (before[i].completed != after[i].completed || before[i].current != after[i].current)
                    return true;
            }

            return false;
        }

        static QuestObjectiveProgress[] CloneProgress(QuestObjectiveProgress[] progress)
        {
            if (progress == null)
                return null;

            var clone = new QuestObjectiveProgress[progress.Length];
            Array.Copy(progress, clone, progress.Length);
            return clone;
        }

        static IReadOnlyList<BaseActor> GetPartyMembers()
        {
            PartyManager party = PartyManager.Instance;
            return party != null ? party.partyMembers : Array.Empty<BaseActor>();
        }

        static BaseActor ResolveRewardRecipient()
        {
            PartyManager party = PartyManager.Instance;
            if (party == null || party.partyMembers == null || party.partyMembers.Count == 0)
                return null;

            int shopperIndex = party.ActiveShopperMemberIndex;
            if (shopperIndex >= 0 && shopperIndex < party.partyMembers.Count)
                return party.partyMembers[shopperIndex];

            return party.GetActiveMember();
        }

        (bool unpinned, long pinOrder, int acceptOrder, int sortOrder) GetSortKey(QuestInstance instance)
        {
            QuestDefinition definition = GetDefinition(instance.questId);
            int sortOrder = definition != null ? definition.sortOrder : 0;
            long pinOrder = instance.isPinned ? instance.pinSequence : long.MaxValue;
            return (!instance.isPinned, pinOrder, -instance.acceptOrder, sortOrder);
        }

        void NotifyChanged() => Changed?.Invoke();
    }
}
