using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Data.Progression;
using JRogue.Manager.Floor;
using JRogue.Manager.Loot;
using JRogue.Manager.Progression;
using JRogue.View;
using UnityEngine;

namespace JRogue.Manager.Party
{
    public class PartyManager : MonoBehaviour
    {
        public static PartyManager Instance;

        [Header("Progression")]
        [SerializeField] ExperienceCurve experienceCurve;

        [Header("Party Members")]
        // The list of all your JRPG party members
        public List<BaseActor> partyMembers = new List<BaseActor>();
        private int activeIndex = 0;

        [Header("Main Character")]
        [SerializeField, Tooltip("Optional explicit main character. Otherwise uses PartyMainCharacterMarker on a member.")]
        BaseActor mainCharacterOverride;

        BaseActor mainCharacter;

        [Header("Merchant (future)")]
        [SerializeField, Tooltip("Party member index that talks to shops (single shopper per requirements).")]
        private int activeShopperMemberIndex;

        [Header("Formation Settings")]
        [SerializeField]
        // Requirement: Toggle between Follow-the-leader and Manual control
        private bool isFormationActive = true;

        // This will store the leader's path for followers to "Rush" into
        // private Queue<Vector3Int> breadcrumbTrail = new Queue<Vector3Int>();
        // INDEX 0: Leader's current tile
        // INDEX 1: Tile leader was on 1 move ago (Follower 1's slot)
        // INDEX 2: Tile leader was on 2 moves ago (Follower 2's slot)
        [Header("Formation History")]
        public List<Vector3Int> positionHistory = new List<Vector3Int>();
        // private const int MAX_BREADCRUMBS = 10;

        public bool IsFormationActive
        {
            get => isFormationActive;
        }

        public BaseActor MainCharacter => mainCharacter;

        public bool HasMainCharacter => mainCharacter != null;

        public bool IsMainCharacter(BaseActor actor) =>
            mainCharacter != null && actor != null && mainCharacter == actor;

        public bool IsMainCharacter(GameObject go)
        {
            if (go == null || mainCharacter == null)
                return false;

            return mainCharacter.gameObject == go;
        }

        /// <summary>Index into <see cref="partyMembers"/> for the current merchant UI (not yet wired).</summary>
        public int ActiveShopperMemberIndex
        {
            get => Mathf.Clamp(activeShopperMemberIndex, 0, Mathf.Max(0, partyMembers.Count - 1));
            set => activeShopperMemberIndex = value;
        }

        // public bool ToggleFormationActive() =>
        //     isFormationActive = !isFormationActive;

        // Modified: Now automatically snaps history when toggled on
        public bool ToggleFormationActive()
        {
            isFormationActive = !isFormationActive;
            if (isFormationActive)
            {
                SnapHistoryToCurrentPositions();
            }
            return isFormationActive;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (experienceCurve == null)
                experienceCurve = Resources.Load<ExperienceCurve>("Progression/DefaultExperienceCurve");

            PartyExperienceService xp = GetComponent<PartyExperienceService>();
            if (xp == null)
                xp = gameObject.AddComponent<PartyExperienceService>();
            xp.Configure(this, experienceCurve);

            EnsureComponent<FloorItemPileService>();
            EnsureComponent<FloorEssenceService>();
            EnsureComponent<EnemyLootService>();
            EnsureComponent<PartyManaStoneLedger>();
            EnsureComponent<PartyCurrencyLedger>();
            EnsureComponent<ManaStoneAutoPickupService>();
            EnsureComponent<PartyRestState>();
            EnsureComponent<RestSessionService>();
        }

        void EnsureComponent<T>() where T : Component
        {
            if (GetComponent<T>() == null)
                gameObject.AddComponent<T>();
        }

        void Start()
        {
            // Clear and force-sync history to actual actor positions
            positionHistory.Clear();
            Debug.Log($"[START-INIT] Syncing {partyMembers.Count} members to history.");

            for (int i = 0; i < partyMembers.Count; i++)
            {
                if (partyMembers[i] == null)
                {
                    Debug.LogError(
                        $"[PartyManager] partyMembers[{i}] is null (missing prefab reference or stripped component). Fix PartyManager list in the scene.");
                    continue;
                }

                Vector3Int actPos = partyMembers[i].GridPosition;
                positionHistory.Add(actPos);
                Debug.Log($"[START-INIT] Slot [{i}] ({partyMembers[i].name}) set to {actPos}");
            }

            SnapHistoryToCurrentPositions();
            ManaStoneAutoPickupService.Instance?.SubscribePartyMembers();
            BootstrapMainCharacterDesignation();
            RefreshCameraFollow();
        }

        /// <summary>
        /// When the roster is created after <see cref="Start"/> (e.g. dungeon generate), run services that
        /// <see cref="Start"/> would have wired with an empty list.
        /// </summary>
        public void InitializeRosterAfterDeferredSpawn()
        {
            ManaStoneAutoPickupService.Instance?.SubscribePartyMembers();
            BootstrapMainCharacterDesignation();
        }

        /// <summary>Points the main camera at the currently controlled party member.</summary>
        public void RefreshCameraFollow()
        {
            BaseActor active = GetActiveMember();
            if (active == null)
                return;

            CameraFollow cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null)
                cam.SetTarget(active.transform);
        }

        /// <summary>One-time main character designation from override or marker (immutable after success).</summary>
        public bool TryDesignateMainCharacter(BaseActor actor)
        {
            const string logPrefix = "[GameOver]";

            if (mainCharacter != null)
            {
                Debug.Log($"{logPrefix} Cannot designate {actor?.name}: main character already set ({mainCharacter.DisplayName}).");
                return false;
            }

            if (actor == null)
                return false;

            if (!partyMembers.Contains(actor))
            {
                Debug.LogWarning($"{logPrefix} Cannot designate {actor.name}: not in partyMembers.");
                return false;
            }

            mainCharacter = actor;
            Debug.Log(
                $"{logPrefix} Main character designated: {mainCharacter.DisplayName} ({mainCharacter.gameObject.name}).");
            return true;
        }

        void BootstrapMainCharacterDesignation()
        {
            if (HasMainCharacter)
                return;

            if (mainCharacterOverride != null)
            {
                TryDesignateMainCharacter(mainCharacterOverride);
                return;
            }

            for (int i = 0; i < partyMembers.Count; i++)
            {
                BaseActor member = partyMembers[i];
                if (member == null)
                    continue;

                if (member.GetComponent<PartyMainCharacterMarker>() != null)
                {
                    TryDesignateMainCharacter(member);
                    return;
                }
            }

            Debug.LogWarning(
                "[GameOver] No main character designated. Add PartyMainCharacterMarker or assign mainCharacterOverride.");
        }

        public void RecordNewLeaderPosition(Vector3Int newPos)
        {
            // 1. STATIONARY CHECK
            if (positionHistory.Count > 0 && positionHistory[0] == newPos)
            {
                // Helpful to know if the logic is skipping because the leader bumped into a wall
                Debug.Log($"[RECORD-SKIP] Leader stationary at {newPos}. History preserved.");
                return;
            }

            Debug.Log($"[RECORD-START] Leader moving to {newPos}. Shifting history for {partyMembers.Count} members.");

            // The leader's OLD position (history[0]) is what the first follower (index 1) will target.
            Vector3Int carryPos = positionHistory[0];

            // Update the leader's current slot
            Debug.Log($"[RECORD-LEADER] Index [0] updated: {positionHistory[0]} -> {newPos}");
            positionHistory[0] = newPos;

            // 2. SHIFT LOOP
            for (int i = 1; i < partyMembers.Count; i++)
            {
                if (i < positionHistory.Count)
                {
                    Vector3Int oldHistoryPos = positionHistory[i];

                    // Log the hand-off: CarryPos is the tile vacated by the person in front
                    Debug.Log($"[RECORD-SHIFT] Index [{i}] ({partyMembers[i].name}) receiving breadcrumb {carryPos}. (Old was {oldHistoryPos})");

                    positionHistory[i] = carryPos;
                    carryPos = oldHistoryPos;
                }
                else
                {
                    // This handles cases where the history list was shorter than the party list
                    Vector3Int memberPos = partyMembers[i].GridPosition;
                    Debug.LogWarning($"[RECORD-PAD] History index [{i}] was missing. Padding with member's current pos: {memberPos}");
                    positionHistory.Add(memberPos);
                }
            }

            PrintHistoryReport("SHIFT-COMPLETE");
        }

        // public void RecordNewLeaderPosition(Vector3Int newPos)
        // {
        //     // --- THE GATEKEEPER ---
        //     // If the leader is still at the same spot, do nothing and return.
        //     // This prevents index 0 and 1 from becoming the same coordinate, which causes clustering.
        //     if (positionHistory.Count > 0 && positionHistory[0] == newPos)
        //     {
        //         // Keeping a log here so you know WHY the shift didn't happen
        //         Debug.Log($"[RECORD-SKIP] Leader stationary at {newPos}. History preserved.");
        //         return;
        //     }

        //     Debug.Log($"[RECORD-START] Leader stepped onto: {newPos}. Current History Count: {positionHistory.Count}");
        //     List<Vector3Int> nextHistory = new List<Vector3Int>();

        //     nextHistory.Add(newPos);

        //     for (int i = 0; i < partyMembers.Count - 1; i++)
        //     {
        //         if (i < positionHistory.Count)
        //         {
        //             Vector3Int oldPos = positionHistory[i];

        //             // Your original massive teleportation jump check
        //             if (i > 0 && Vector3Int.Distance(oldPos, nextHistory[i]) > 1.5f)
        //             {
        //                 Debug.LogError($"[SANITY-FAIL] Index {i + 1} is receiving {oldPos}, which is too far from {nextHistory[i]}!");
        //             }

        //             Debug.Log($"[RECORD-SHIFT] Index {i} -> {i + 1}. Carrying Pos: {oldPos}");
        //             nextHistory.Add(oldPos);
        //         }
        //         else
        //         {
        //             Vector3Int fallback = partyMembers[i].GridPosition;
        //             Debug.LogWarning($"[RECORD-OOB] Index {i} missing history. Fallback to Member Pos: {fallback}");
        //             nextHistory.Add(fallback);
        //         }
        //     }

        //     positionHistory = nextHistory;
        //     PrintHistoryReport("FINAL SHIFT");
        // }

        private void PrintHistoryReport(string label)
        {
            string report = "";
            for (int i = 0; i < positionHistory.Count; i++)
                report += $"[{i}]:{positionHistory[i]} ";
            Debug.Log($"[{label}] {report}");
        }

        public BaseActor GetActiveMember() => (partyMembers.Count > 0) ? partyMembers[activeIndex] : null;

        /// <summary>Removes a fallen member before destroy; snaps formation and repoints camera if leader died.</summary>
        public bool RemovePartyMember(BaseActor member)
        {
            if (member == null)
                return false;

            int index = partyMembers.IndexOf(member);
            if (index < 0)
                return false;

            bool wasLeader = index == 0;
            partyMembers.RemoveAt(index);

            for (int i = partyMembers.Count - 1; i >= 0; i--)
            {
                if (partyMembers[i] == null)
                    partyMembers.RemoveAt(i);
            }

            activeIndex = 0;
            SnapHistoryToCurrentPositions();

            if (partyMembers.Count > 0 && wasLeader)
                RefreshCameraFollow();

            return true;
        }

        // Cycle through members (good for a single "Tab" key bind)
        public void CycleActiveMember()
        {
            int nextIndex = (activeIndex + 1) % partyMembers.Count;
            SwapActiveMember(nextIndex);
        }

        /// <summary>
        /// Logic to designate a character as leader and reorder the party list.
        /// This ensures Index 0 is ALWAYS the person you are controlling.
        /// </summary>
        public void SwapActiveMember(int index)
        {
            if (partyMembers.Count == 0 || index < 0 || index >= partyMembers.Count) return;

            // 1. Identify the new leader
            BaseActor newLeader = partyMembers[index];

            // 2. REORDER: Move selected member to index 0
            // This is critical so that RecordNewLeaderPosition and Rush logic
            // always see the controlled player as the start of the chain.
            partyMembers.RemoveAt(index);
            partyMembers.Insert(0, newLeader);

            // Active index stays 0 because of the reordering
            activeIndex = 0;

            RefreshCameraFollow();

            // 4. RESET HISTORY: When the leader changes, the old breadcrumbs 
            // are invalid for the new line formation.
            SnapHistoryToCurrentPositions();

            Debug.Log($"[SWAP] Now controlling {newLeader.name}. Party list reordered and history snapped.");
        }

        // public void SnapHistoryToCurrentPositions()
        // {
        //     positionHistory.Clear();

        //     // The active member (new leader) always takes slot 0
        //     BaseActor leader = GetActiveMember();
        //     if (leader == null) return;

        //     // Fill the history with the current physical locations of the party
        //     // index 0 = Leader, index 1 = First Follower, etc.
        //     for (int i = 0; i < partyMembers.Count; i++)
        //     {
        //         positionHistory.Add(partyMembers[i].GridPosition);
        //     }

        //     Debug.Log($"[PARTY-SNAP] History realigned to current party formation. Count: {positionHistory.Count}");
        // }

        // <summary>
        /// Hard-aligns the breadcrumb trail to the current physical grid positions.
        /// Use this when swapping leaders or enabling formation mid-turn.
        /// </summary>
        public void SnapHistoryToCurrentPositions()
        {
            positionHistory.Clear();

            // Fill the history with the current physical locations of the party
            // index 0 = Leader, index 1 = First Follower, etc.
            for (int i = 0; i < partyMembers.Count; i++)
            {
                if (partyMembers[i] == null)
                {
                    Debug.LogError($"[PartyManager] partyMembers[{i}] is null; skipping snap slot.");
                    continue;
                }

                Vector3Int pos = partyMembers[i].GridPosition;
                positionHistory.Add(pos);
                Debug.Log($"[PARTY-SNAP] Index [{i}] set to {pos}");
            }

            Debug.Log($"[PARTY-SNAP] History realigned to current party formation. Count: {positionHistory.Count}");
        }

        public void UpdatePositionHistory(Vector3Int newLeaderPos)
        {
            // If the leader moved to a new tile, push the history
            if (positionHistory.Count == 0 || positionHistory[0] != newLeaderPos)
            {
                positionHistory.Insert(0, newLeaderPos);
            }
            // If leader bumped/stayed, we don't insert, 
            // but the followers will still use the existing history to close the gap.

            // Keep history sized to the party count
            if (positionHistory.Count > partyMembers.Count)
            {
                positionHistory.RemoveAt(positionHistory.Count - 1);
            }
        }
    }
}
