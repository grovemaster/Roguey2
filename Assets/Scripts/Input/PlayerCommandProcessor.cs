using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.Actors.Components;
using JRogue.Core.Actor;
using JRogue.Manager.Equipment;
using JRogue.Manager.Essence;
using JRogue.Manager.Grid;
using JRogue.Manager.Floor;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Progression.Proficiency;
using JRogue.Racial;
using JRogue.Hazards;
using JRogue.Traps;
using JRogue.Interactables;
using JRogue.Combat;
using JRogue.Combat.FriendlyFire;
using JRogue.Manager.Combat;
using JRogue.Combat.Targeting;
using JRogue.Manager.Door;
using JRogue.World.MapInteract;
using JRogue.Service.Formation;
using JRogue.UI.Gameplay;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Targeting;
using JRogue.UI.Hotbar;
using JRogue.UI.Inventory;
using JRogue.Core.Targeting;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.Input
{
    /// <summary>
    /// Applies <see cref="PlayerCommand"/> to the game simulation. Single path for live input and replays.
    /// </summary>
    public sealed class PlayerCommandProcessor
    {
        private struct PendingTargetedAbility
        {
            public PlayerAbilitySource Source;
            public int SlotIndex;
            public int AbilityIndex;
            public AbilityAction InventoryAbility;
            public AbilityAction ResolvedAbility;
            public ItemInstance InventoryItemInstance;
            public BaseActor InventoryOwner;
            public int InventoryResumeSelectionIndex;
            public string InventoryLogTag;
            public ItemInstance BowRestoreOffHandInstance;
            public int BowInventoryResumeIndex;
        }

        private InputState currentState = InputState.Normal;
        private PendingTargetedAbility? pendingTargetedAbility;
        private System.Action<int> inventoryTargetedUseCancelCallback;

        private PartyManager partyManager;
        private TurnManager turnManager;
        private GridManager gridManager;
        private MapManager mapManager;
        private TargetingReticleView reticleView;

        public InputState CurrentState => currentState;

        public bool IsPendingInventoryTargetedUse =>
            currentState == InputState.Targeting
            && pendingTargetedAbility.HasValue
            && pendingTargetedAbility.Value.Source == PlayerAbilitySource.InventoryItem;

        public bool IsPendingBowAim =>
            currentState == InputState.Targeting
            && pendingTargetedAbility.HasValue
            && pendingTargetedAbility.Value.Source == PlayerAbilitySource.BowAim;

        public bool IsPendingBowOrInventoryTargeting =>
            IsPendingInventoryTargetedUse || IsPendingBowAim;

        public void SetReticleView(TargetingReticleView view) => reticleView = view;

        public void SetInventoryTargetedUseCancelCallback(System.Action<int> callback) =>
            inventoryTargetedUseCancelCallback = callback;

        /// <summary>Begin targeting for a carried item (inventory closed by UI before call).</summary>
        public bool TryBeginInventoryTargetedUse(
            BaseActor activeMember,
            AbilityAction ability,
            ItemInstance itemInstance,
            BaseActor itemOwner,
            int resumeSelectionIndex,
            string logTag)
        {
            if (ability == null || activeMember == null)
                return false;

            EnsureManagers();

            if (currentState == InputState.Targeting)
                return false;

            if (!turnManager.CanActorTakeAction(activeMember.gameObject))
                return false;

            ItemData itemDef = itemInstance?.Definition;
            if (itemDef != null && !SafeZonePolicyService.TryAllowInventoryUse(itemDef, out string safeDenyReason))
            {
                Debug.Log($"{SafeZonePolicyService.LogPrefix} {safeDenyReason}");
                return false;
            }

            currentState = InputState.Targeting;
            pendingTargetedAbility = new PendingTargetedAbility
            {
                Source = PlayerAbilitySource.InventoryItem,
                InventoryAbility = ability,
                InventoryItemInstance = itemInstance,
                InventoryOwner = itemOwner,
                InventoryResumeSelectionIndex = resumeSelectionIndex,
                InventoryLogTag = logTag ?? string.Empty,
            };

            InventoryTargetedUseLog.Log(logTag, "Use started; inventory closed; targeting active.");
            ShowTargetingReticle(activeMember, ability);
            return true;
        }

        /// <summary>Begin bow targeting. Optional restore instance for invoke-arrow cancel.</summary>
        public bool TryBeginBowAim(
            BaseActor activeMember,
            ItemInstance restoreOffHandOnCancel = null,
            int inventoryResumeIndex = -1)
        {
            if (activeMember == null)
                return false;

            EnsureManagers();

            if (currentState == InputState.Targeting)
                return false;

            if (!turnManager.CanActorTakeAction(activeMember.gameObject))
                return false;

            if (!SafeZonePolicyService.TryAllowHostileAction(out string denyReason))
            {
                Debug.Log($"{SafeZonePolicyService.LogPrefix} {denyReason}");
                return false;
            }

            if (!BowRangedCombatService.HasBowEquipped(activeMember))
            {
                Debug.Log("[Bow] Cannot aim: no bow equipped.");
                return false;
            }

            EquipmentManager equip = activeMember.GetComponent<EquipmentManager>();
            equip?.TryEnsureDefaultAmmoEquipped();

            if (!BowRangedCombatService.HasAnyArrowAvailable(activeMember))
            {
                Debug.Log("[Bow] Cannot shoot: no arrows.");
                return false;
            }

            currentState = InputState.Targeting;
            pendingTargetedAbility = new PendingTargetedAbility
            {
                Source = PlayerAbilitySource.BowAim,
                BowRestoreOffHandInstance = restoreOffHandOnCancel,
                BowInventoryResumeIndex = inventoryResumeIndex,
            };

            Debug.Log("[Bow] Aim started; targeting active.");
            reticleView?.Show(activeMember.GridPosition);
            return true;
        }

        /// <summary>
        /// Returns false when the command is ignored (wrong turn, invalid context, no-op move, etc.).
        /// Party swap matches legacy input and is accepted regardless of turn phase.
        /// </summary>
        public bool TryApply(PlayerCommand command)
        {
            if (command.Kind != PlayerCommandKind.SwapPartyMember && !IsPlayerTurnActive()) return false;

            switch (command.Kind)
            {
                case PlayerCommandKind.MoveGrid:
                    return ApplyMoveGrid(command.Direction);
                case PlayerCommandKind.Wait:
                    return ApplyWait(command.PartyWait);
                case PlayerCommandKind.ConfirmTarget:
                    return ApplyConfirmTarget();
                case PlayerCommandKind.CancelTarget:
                    return ApplyCancelTarget();
                case PlayerCommandKind.AbilitySlot:
                case PlayerCommandKind.HotbarSlot:
                    return ApplyHotbarSlot(command.SlotIndex);
                case PlayerCommandKind.ToggleFormation:
                    return ApplyToggleFormation();
                case PlayerCommandKind.SwapPartyMember:
                    return ApplySwapPartyMember(command.PartyMemberIndex);
                case PlayerCommandKind.PickupFloorItems:
                    return ApplyPickupFloorItems();
                case PlayerCommandKind.AimBow:
                    return ApplyAimBow();
                case PlayerCommandKind.OpenDoor:
                    return ApplyOpenDoor();
                case PlayerCommandKind.CloseDoor:
                    return ApplyCloseDoor();
                case PlayerCommandKind.Interact:
                    return ApplyInteract();
                default:
                    return false;
            }
        }

        public void ProcessFollowerRush()
        {
            EnsureManagers();
            FormationRushService.Rush(partyManager, turnManager, gridManager, mapManager);
        }

        void CompleteFormationMemberMove(BaseActor member, Vector3Int oldPosition)
        {
            if (member.GridPosition != oldPosition)
                partyManager.RecordMemberMove(member, member.GridPosition, oldPosition);
            else
            {
                Debug.Log(
                    $"[FORMATION-BUMP] {member.name} attacked without moving. Position stayed {oldPosition}.");
                partyManager.SnapHistoryToCurrentPositions();
            }

            ProcessFollowerRush();
        }

        private bool IsPlayerTurnActive() =>
            TurnManager.Instance != null && TurnManager.Instance.currentState == GameState.PLAYER_TURN;

        private void EnsureManagers()
        {
            if (partyManager == null) partyManager = PartyManager.Instance;
            if (turnManager == null) turnManager = TurnManager.Instance;
            if (gridManager == null) gridManager = GridManager.Instance;
            if (mapManager == null) mapManager = MapManager.Instance;
        }

        private bool ApplyMoveGrid(Vector3Int direction)
        {
            EnsureManagers();

            if (currentState == InputState.Targeting)
            {
                if (direction != Vector3Int.zero) reticleView?.Move(direction);
                return true;
            }

            if (direction == Vector3Int.zero) return false;

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null) return false;
            if (!turnManager.CanActorTakeAction(activeMember.gameObject))
            {
                Debug.Log($"{activeMember.name} has already acted! Switch characters or end turn.");
                return false;
            }

            Vector3Int targetTile = activeMember.GridPosition + direction;
            Vector3Int oldPosition = activeMember.GridPosition;

            DoorPlayerActionResult doorBump = DoorPlayerInteraction.TryBumpOpenAndMove(activeMember, targetTile);
            if (doorBump != DoorPlayerActionResult.NotHandled)
                return true;

            IBattleTarget occupant = gridManager.GetActorAt(targetTile);

            BaseActor swappableAlly = null;
            bool isAllySwap = occupant is BaseActor actor && partyManager.partyMembers.Contains(actor);
            if (isAllySwap) swappableAlly = (BaseActor)occupant;

            bool isEnemyBump = occupant != null && !isAllySwap;

            bool isInteractableBump = InteractableTileService.Instance != null
                && InteractableTileService.Instance.ShouldAttemptPlayerBump(
                    activeMember.GridPosition,
                    targetTile);

            if (isAllySwap
                && swappableAlly != null
                && FormationRushService.IsValidMove(
                    mapManager,
                    gridManager,
                    targetTile,
                    new Dictionary<BaseActor, Vector3Int>(),
                    allowAllies: true,
                    follower: activeMember))
            {
                GridMover leaderMover = activeMember.GetComponent<GridMover>();
                GridMover allyMover = swappableAlly.GetComponent<GridMover>();
                if (GridMover.TrySwap(leaderMover, allyMover))
                {
                    Debug.Log($"[MANUAL-SWAP] {activeMember.name} swapped with {swappableAlly.name}");
                    turnManager.OnPlayerActionComplete(activeMember.gameObject);
                }

                return true;
            }

            bool formationActive = partyManager.IsFormationActive;
            if (TryConfirmGatedMove(
                    activeMember,
                    direction,
                    targetTile,
                    isEnemyBump,
                    formationActive,
                    oldPosition))
            {
                return true;
            }

            if (isInteractableBump
                && InteractableTileService.Instance != null
                && TryActivateInteractableBump(activeMember, targetTile))
            {
                return true;
            }

            if (formationActive)
            {
                if (isEnemyBump
                    || isInteractableBump
                    || FormationRushService.IsValidMove(
                        mapManager,
                        gridManager,
                        targetTile,
                        new Dictionary<BaseActor, Vector3Int>(),
                        allowAllies: true,
                        follower: activeMember))
                {
                    if (activeMember.TryMove(direction))
                        CompleteFormationMemberMove(activeMember, oldPosition);
                }

                return true;
            }

            if (activeMember.TryMove(direction))
                turnManager.OnPlayerActionComplete(activeMember.gameObject);

            return true;
        }

        bool ApplyPickupFloorItems()
        {
            if (currentState == InputState.Targeting)
                return false;

            EnsureManagers();
            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null)
                return false;

            return FloorPickupCoordinator.TryBeginManualPickup(activeMember);
        }

        static bool TryActivateInteractableBump(BaseActor actor, Vector3Int targetTile)
        {
            if (actor == null || InteractableTileService.Instance == null)
                return false;

            if (!InteractableTileService.Instance.ShouldAttemptPlayerBump(actor.GridPosition, targetTile))
                return false;

            InteractableBumpResult result =
                InteractableTileService.Instance.TryBumpActivate(targetTile, actor);

            if (result != InteractableBumpResult.Activated)
                return false;

            TurnManager turn = TurnManager.Instance;
            if (turn != null && actor.gameObject.CompareTag("Player"))
                turn.OnPlayerActionComplete(actor.gameObject);

            PartyManager party = PartyManager.Instance;
            if (party != null && party.IsFormationActive)
                FormationRushService.Rush(party, turn, GridManager.Instance, MapManager.Instance);

            return true;
        }

        bool TryConfirmGatedMove(
            BaseActor activeMember,
            Vector3Int direction,
            Vector3Int targetTile,
            bool isEnemyBump,
            bool formationActive,
            Vector3Int oldPosition)
        {
            if (TrapMoveGate.TryInterceptMove(
                    activeMember,
                    targetTile,
                    isEnemyBump,
                    () => CompleteConfirmedMove(activeMember, direction, formationActive, oldPosition)))
            {
                return true;
            }

            if (HazardMoveGate.TryInterceptMove(
                    activeMember,
                    targetTile,
                    isEnemyBump,
                    () => CompleteConfirmedMove(activeMember, direction, formationActive, oldPosition)))
            {
                return true;
            }

            if (EssenceMoveGate.TryInterceptMove(
                    activeMember,
                    targetTile,
                    isEnemyBump,
                    () => CompleteConfirmedMove(activeMember, direction, formationActive, oldPosition)))
            {
                return true;
            }

            return AutoPickupMoveGate.TryInterceptMove(
                activeMember,
                targetTile,
                isEnemyBump,
                () => CompleteConfirmedMove(activeMember, direction, formationActive, oldPosition));
        }

        void CompleteConfirmedMove(
            BaseActor activeMember,
            Vector3Int direction,
            bool formationActive,
            Vector3Int oldPosition)
        {
            if (!activeMember.TryMove(direction))
                return;

            Vector3Int dest = activeMember.GridPosition;
            FloorEssenceService.Instance?.TryClaimAt(dest, activeMember);
            // Silent auto-pickup already ran via GridMover.Moved → ManaStoneAutoPickupService.
            FloorPickupService.PickupConfirmGatedAt(dest, activeMember.gameObject);

            if (formationActive)
            {
                CompleteFormationMemberMove(activeMember, oldPosition);
            }
            else
            {
                turnManager.OnPlayerActionComplete(activeMember.gameObject);
            }
        }

        private bool ApplyWait(bool partyWait)
        {
            EnsureManagers();

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null) return false;

            if (!turnManager.CanActorTakeAction(activeMember.gameObject))
            {
                Debug.Log($"{activeMember.name} has already acted! Switch characters or end turn.");
                return false;
            }

            if (partyWait)
            {
                Debug.Log("Party is skipping turns...");
                if (partyManager.IsFormationActive) ProcessFollowerRush();
                turnManager.ForceEndPlayerTurn();
                return true;
            }

            Debug.Log($"{activeMember.name} is skipping turn...");
            HazardService.Instance?.OnActorWaitOnCell(activeMember);

            if (partyManager.IsFormationActive)
            {
                ProcessFollowerRush();
                turnManager.OnPlayerActionComplete(activeMember.gameObject);
            }
            else
            {
                turnManager.OnPlayerActionComplete(activeMember.gameObject);
            }

            return true;
        }

        private bool ApplyConfirmTarget()
        {
            if (currentState != InputState.Targeting || !pendingTargetedAbility.HasValue) return false;

            EnsureManagers();

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null || reticleView == null) return false;

            PendingTargetedAbility pending = pendingTargetedAbility.Value;

            if (!TryAllowPendingTargetedAction(pending, out string denyReason))
            {
                Debug.Log($"{SafeZonePolicyService.LogPrefix} {denyReason}");
                return false;
            }

            Vector3Int target = reticleView.Position;
            TargetedActionContext context = BuildTargetedActionContext(pending);

            if (!TargetingSightGate.TryAllowConfirm(target, out string sightDeny))
            {
                Debug.Log($"{TargetingSightGate.LogPrefix} {sightDeny}");
                return true;
            }

            if (FriendlyFireTargetGate.TryInterceptConfirm(
                    activeMember,
                    context,
                    target,
                    () => CompletePendingTargetedAction(activeMember, pending, target)))
            {
                return true;
            }

            return CompletePendingTargetedAction(activeMember, pending, target);
        }

        bool CompletePendingTargetedAction(
            BaseActor activeMember,
            PendingTargetedAbility pending,
            Vector3Int target)
        {
            EssenceSlotManager actorEssence = activeMember.GetComponent<EssenceSlotManager>();
            EquipmentManager equipManager = activeMember.GetComponent<EquipmentManager>();
            HumanMageSpellsRuntime mageSpells = activeMember.GetComponent<HumanMageSpellsRuntime>();
            DragonianSpellsRuntime dragonianSpells = activeMember.GetComponent<DragonianSpellsRuntime>();

            bool ok = pending.Source switch
            {
                PlayerAbilitySource.Essence =>
                    actorEssence != null
                    && actorEssence.TryExecuteAbility(pending.SlotIndex, pending.AbilityIndex, target),
                PlayerAbilitySource.EquipmentItem =>
                    equipManager != null
                    && equipManager.TryExecuteItemAbility(pending.SlotIndex, pending.AbilityIndex, target),
                PlayerAbilitySource.HumanMageSpell =>
                    TryExecuteMageSpellWithProficiency(
                        activeMember,
                        mageSpells,
                        pending.SlotIndex,
                        target),
                PlayerAbilitySource.DragonianSpell =>
                    TryExecuteDragonianSpellWithProficiency(
                        activeMember,
                        dragonianSpells,
                        pending.SlotIndex,
                        target),
                PlayerAbilitySource.HumanKnightSkill =>
                    HumanKnightSkillExecutionService.TryExecute(
                        activeMember,
                        pending.ResolvedAbility,
                        target,
                        out _),
                PlayerAbilitySource.InventoryItem =>
                    TryExecuteInventoryItemTargetedUse(pending, activeMember, target),
                PlayerAbilitySource.BowAim =>
                    BowRangedCombatService.TryExecuteBowShot(activeMember, target, 1),
                PlayerAbilitySource.RacialActive =>
                    pending.ResolvedAbility != null
                    && pending.ResolvedAbility.Execute(activeMember.gameObject, target)
                    && HumanClassAbilityResources.TrySpend(activeMember.stats, pending.ResolvedAbility),
                _ => false,
            };

            if (!ok) return false;

            ExitTargetingMode();

            if (partyManager.IsFormationActive)
            {
                partyManager.RecordMemberMove(activeMember, activeMember.GridPosition);
                ProcessFollowerRush();
                turnManager.ForceEndPlayerTurn();
            }
            else
            {
                turnManager.OnPlayerActionComplete(activeMember.gameObject);
            }

            Debug.Log($"Targeted ability executed. Leader now at: {activeMember.GridPosition}");
            return true;
        }

        static TargetedActionContext BuildTargetedActionContext(PendingTargetedAbility pending)
        {
            switch (pending.Source)
            {
                case PlayerAbilitySource.InventoryItem:
                    return TargetedActionContext.FromInventory(pending.InventoryAbility);
                case PlayerAbilitySource.EquipmentItem:
                    return TargetedActionContext.FromEquipment(pending.SlotIndex, pending.AbilityIndex);
                case PlayerAbilitySource.HumanMageSpell:
                    return TargetedActionContext.FromHumanMageSpell(pending.AbilityIndex);
                case PlayerAbilitySource.DragonianSpell:
                    return TargetedActionContext.FromDragonianSpell(pending.AbilityIndex);
                case PlayerAbilitySource.BowAim:
                    return TargetedActionContext.BowAim();
                case PlayerAbilitySource.RacialActive:
                    return TargetedActionContext.FromRacial(pending.ResolvedAbility);
                case PlayerAbilitySource.HumanKnightSkill:
                    return TargetedActionContext.FromRacial(pending.ResolvedAbility);
                default:
                    return TargetedActionContext.FromEssence(pending.SlotIndex, pending.AbilityIndex);
            }
        }

        bool ApplyAimBow()
        {
            EnsureManagers();
            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null)
                return false;

            return TryBeginBowAim(activeMember);
        }

        bool ApplyOpenDoor()
        {
            EnsureManagers();
            if (currentState == InputState.Targeting)
                return false;

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null)
                return false;

            if (!turnManager.CanActorTakeAction(activeMember.gameObject))
            {
                Debug.Log($"{activeMember.name} has already acted!");
                return false;
            }

            return DoorPlayerInteraction.TryOpenAdjacent(activeMember) == DoorPlayerActionResult.Succeeded;
        }

        bool ApplyCloseDoor()
        {
            EnsureManagers();
            if (currentState == InputState.Targeting)
                return false;

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null)
                return false;

            if (!turnManager.CanActorTakeAction(activeMember.gameObject))
            {
                Debug.Log($"{activeMember.name} has already acted!");
                return false;
            }

            return DoorPlayerInteraction.TryCloseAdjacent(activeMember) == DoorPlayerActionResult.Succeeded;
        }

        bool ApplyInteract()
        {
            EnsureManagers();
            if (currentState == InputState.Targeting)
                return false;

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null)
                return false;

            return MapInteractPlayerInteraction.TryInteractAdjacent(activeMember);
        }

        static void RestoreBowOffHandAfterCancel(BaseActor actor, PendingTargetedAbility pending)
        {
            if (actor == null || pending.BowRestoreOffHandInstance == null)
                return;

            EquipmentManager equip = actor.GetComponent<EquipmentManager>();
            if (equip == null)
                return;

            ItemInstance restore = pending.BowRestoreOffHandInstance;
            ItemInstance current = equip.GetEquippedInstance(EquipmentSlot.OffHand);
            if (current != null && current.Id == restore.Id)
                return;

            if (current != null)
                equip.TryUnequipToBag(EquipmentSlot.OffHand);

            InventoryManager inv = actor.GetComponent<InventoryManager>();
            if (inv == null)
                return;

            foreach (ItemInstance c in inv.CarriedItems)
            {
                if (c != null && c.Id == restore.Id)
                {
                    equip.EquipItem(EquipmentSlot.OffHand, restore);
                    return;
                }
            }
        }

        private bool ApplyCancelTarget()
        {
            if (currentState != InputState.Targeting) return false;

            EnsureManagers();
            BaseActor activeMember = partyManager.GetActiveMember();

            if (pendingTargetedAbility.HasValue)
            {
                PendingTargetedAbility pending = pendingTargetedAbility.Value;

                if (pending.Source == PlayerAbilitySource.InventoryItem)
                {
                    if (inventoryTargetedUseCancelCallback != null)
                    {
                        inventoryTargetedUseCancelCallback.Invoke(pending.InventoryResumeSelectionIndex);
                        InventoryTargetedUseLog.Log(
                            pending.InventoryLogTag,
                            "Cancelled; item retained; inventory reopened; selection restored.");
                    }
                    else
                    {
                        InventoryTargetedUseLog.LogWarning(
                            pending.InventoryLogTag,
                            "Cancel with no pending scroll state.");
                    }
                }
                else if (pending.Source == PlayerAbilitySource.BowAim)
                {
                    RestoreBowOffHandAfterCancel(activeMember, pending);
                    if (pending.BowInventoryResumeIndex >= 0 && inventoryTargetedUseCancelCallback != null)
                        inventoryTargetedUseCancelCallback.Invoke(pending.BowInventoryResumeIndex);
                    Debug.Log("[Bow] Aim cancelled; no arrow consumed.");
                }
                else
                {
                    Debug.Log("Targeted Ability Cancelled.");
                }
            }

            ExitTargetingMode();
            return true;
        }

        static bool TryExecuteInventoryItemTargetedUse(
            PendingTargetedAbility pending,
            BaseActor activeMember,
            Vector3Int target)
        {
            AbilityAction ability = pending.InventoryAbility;
            if (ability == null || activeMember == null)
                return false;

            if (!ability.Execute(activeMember.gameObject, target))
            {
                InventoryTargetedUseLog.Log(pending.InventoryLogTag, $"Confirm rejected at {target}.");
                return false;
            }

            BaseActor itemOwner = pending.InventoryOwner;
            InventoryManager inventory = itemOwner != null ? itemOwner.GetComponent<InventoryManager>() : null;
            ItemInstance itemInstance = pending.InventoryItemInstance;
            if (inventory != null && itemInstance != null)
            {
                if (EvocableChargeRules.IsEvocable(itemInstance))
                    EvocableChargeRules.SpendChargeAfterSuccessfulInvoke(inventory, itemInstance);
                else if (!inventory.TryConsumeCarriedQuantity(itemInstance, 1))
                {
                    InventoryTargetedUseLog.LogWarning(
                        pending.InventoryLogTag,
                        $"Execute succeeded but TryConsumeCarriedQuantity failed for {itemInstance.Id}.");
                }
            }

            string consumeNote = itemInstance != null && EvocableChargeRules.IsEvocable(itemInstance)
                ? "charge spent"
                : "item consumed";
            InventoryTargetedUseLog.Log(
                pending.InventoryLogTag,
                $"Confirm success at {target}; {consumeNote}; turn ended.");
            return true;
        }

        private bool ApplyHotbarSlot(int slotIndex)
        {
            EnsureManagers();

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null)
                return false;

            return TryActivateHotbarMainSlot(slotIndex);
        }

        public bool TryActivateHotbarMainSlot(int slotIndex)
        {
            EnsureManagers();

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null || slotIndex < 0 || slotIndex >= HotbarLayout.HotbarMainSlotCount)
                return false;

            HotbarLayout layout = HotbarLayout.EnsureOn(activeMember);
            if (layout == null)
                return false;

            return TryActivateHotbarEntry(activeMember, layout.GetSlot(slotIndex));
        }

        public bool TryActivateHotbarEntry(BaseActor actor, HotbarEntry entry)
        {
            if (actor == null || entry == null || entry.IsEmpty())
                return false;

            EnsureManagers();

            if (currentState == InputState.Targeting)
                return false;

            HotbarResolvedAction resolved = HotbarResolver.Resolve(actor, entry);
            if (!resolved.IsValid)
            {
                if (!string.IsNullOrEmpty(resolved.DenyReason))
                    Debug.Log(resolved.DenyReason);
                return false;
            }

            (bool usable, _, string denyReason) = HotbarUsabilityService.Evaluate(actor, resolved);
            if (!usable)
            {
                if (!string.IsNullOrEmpty(denyReason))
                    Debug.Log(denyReason);
                return false;
            }

            if (resolved.Kind == HotbarEntryKind.ElementalSpiritSummon)
                return TryExecuteHotbarSpiritSummon(actor, resolved);

            if (HotbarUsabilityService.RequiresTurnSlotBeforeUse(actor, resolved)
                && !turnManager.CanActorTakeAction(actor.gameObject))
            {
                Debug.LogWarning($"[INPUT] {actor.name} has already acted and cannot use abilities.");
                return false;
            }

            if (resolved.Kind == HotbarEntryKind.InventoryUse)
                return TryExecuteHotbarInventoryUse(actor, resolved);

            AbilityAction ability = resolved.Ability;
            if (ability == null)
                return false;

            if (!TryAllowPlayerAbilitySource(resolved, out string safeDeny))
            {
                Debug.Log($"{SafeZonePolicyService.LogPrefix} {safeDeny}");
                return false;
            }

            if (ability.requiresTarget)
                return TryBeginHotbarTargetedUse(actor, resolved);

            bool success = ExecuteResolvedUntargeted(actor, resolved);
            if (success && HotbarUsabilityService.RequiresTurnSlotAfterUse(actor, resolved))
                CompleteActorAction(actor);

            return success;
        }

        bool TryExecuteHotbarSpiritSummon(BaseActor actor, HotbarResolvedAction resolved)
        {
            ElementalSpiritContractsRuntime contracts = actor.GetComponent<ElementalSpiritContractsRuntime>();
            if (contracts == null)
                return false;

            string instanceId = resolved.ContractInstanceId;
            if (string.IsNullOrEmpty(instanceId))
                return false;

            if (contracts.IsInstanceSummoned(instanceId))
            {
                if (!contracts.TryDismissInstance(instanceId))
                    return false;

                if (contracts.TryGetPreset(instanceId, out ElementalSpiritContractPreset preset)
                    && preset.spirit != null)
                {
                    string name = string.IsNullOrWhiteSpace(preset.spirit.displayName)
                        ? preset.spirit.spiritId
                        : preset.spirit.displayName.Trim();
                    Debug.Log($"[ElementalSpirit] {name} dismissed.");
                }

                AbilityHotbarUI.Instance?.RefreshAll();
                return true;
            }

            if (!contracts.TrySummonInstance(instanceId, out string failureReason))
            {
                if (!string.IsNullOrEmpty(failureReason))
                    Debug.Log(failureReason);
                return false;
            }

            if (contracts.TryGetPreset(instanceId, out ElementalSpiritContractPreset summoned)
                && summoned.spirit != null)
            {
                string name = string.IsNullOrWhiteSpace(summoned.spirit.displayName)
                    ? summoned.spirit.spiritId
                    : summoned.spirit.displayName.Trim();
                Debug.Log($"[ElementalSpirit] {name} summoned.");
            }

            AbilityHotbarUI.Instance?.RefreshAll();
            return true;
        }

        public bool TryBeginHotbarTargetedUse(BaseActor actor, HotbarResolvedAction resolved)
        {
            if (actor == null || !resolved.IsValid)
                return false;

            AbilityAction ability = resolved.Ability;
            if (ability == null || !ability.requiresTarget)
                return false;

            EnsureManagers();

            if (currentState == InputState.Targeting)
                return false;

            if (!turnManager.CanActorTakeAction(actor.gameObject))
                return false;

            if (resolved.Source == PlayerAbilitySource.InventoryItem)
            {
                ItemData itemDef = resolved.ItemInstance?.Definition;
                if (itemDef != null && !SafeZonePolicyService.TryAllowInventoryUse(itemDef, out string safeDenyReason))
                {
                    Debug.Log($"{SafeZonePolicyService.LogPrefix} {safeDenyReason}");
                    return false;
                }

                currentState = InputState.Targeting;
                pendingTargetedAbility = new PendingTargetedAbility
                {
                    Source = PlayerAbilitySource.InventoryItem,
                    InventoryAbility = ability,
                    InventoryItemInstance = resolved.ItemInstance,
                    InventoryOwner = resolved.ItemOwner,
                    InventoryResumeSelectionIndex = -1,
                    InventoryLogTag = itemDef?.inventoryTargetedUseLogTag ?? string.Empty,
                };

                ShowTargetingReticle(actor, ability);
                return true;
            }

            if (!TryAllowPlayerAbilitySource(resolved, out string denyReason))
            {
                Debug.Log($"{SafeZonePolicyService.LogPrefix} {denyReason}");
                return false;
            }

            currentState = InputState.Targeting;
            pendingTargetedAbility = new PendingTargetedAbility
            {
                Source = resolved.Source,
                SlotIndex = resolved.SlotIndex,
                AbilityIndex = resolved.AbilityIndex,
                ResolvedAbility = resolved.Source == PlayerAbilitySource.RacialActive
                    || resolved.Source == PlayerAbilitySource.HumanKnightSkill
                        ? ability
                        : null,
                InventoryAbility = resolved.Source == PlayerAbilitySource.RacialActive ? ability : null,
            };

            Debug.Log($"Entered Targeting Mode for {ability.abilityName}. Move reticle, then confirm.");
            ShowTargetingReticle(actor, ability);
            return true;
        }

        bool TryExecuteHotbarInventoryUse(BaseActor actor, HotbarResolvedAction resolved)
        {
            if (resolved.ItemInstance == null || resolved.ItemOwner == null)
                return false;

            InventoryViewModel viewModel = InventoryViewModel.BuildPartyMember(new[] { actor }, resolved.ItemOwner);
            InventoryViewModel.Row? row = null;
            foreach (InventoryViewModel.Row candidate in viewModel.Rows)
            {
                if (candidate.Instance != null && candidate.Instance.Id == resolved.ItemInstance.Id)
                {
                    row = candidate;
                    break;
                }
            }

            if (!row.HasValue)
                return false;

            bool inCombat = CombatThreatCoordinator.Instance != null && CombatThreatCoordinator.Instance.IsInCombat;
            InventoryUseResult result = InventoryItemUse.TryUseCarriedItem(row.Value, inCombat);
            if (result.Outcome == InventoryUseOutcome.StartedTargeting)
            {
                InventoryTargetedUsePending pending = result.TargetingPending;
                return TryBeginInventoryTargetedUse(
                    actor,
                    pending.Ability,
                    pending.Instance,
                    pending.Owner,
                    pending.ResumeSelectionIndex,
                    pending.LogTag);
            }

            if (result.Outcome == InventoryUseOutcome.Failed)
            {
                if (!string.IsNullOrEmpty(result.FailureReason))
                    Debug.Log(result.FailureReason);
                return false;
            }

            return true;
        }

        bool ExecuteResolvedUntargeted(BaseActor actor, HotbarResolvedAction resolved)
        {
            EssenceSlotManager actorEssence = actor.GetComponent<EssenceSlotManager>();
            EquipmentManager equipManager = actor.GetComponent<EquipmentManager>();
            HumanMageSpellsRuntime mageSpells = actor.GetComponent<HumanMageSpellsRuntime>();
            DragonianSpellsRuntime dragonianSpells = actor.GetComponent<DragonianSpellsRuntime>();

            return resolved.Source switch
            {
                PlayerAbilitySource.EquipmentItem =>
                    equipManager != null
                    && equipManager.TryExecuteItemAbility(resolved.SlotIndex, resolved.AbilityIndex),
                PlayerAbilitySource.HumanMageSpell =>
                    TryExecuteMageSpellWithProficiency(actor, mageSpells, resolved.AbilityIndex),
                PlayerAbilitySource.DragonianSpell =>
                    TryExecuteDragonianSpellWithProficiency(actor, dragonianSpells, resolved.AbilityIndex),
                PlayerAbilitySource.HumanKnightSkill =>
                    HumanKnightSkillExecutionService.TryExecute(
                        actor,
                        resolved.Ability,
                        null,
                        out _),
                PlayerAbilitySource.RacialActive =>
                    TryExecuteHotbarRacialActive(actor, resolved),
                PlayerAbilitySource.InventoryItem =>
                    TryExecuteHotbarInventoryActiveImmediate(actor, resolved),
                _ => actorEssence != null && actorEssence.TryExecuteAbility(resolved.SlotIndex, resolved.AbilityIndex),
            };
        }

        static bool TryExecuteHotbarRacialActive(BaseActor actor, HotbarResolvedAction resolved)
        {
            AbilityAction ability = resolved.Ability;
            if (ability == null)
                return false;

            if (HotbarResolver.IsElementalSpiritActiveBinding(resolved.RacialBindingKey))
            {
                ElementalSpiritContractsRuntime contracts = actor.GetComponent<ElementalSpiritContractsRuntime>();
                return contracts != null && contracts.TryExecuteSpiritActiveForAbility(ability);
            }

            return ability.Execute(actor.gameObject)
                && HumanClassAbilityResources.TrySpend(actor.stats, ability);
        }

        bool TryExecuteHotbarInventoryActiveImmediate(BaseActor actor, HotbarResolvedAction resolved)
        {
            AbilityAction ability = resolved.Ability;
            BaseActor itemOwner = resolved.ItemOwner ?? actor;
            if (ability == null || ability.requiresTarget)
                return false;

            if (!ability.Execute(itemOwner.gameObject))
                return false;

            InventoryManager inventory = itemOwner.GetComponent<InventoryManager>();
            ItemInstance itemInstance = resolved.ItemInstance;
            if (inventory != null && itemInstance != null)
            {
                if (EvocableChargeRules.IsEvocable(itemInstance))
                    EvocableChargeRules.SpendChargeAfterSuccessfulInvoke(inventory, itemInstance);
                else
                    inventory.TryConsumeCarriedQuantity(itemInstance, 1);
            }

            return true;
        }

        void CompleteActorAction(BaseActor actor)
        {
            if (partyManager.IsFormationActive)
            {
                partyManager.RecordMemberMove(actor, actor.GridPosition);
                ProcessFollowerRush();
            }
            else
            {
                turnManager.OnPlayerActionComplete(actor.gameObject);
            }
        }

        private bool ApplyToggleFormation()
        {
            EnsureManagers();

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null) return false;

            bool hasActed = !turnManager.CanActorTakeAction(activeMember.gameObject);

            if (!partyManager.IsFormationActive)
            {
                if (hasActed)
                {
                    Debug.LogWarning($"[FORMATION] Cannot enable: {activeMember.name} has already taken an action.");
                    return false;
                }

                partyManager.ToggleFormationActive();
                partyManager.SnapHistoryToCurrentPositions();
                Debug.Log($"[FORMATION] Enabled. {activeMember.name} is now the leader.");
            }
            else
            {
                partyManager.ToggleFormationActive();
                Debug.Log("[FORMATION] Disabled. Party members will move individually.");
            }

            return true;
        }

        private bool ApplySwapPartyMember(int zeroBasedIndex)
        {
            EnsureManagers();
            partyManager.SwapActiveMember(zeroBasedIndex);

            BaseActor newActive = partyManager.GetActiveMember();
            if (newActive != null)
            {
                Debug.Log($"[SWAP] Now controlling {newActive.name}.");
            }

            return true;
        }

        private void EnterTargetingMode(
            BaseActor actor,
            AbilityAction ability,
            PlayerAbilitySource source,
            int slotIndex,
            int abilityIndex)
        {
            currentState = InputState.Targeting;
            pendingTargetedAbility = new PendingTargetedAbility
            {
                Source = source,
                SlotIndex = slotIndex,
                AbilityIndex = abilityIndex,
            };

            Debug.Log($"Entered Targeting Mode for {ability.abilityName}. Move reticle, then confirm.");
            ShowTargetingReticle(actor, ability);
        }

        void ShowTargetingReticle(BaseActor actor, AbilityAction ability)
        {
            if (reticleView == null || actor == null)
                return;

            reticleView.Show(
                actor.GridPosition,
                ability?.ResolveSplashZone(),
                actor);
        }

        private void ExitTargetingMode()
        {
            currentState = InputState.Normal;
            pendingTargetedAbility = null;
            reticleView?.Hide();
        }

        /// <summary>Exits targeting without inventory restore (e.g. active member died).</summary>
        public void ForceExitTargeting()
        {
            if (currentState != InputState.Targeting)
                return;

            ExitTargetingMode();
        }

        static bool TryAllowPlayerAbilitySource(HotbarResolvedAction resolved, out string denyReason)
        {
            if (resolved.Source == PlayerAbilitySource.RacialActive
                && HotbarResolver.IsElementalSpiritActiveBinding(resolved.RacialBindingKey))
            {
                denyReason = null;
                return true;
            }

            return TryAllowPlayerAbilitySource(resolved.Source, out denyReason);
        }

        static bool TryAllowPlayerAbilitySource(PlayerAbilitySource source, out string denyReason)
        {
            denyReason = null;
            if (source == PlayerAbilitySource.InventoryItem)
                return true;

            if (source == PlayerAbilitySource.Essence)
                return SafeZonePolicyService.TryAllowEssenceAbility(out denyReason);

            return SafeZonePolicyService.TryAllowHostileAction(out denyReason);
        }

        static bool TryAllowPendingTargetedAction(PendingTargetedAbility pending, out string denyReason)
        {
            denyReason = null;

            if (pending.Source == PlayerAbilitySource.InventoryItem)
            {
                ItemData item = pending.InventoryItemInstance?.Definition;
                return SafeZonePolicyService.TryAllowInventoryUse(item, out denyReason);
            }

            if (pending.Source == PlayerAbilitySource.RacialActive)
                return SafeZonePolicyService.TryAllowHostileAction(out denyReason);

            return TryAllowPlayerAbilitySource(pending.Source, out denyReason);
        }

        static bool TryExecuteMageSpellWithProficiency(
            BaseActor actor,
            HumanMageSpellsRuntime runtime,
            int equippedIndex,
            Vector3Int? targetTile = null)
        {
            if (actor == null || runtime == null)
                return false;

            bool ok = targetTile.HasValue
                ? runtime.TryExecuteEquipped(equippedIndex, actor.gameObject, targetTile.Value)
                : runtime.TryExecuteEquipped(equippedIndex, actor.gameObject);

            if (ok)
                ProficiencyAwardService.AwardMageSpellCast(actor, runtime, equippedIndex);

            return ok;
        }

        static bool TryExecuteDragonianSpellWithProficiency(
            BaseActor actor,
            DragonianSpellsRuntime runtime,
            int memorizedIndex,
            Vector3Int? targetTile = null)
        {
            if (actor == null || runtime == null)
                return false;

            bool ok = targetTile.HasValue
                ? runtime.TryExecuteMemorized(memorizedIndex, actor.gameObject, targetTile.Value)
                : runtime.TryExecuteMemorized(memorizedIndex, actor.gameObject);

            if (ok)
                ProficiencyAwardService.AwardDragonianSpellCast(actor, runtime, memorizedIndex);

            return ok;
        }
    }
}
