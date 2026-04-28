using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Core.Actor;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Essence;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JRogue.Input
{
    public enum InputState { Normal, Targeting }
    public class InputHandler : MonoBehaviour
    {
        private GameControls controls;
        private EssenceSlotManager essenceManager;

        private InputState currentState = InputState.Normal;
        private Vector3Int reticlePosition;
        private AbilityAction pendingAbility;

        [Header("Targeting Visuals")]
        [SerializeField] private GameObject reticlePrefab;
        private GameObject activeReticle;

        private void Awake()
        {
            controls = new GameControls();
            essenceManager = GetComponent<EssenceSlotManager>();

            // Link the three new action layers to one handler
            controls.Player.PrimaryAbilities.performed += ctx => OnAbilityPerformed(ctx, false, false);
            controls.Player.ShiftAbilities.performed += ctx => OnAbilityPerformed(ctx, true, false);
            controls.Player.CtrlAbilities.performed += ctx => OnAbilityPerformed(ctx, false, true);
            controls.Player.Confirm.performed += OnConfirm;
            controls.Player.Cancel.performed += OnCancel;

            // Party Member Selection
            controls.Player.SelectPartyMember.performed += ctx => SwapTo(ctx);
            // controls.Player.SelectMember1.performed += _ => SwapTo(0);
            // controls.Player.SelectMember2.performed += _ => SwapTo(1);
            // controls.Player.SelectMember3.performed += _ => SwapTo(2);
        }

        private void OnEnable() => controls.Player.Enable();
        private void OnDisable() => controls.Player.Disable();

        public void OnMove(InputAction.CallbackContext context)
        {
            // --- RESTORED: Input system check ---
            if (!context.performed) return;
            if (TurnManager.Instance.currentState != GameState.PLAYER_TURN) return;

            Vector2 input = context.ReadValue<Vector2>();
            Vector3Int direction = new Vector3Int(Mathf.RoundToInt(input.x), Mathf.RoundToInt(input.y), 0);

            // --- RESTORED: Targeting Mode logic ---
            if (currentState == InputState.Targeting)
            {
                MoveReticle(direction);
            }
            else if (direction != Vector3Int.zero)
            {
                BaseActor activeMember = PartyManager.Instance.GetActiveMember();
                if (activeMember == null) return;

                if (!TurnManager.Instance.CanActorTakeAction(activeMember.gameObject)) return;

                Vector3Int targetTile = activeMember.GridPosition + direction;
                Vector3Int oldPosition = activeMember.GridPosition;

                IBattleTarget occupant = GridManager.Instance.GetActorAt(targetTile);

                // --- FIXED: Proper declaration for the swappable ally ---
                BaseActor swappableAlly = null;
                bool isAllySwap = occupant is BaseActor actor && PartyManager.Instance.partyMembers.Contains(actor);
                if (isAllySwap) swappableAlly = (BaseActor)occupant;

                // Requirement Check: Is this a bump against an enemy?
                bool isEnemyBump = occupant != null && !isAllySwap;

                if (PartyManager.Instance.isFormationActive)
                {
                    // We allow the action if it's a valid move OR an enemy bump
                    if (isEnemyBump || IsValidMove(targetTile, new Dictionary<BaseActor, Vector3Int>(), true))
                    {
                        if (activeMember.TryMove(direction))
                        {
                            // Only record breadcrumbs if the position actually changed (prevents clustering)
                            if (activeMember.GridPosition != oldPosition)
                            {
                                PartyManager.Instance.RecordNewLeaderPosition(activeMember.GridPosition);
                            }
                            else
                            {
                                Debug.Log($"[FORMATION-BUMP] Leader attacked at {targetTile}. Position stayed {oldPosition}.");
                            }

                            // CRITICAL: Trigger Rush so followers catch up during the attack
                            ProcessFollowerRush();
                        }
                    }
                }
                else
                {
                    // --- RESTORED: Manual Mode Atomic Swap ---
                    if (isAllySwap && swappableAlly != null && IsValidMove(targetTile, new Dictionary<BaseActor, Vector3Int>(), true))
                    {
                        GridManager.Instance.UnregisterActor(activeMember.GridPosition);
                        GridManager.Instance.UnregisterActor(swappableAlly.GridPosition);

                        activeMember.ApplyPositionChange(targetTile);
                        swappableAlly.ApplyPositionChange(oldPosition);

                        Debug.Log($"[MANUAL-SWAP] {activeMember.name} swapped with {swappableAlly.name}");
                        TurnManager.Instance.OnPlayerActionComplete(activeMember.gameObject);
                    }
                    else if (activeMember.TryMove(direction))
                    {
                        TurnManager.Instance.OnPlayerActionComplete(activeMember.gameObject);
                    }
                }
            }
        }

        // public void OnMove(InputAction.CallbackContext context)
        // {
        //     if (!context.performed) return;
        //     if (TurnManager.Instance.currentState != GameState.PLAYER_TURN) return;

        //     Vector2 input = context.ReadValue<Vector2>();
        //     Vector3Int direction = new Vector3Int(Mathf.RoundToInt(input.x), Mathf.RoundToInt(input.y), 0);

        //     if (currentState == InputState.Targeting)
        //     {
        //         MoveReticle(direction);
        //     }
        //     else if (direction != Vector3Int.zero)
        //     {
        //         BaseActor activeMember = PartyManager.Instance.GetActiveMember();
        //         if (activeMember == null) return;

        //         if (!TurnManager.Instance.CanActorTakeAction(activeMember.gameObject)) return;

        //         Vector3Int targetTile = activeMember.GridPosition + direction;
        //         Vector3Int oldPosition = activeMember.GridPosition;

        //         IBattleTarget occupant = GridManager.Instance.GetActorAt(targetTile);
        //         bool isAllySwap = occupant is BaseActor ally && PartyManager.Instance.partyMembers.Contains(ally);

        //         if (IsValidMove(targetTile, new Dictionary<BaseActor, Vector3Int>(), true))
        //         {
        //             if (PartyManager.Instance.isFormationActive)
        //             {
        //                 // Move the leader first
        //                 if (activeMember.TryMove(direction))
        //                 {
        //                     PartyManager.Instance.RecordNewLeaderPosition(activeMember.GridPosition);

        //                     // CRITICAL: We do NOT end the turn here anymore. 
        //                     // We let ProcessFollowerRush handle the transition.
        //                     ProcessFollowerRush();
        //                 }
        //             }
        //             else
        //             {
        //                 // MANUAL MODE ATOMIC SWAP
        //                 if (isAllySwap && occupant is BaseActor swappableAlly)
        //                 {
        //                     GridManager.Instance.UnregisterActor(activeMember.GridPosition);
        //                     GridManager.Instance.UnregisterActor(swappableAlly.GridPosition);

        //                     activeMember.ApplyPositionChange(targetTile);
        //                     swappableAlly.ApplyPositionChange(oldPosition);

        //                     Debug.Log($"[MANUAL-SWAP] {activeMember.name} moved to {targetTile}, pushed {swappableAlly.name} to {oldPosition}");
        //                     TurnManager.Instance.OnPlayerActionComplete(activeMember.gameObject);
        //                 }
        //                 else
        //                 {
        //                     if (activeMember.TryMove(direction))
        //                     {
        //                         TurnManager.Instance.OnPlayerActionComplete(activeMember.gameObject);
        //                     }
        //                 }
        //             }
        //         }
        //     }
        // }

        public void OnWait(InputAction.CallbackContext context)
        {
            // Only trigger on the initial press and during the Player's turn
            if (!context.performed || TurnManager.Instance.currentState != GameState.PLAYER_TURN) return;

            BaseActor activeMember = PartyManager.Instance.GetActiveMember();
            if (activeMember == null) return;

            // --- THE FIX ---
            // If the character has already acted this turn, ignore the input
            if (!TurnManager.Instance.CanActorTakeAction(activeMember.gameObject))
            {
                Debug.Log($"{activeMember.name} has already moved! Switch characters or end turn.");
                return;
            }
            // ----------------

            // Check if Shift is held for "Party Wait" (You'll need to check your Input bindings)
            bool isPartyWait = Keyboard.current.shiftKey.isPressed;

            if (isPartyWait)
            {
                Debug.Log("Party is skipping turns...");
                // If they are in Auto mode, let them rush one step while waiting
                if (PartyManager.Instance.isFormationActive)
                {
                    ProcessFollowerRush();
                }

                // Immediately end the player phase
                TurnManager.Instance.ForceEndPlayerTurn();
            }
            else
            {
                Debug.Log($"{activeMember.name} is skipping turn...");
                // If in Auto mode, others rush even if leader waits
                // Even if only the leader waits, followers in "Auto" mode still move
                if (PartyManager.Instance.isFormationActive)
                {
                    ProcessFollowerRush();

                    // In Formation mode, a Leader's wait typically ends the whole turn 
                    // because they are the "Clock" for the squad.
                    TurnManager.Instance.OnPlayerActionComplete(activeMember.gameObject);
                }
                else
                {
                    // In Manual mode, only this character is marked as "Done"
                    TurnManager.Instance.OnPlayerActionComplete(activeMember.gameObject);
                }
            }
        }

        public void OnConfirm(InputAction.CallbackContext context)
        {
            if (!context.performed || currentState != InputState.Targeting) return;

            BaseActor activeMember = PartyManager.Instance.GetActiveMember();
            if (activeMember == null || pendingAbility == null) return;

            // Execute the ability at the reticle position
            if (pendingAbility.Execute(activeMember.gameObject, reticlePosition))
            {
                currentState = InputState.Normal;
                pendingAbility = null;
                ExitTargetingMode();

                if (PartyManager.Instance.isFormationActive)
                {
                    // 1. Sync the history to the Leader's NEW teleported position
                    // This ensures the followers' "target slots" are updated to the teleport destination

                    // DO NOT call RecordNewLeaderPosition here for stationary abilities!
                    // Just rush so laggards can finish catching up to the existing line.
                    PartyManager.Instance.RecordNewLeaderPosition(activeMember.GridPosition);

                    // 2. (Optional) Run the rush so followers move toward the new spot immediately
                    ProcessFollowerRush();

                    // 3. End the squad turn
                    TurnManager.Instance.ForceEndPlayerTurn();
                }
                else
                {
                    TurnManager.Instance.OnPlayerActionComplete(activeMember.gameObject);
                }

                Debug.Log($"Teleport/Targeted Ability executed. Leader now at: {activeMember.GridPosition}");
            }
        }

        // You will need to bind this to a 'Confirm' action (e.g., Space or Enter) in your GameControls
        public void OnCancel(InputAction.CallbackContext context)
        {
            if (!context.performed || currentState != InputState.Targeting) return;

            BaseActor activeMember = PartyManager.Instance.GetActiveMember();
            if (activeMember == null || pendingAbility == null) return;

            // Execute the ability at the reticle position
            if (currentState == InputState.Targeting)
            {
                currentState = InputState.Normal;
                pendingAbility = null;
                ExitTargetingMode();
                // TurnManager.Instance.OnPlayerActionComplete();
                Debug.Log("Targeted Ability Cancelled.");
            }
        }

        // public void ProcessFollowerRush()
        // {
        //     BaseActor leader = PartyManager.Instance.GetActiveMember();
        //     var members = PartyManager.Instance.partyMembers;

        //     for (int i = 0; i < members.Count; i++)
        //     {
        //         // Skip the leader
        //         if (members[i] == leader) continue;

        //         // 1. Calculate path to the leader or breadcrumb
        //         // 2. Move the follower one step closer
        //         Debug.Log($"{members[i].name} is rushing toward the formation.");

        //         // Logic will eventually look like:
        //         // Vector3Int nextStep = Pathfinding.GetNextStep(member.GridPosition, leader.GridPosition);
        //         // member.ApplyPositionChange(nextStep);
        //     }
        // }

        private void OnAbilityPerformed(InputAction.CallbackContext context, bool isShift, bool isCtrl)
        {
            // Consistency Check: Match your OnMove turn check exactly
            if (!context.performed) return;
            if (TurnManager.Instance.currentState != GameState.PLAYER_TURN) return;

            string keyName = context.control.name;

            if (int.TryParse(keyName, out int numberPressed))
            {
                int slotIndex = numberPressed - 1;

                // Fetch the actor currently controlled by the player
                BaseActor activeMember = PartyManager.Instance.GetActiveMember();

                if (activeMember != null)
                {
                    ProcessAbilityInput(activeMember, slotIndex, isShift, isCtrl);
                }
            }
        }

        private void ProcessAbilityInput(BaseActor actor, int slotIndex, bool isShift, bool isCtrl)
        {
            // Get the manager from the ACTIVE member, not 'this' gameObject
            var essenceManager = actor.GetComponent<EssenceSlotManager>();
            // if (essenceManager == null) return;
            var equipManager = actor.GetComponent<EquipmentManager>();

            AbilityAction abilityToTry = null;
            // bool success = false;

            // Determine which ability we are looking at
            int abilityIndex = isShift ? 1 : 0;
            if (isCtrl) abilityToTry = equipManager?.GetItemAbility(slotIndex, abilityIndex);
            else abilityToTry = essenceManager?.GetAbility(slotIndex, abilityIndex);

            if (abilityToTry == null) return;

            // Check if it's a Targeted ability (We'll define this in the AbilityAction class next)
            if (abilityToTry.requiresTarget)
            {
                EnterTargetingMode(actor, abilityToTry);
            }
            else if (abilityToTry.CanExecute(actor.gameObject))
            {
                if (abilityToTry.Execute(actor.gameObject))
                {
                    TurnManager.Instance.OnPlayerActionComplete(actor.gameObject);
                }
            }

            // if (isCtrl)
            // {
            //     // We use isShift to decide which ability index within the ITEM to fire
            //     int abilityIndex = isShift ? 1 : 0;
            //     success = equipManager.TryExecuteItemAbility(slotIndex, abilityIndex);
            // }
            // else if (isShift)
            // {
            //     // Second ability of the Essence in this slot
            //     success = essenceManager.TryExecuteAbility(slotIndex, 1);
            // }
            // else
            // {
            //     // Primary ability of the Essence in this slot
            //     success = essenceManager.TryExecuteAbility(slotIndex, 0);
            // }

            // if (success)
            // {
            //     // Consistency: Use your existing turn-ending method
            //     TurnManager.Instance.OnPlayerActionComplete();
            // }
        }

        private void EnterTargetingMode(BaseActor actor, AbilityAction ability)
        {
            currentState = InputState.Targeting;
            pendingAbility = ability;
            // Start the reticle at the player's current grid position
            //reticlePosition = Vector3Int.RoundToInt(actor.transform.position);
            reticlePosition = actor.GridPosition;

            Debug.Log($"Entered Targeting Mode for {ability.abilityName}. Move reticle, then confirm.");

            // Instantiate the visual reticle
            if (activeReticle == null)
            {
                activeReticle = Instantiate(reticlePrefab);
            }
            activeReticle.SetActive(true);
            UpdateReticleVisuals();

            Debug.Log($"Targeting: {ability.abilityName}");
        }

        private void MoveReticle(Vector3Int direction)
        {
            // Update the logical position
            reticlePosition += direction;
            UpdateReticleVisuals();

            // TODO: Update the visual position of a reticle sprite/object here
            Debug.Log($"Targeting Reticle moved to: {reticlePosition}");
        }

        private void UpdateReticleVisuals()
        {
            if (activeReticle != null)
            {
                // Align with your grid (adding 0.5f to center on tile)
                activeReticle.transform.position = new Vector3(reticlePosition.x + 0.5f, reticlePosition.y + 0.5f, 0);
            }
        }

        private void ExitTargetingMode()
        {
            currentState = InputState.Normal;
            pendingAbility = null;
            if (activeReticle != null) activeReticle.SetActive(false);
        }

        private void PerformIndividualWait(BaseActor actor)
        {
            Debug.Log($"{actor.name} is waiting...");

            // In "Auto" mode, even if the leader waits, the followers still get to "Rush"
            if (PartyManager.Instance.isFormationActive)
            {
                ProcessFollowerRush();
            }

            TurnManager.Instance.OnPlayerActionComplete(actor.gameObject);
        }

        private void PerformPartyWait()
        {
            Debug.Log("Entire party is waiting...");

            // Logic for skipping everyone's turn simultaneously
            // This effectively ends the Player Turn phase entirely
            //TurnManager.Instance.OnPlayerActionComplete();
            // Requirement: In Auto mode, making the leader wait ends the WHOLE party turn
            TurnManager.Instance.ForceEndPlayerTurn();
        }

        private void SwapTo(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            string keyName = context.control.name;
            // Should be F1,F2,F3,F4,F5; hack it for now
            string prefix = "F";
            string suffix = keyName.Replace(prefix, "").Replace(prefix.ToLower(), ""); // Handle both F1 and f1
            int index;

            if (int.TryParse(suffix, out int numberPressed))
            {
                index = numberPressed - 1;
            }
            else
            {
                Debug.LogWarning($"Unrecognized key for party swap: {keyName}");
                return;
            }

            // 1. Tell the PartyManager to change the activeIndex
            PartyManager.Instance.SwapActiveMember(index);

            // 2. NEW: SNAP the history to the new arrangement
            // This prevents the party from trying to rush toward the OLD leader's trail.
            PartyManager.Instance.SnapHistoryToCurrentPositions();

            // 3. Camera Tracking
            BaseActor newActive = PartyManager.Instance.GetActiveMember();
            if (newActive != null)
            {
                // Use your camera's follow method
                // Camera.main.GetComponent<CameraFollow>()?.SetTarget(newActive.transform);
                Debug.Log($"[SWAP] Now controlling {newActive.name}. Camera following and History Snapped.");
            }
        }

        private void ProcessFollowerRush()
        {
            var party = PartyManager.Instance.partyMembers;
            var history = PartyManager.Instance.positionHistory;
            BaseActor leader = PartyManager.Instance.GetActiveMember();

            Debug.Log($"[RUSH] Starting Rush. Party: {party.Count}, History: {history.Count}");

            int maxRushDistance = 2;

            // 1. TOTAL LIFT - Your original logic
            foreach (var member in party)
            {
                GridManager.Instance.UnregisterActor(member.GridPosition);
            }

            // 2. LOCK LEADER - Your original logic
            GridManager.Instance.RegisterActor(leader.GridPosition, leader);

            Dictionary<BaseActor, Vector3Int> plannedMoves = new Dictionary<BaseActor, Vector3Int>();

            // 3. THE PLAN
            for (int i = 1; i < party.Count; i++)
            {
                BaseActor follower = party[i];
                Vector3Int historicalTarget = (i < history.Count) ? history[i] : follower.GridPosition;

                Vector3Int finalTarget = follower.GridPosition;
                float dist = Vector3Int.Distance(follower.GridPosition, historicalTarget);

                // Your distance-based target selection
                if (dist <= maxRushDistance)
                    finalTarget = historicalTarget;
                else
                {
                    Vector3 direction = ((Vector3)(historicalTarget - follower.GridPosition)).normalized;
                    finalTarget = Vector3Int.RoundToInt((Vector3)follower.GridPosition + (direction * maxRushDistance));
                }

                if (IsValidMove(finalTarget, plannedMoves))
                {
                    plannedMoves.Add(follower, finalTarget);
                    Debug.Log($"[RUSH-PLAN] {follower.name} accepted target {finalTarget}");
                }
                else
                {
                    // Your Smart Step search logic
                    Vector3Int bestSmartTile = follower.GridPosition;
                    float bestDistToBreadcrumb = float.MaxValue;
                    bool foundSpot = false;

                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            if (x == 0 && y == 0) continue;
                            Vector3Int neighbor = finalTarget + new Vector3Int(x, y, 0);
                            if (Vector3Int.Distance(follower.GridPosition, neighbor) > maxRushDistance + 0.5f) continue;

                            if (IsValidMove(neighbor, plannedMoves))
                            {
                                float d = Vector3Int.Distance(neighbor, historicalTarget);
                                if (d < bestDistToBreadcrumb)
                                {
                                    bestDistToBreadcrumb = d;
                                    bestSmartTile = neighbor;
                                    foundSpot = true;
                                }
                            }
                        }
                    }
                    plannedMoves.Add(follower, foundSpot ? bestSmartTile : follower.GridPosition);
                }
            }

            // 4. THE LAND
            foreach (var move in plannedMoves)
            {
                BaseActor actor = move.Key;
                Vector3Int dest = move.Value;

                if (actor.GridPosition != dest)
                {
                    Debug.Log($"[RUSH-LAND] {actor.name} moving {actor.GridPosition} -> {dest}");
                    actor.ApplyPositionChange(dest);
                }
                else
                {
                    GridManager.Instance.RegisterActor(actor.GridPosition, actor);
                }

                // Signal that this follower has finished their part of the turn
                TurnManager.Instance.OnPlayerActionComplete(actor.gameObject);
            }

            // Signal leader completion
            TurnManager.Instance.OnPlayerActionComplete(leader.gameObject);

            Debug.Log("[RUSH-COMPLETE] Grid synchronized. Ending player turn.");
            TurnManager.Instance.ForceEndPlayerTurn();
        }

        // private void ProcessFollowerRush()
        // {
        //     var party = PartyManager.Instance.partyMembers;
        //     var history = PartyManager.Instance.positionHistory;
        //     BaseActor leader = PartyManager.Instance.GetActiveMember();

        //     Debug.Log($"[RUSH] Starting Rush. Party: {party.Count}, History: {history.Count}");

        //     int maxRushDistance = 2;

        //     // 1. TOTAL LIFT - Clear the whole party from the grid
        //     foreach (var member in party)
        //     {
        //         GridManager.Instance.UnregisterActor(member.GridPosition);
        //     }

        //     // 2. LOCK LEADER - Immediately re-register leader at their NEW position
        //     // This ensures no follower or enemy can claim this tile during the Rush calculation
        //     GridManager.Instance.RegisterActor(leader.GridPosition, leader);

        //     Dictionary<BaseActor, Vector3Int> plannedMoves = new Dictionary<BaseActor, Vector3Int>();
        //     // We don't add leader to plannedMoves because they are already 'Landed'

        //     // 3. THE PLAN (Skip index 0 as it's the leader)
        //     for (int i = 1; i < party.Count; i++)
        //     {
        //         BaseActor follower = party[i];
        //         Vector3Int historicalTarget = (i < history.Count) ? history[i] : follower.GridPosition;

        //         Vector3Int finalTarget = follower.GridPosition;
        //         float dist = Vector3Int.Distance(follower.GridPosition, historicalTarget);

        //         if (dist <= maxRushDistance)
        //             finalTarget = historicalTarget;
        //         else
        //         {
        //             Vector3 direction = ((Vector3)(historicalTarget - follower.GridPosition)).normalized;
        //             finalTarget = Vector3Int.RoundToInt((Vector3)follower.GridPosition + (direction * maxRushDistance));
        //         }

        //         if (IsValidMove(finalTarget, plannedMoves))
        //         {
        //             plannedMoves.Add(follower, finalTarget);
        //             Debug.Log($"[RUSH-PLAN] {follower.name} accepted target {finalTarget}");
        //         }
        //         else
        //         {
        //             // Smart Step logic
        //             Vector3Int bestSmartTile = follower.GridPosition;
        //             float bestDistToBreadcrumb = float.MaxValue;
        //             bool foundSpot = false;

        //             for (int x = -1; x <= 1; x++)
        //             {
        //                 for (int y = -1; y <= 1; y++)
        //                 {
        //                     if (x == 0 && y == 0) continue;
        //                     Vector3Int neighbor = finalTarget + new Vector3Int(x, y, 0);
        //                     if (Vector3Int.Distance(follower.GridPosition, neighbor) > maxRushDistance + 0.5f) continue;

        //                     if (IsValidMove(neighbor, plannedMoves))
        //                     {
        //                         float d = Vector3Int.Distance(neighbor, historicalTarget);
        //                         if (d < bestDistToBreadcrumb)
        //                         {
        //                             bestDistToBreadcrumb = d;
        //                             bestSmartTile = neighbor;
        //                             foundSpot = true;
        //                         }
        //                     }
        //                 }
        //             }
        //             plannedMoves.Add(follower, foundSpot ? bestSmartTile : follower.GridPosition);
        //             Debug.Log($"[RUSH-SMART] {follower.name} directed to {plannedMoves[follower]}");
        //         }
        //     }

        //     // 4. THE LAND
        //     foreach (var move in plannedMoves)
        //     {
        //         BaseActor actor = move.Key;
        //         Vector3Int dest = move.Value;

        //         if (actor.GridPosition != dest)
        //         {
        //             Debug.Log($"[RUSH-LAND] {actor.name} moving {actor.GridPosition} -> {dest}");
        //             actor.ApplyPositionChange(dest);
        //         }
        //         else
        //         {
        //             GridManager.Instance.RegisterActor(actor.GridPosition, actor);
        //         }
        //     }

        //     Debug.Log("[RUSH-COMPLETE] Grid synchronized. Ending player turn.");
        //     TurnManager.Instance.ForceEndPlayerTurn();
        // }

        private bool IsSwappableAlly(Vector3Int tile)
        {
            IBattleTarget occupant = GridManager.Instance.GetActorAt(tile);
            if (occupant is BaseActor actor)
            {
                // Check if the actor is part of our party
                return PartyManager.Instance.partyMembers.Contains(actor);
            }
            return false;
        }

        private bool IsValidMove(Vector3Int tile, Dictionary<BaseActor, Vector3Int> plannedMoves, bool allowAllies = false)
        {
            if (MapManager.Instance.IsWalkable(tile) == false) return false;

            IBattleTarget occupant = GridManager.Instance.GetActorAt(tile);
            if (occupant != null)
            {
                // If this is the Leader's manual move (allowAllies is true), 
                // we return true so they can reach OnBump().
                if (allowAllies) return true;

                // If this is a Follower's Rush (allowAllies is false),
                // we return false because followers shouldn't 'Rush' into enemies.
                return false;
            }

            if (plannedMoves.ContainsValue(tile)) return false;
            return true;
        }

        // private bool IsValidMove(Vector3Int tile, Dictionary<BaseActor, Vector3Int> plannedMoves, bool allowAllies = false)
        // {
        //     // Check Map (Walls/Void) - ALWAYS a hard block
        //     if (MapManager.Instance.IsWalkable(tile) == false) return false;

        //     // Check Actors
        //     IBattleTarget occupant = GridManager.Instance.GetActorAt(tile);
        //     if (occupant != null)
        //     {
        //         if (allowAllies && IsSwappableAlly(tile))
        //         {
        //             return true; // We can move here, but we'll need to swap
        //         }
        //         return false; // Enemy or non-swappable
        //     }

        //     // Check Teammate Claims (used during Rush)
        //     if (plannedMoves.ContainsValue(tile)) return false;

        //     return true;
        // }

        private Vector3Int ProcessInput(Vector2 input)
        {
            if (input.x != 0) return new Vector3Int(input.x > 0 ? 1 : -1, 0, 0);
            if (input.y != 0) return new Vector3Int(0, input.y > 0 ? 1 : -1, 0);
            return Vector3Int.zero;
        }
    }
}