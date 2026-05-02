using System.Collections.Generic;
using JRogue.Actors;
using JRogue.View;
using UnityEngine;

namespace JRogue.Manager.Party
{
    public class PartyManager : MonoBehaviour
    {
        public static PartyManager Instance;

        [Header("Party Members")]
        // The list of all your JRPG party members
        public List<BaseActor> partyMembers = new List<BaseActor>();
        private int activeIndex = 0;

        [Header("Formation Settings")]
        // Requirement: Toggle between Follow-the-leader and Manual control
        public bool isFormationActive = true;

        // This will store the leader's path for followers to "Rush" into
        // private Queue<Vector3Int> breadcrumbTrail = new Queue<Vector3Int>();
        // INDEX 0: Leader's current tile
        // INDEX 1: Tile leader was on 1 move ago (Follower 1's slot)
        // INDEX 2: Tile leader was on 2 moves ago (Follower 2's slot)
        [Header("Formation History")]
        public List<Vector3Int> positionHistory = new List<Vector3Int>();
        // private const int MAX_BREADCRUMBS = 10;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void Start()
        {
            // Clear and force-sync history to actual actor positions
            positionHistory.Clear();
            Debug.Log($"[START-INIT] Syncing {partyMembers.Count} members to history.");

            for (int i = 0; i < partyMembers.Count; i++)
            {
                Vector3Int actPos = partyMembers[i].GridPosition;
                positionHistory.Add(actPos);
                Debug.Log($"[START-INIT] Slot [{i}] ({partyMembers[i].name}) set to {actPos}");
            }
        }

        public void RecordNewLeaderPosition(Vector3Int newPos)
        {
            // --- THE GATEKEEPER ---
            // If the leader is still at the same spot, do nothing and return.
            // This prevents index 0 and 1 from becoming the same coordinate, which causes clustering.
            if (positionHistory.Count > 0 && positionHistory[0] == newPos)
            {
                // Keeping a log here so you know WHY the shift didn't happen
                Debug.Log($"[RECORD-SKIP] Leader stationary at {newPos}. History preserved.");
                return;
            }

            Debug.Log($"[RECORD-START] Leader stepped onto: {newPos}. Current History Count: {positionHistory.Count}");
            List<Vector3Int> nextHistory = new List<Vector3Int>();

            nextHistory.Add(newPos);

            for (int i = 0; i < partyMembers.Count - 1; i++)
            {
                if (i < positionHistory.Count)
                {
                    Vector3Int oldPos = positionHistory[i];

                    // Your original massive teleportation jump check
                    if (i > 0 && Vector3Int.Distance(oldPos, nextHistory[i]) > 1.5f)
                    {
                        Debug.LogError($"[SANITY-FAIL] Index {i + 1} is receiving {oldPos}, which is too far from {nextHistory[i]}!");
                    }

                    Debug.Log($"[RECORD-SHIFT] Index {i} -> {i + 1}. Carrying Pos: {oldPos}");
                    nextHistory.Add(oldPos);
                }
                else
                {
                    Vector3Int fallback = partyMembers[i].GridPosition;
                    Debug.LogWarning($"[RECORD-OOB] Index {i} missing history. Fallback to Member Pos: {fallback}");
                    nextHistory.Add(fallback);
                }
            }

            positionHistory = nextHistory;
            PrintHistoryReport("FINAL SHIFT");
        }

        private void PrintHistoryReport(string label)
        {
            string report = "";
            for (int i = 0; i < positionHistory.Count; i++)
                report += $"[{i}]:{positionHistory[i]} ";
            Debug.Log($"[{label}] {report}");
        }

        public BaseActor GetActiveMember() => (partyMembers.Count > 0) ? partyMembers[activeIndex] : null;

        // Cycle through members (good for a single "Tab" key bind)
        public void CycleActiveMember()
        {
            int nextIndex = (activeIndex + 1) % partyMembers.Count;
            SwapActiveMember(nextIndex);
        }

        public void SwapActiveMember(int index)
        {
            if (partyMembers.Count == 0) return;

            activeIndex = Mathf.Clamp(index, 0, partyMembers.Count - 1);

            // Re-enable your camera logic
            // Camera.main.GetComponent<CameraFollow>()?.SetTarget(GetActiveMember().transform);

            // 1. Get the newly selected character
            BaseActor activeActor = GetActiveMember();

            // 2. Find the camera and update its target
            // We use FindAnyObjectByType for the setup phase, but you can 
            // cache this reference in Start() for better performance later.
            CameraFollow cam = FindAnyObjectByType<JRogue.View.CameraFollow>();
            if (cam != null && activeActor != null)
            {
                cam.SetTarget(activeActor.transform);
            }

            Debug.Log($"Active Party Member: {GetActiveMember().name}");
        }

        public void SnapHistoryToCurrentPositions()
        {
            positionHistory.Clear();

            // The active member (new leader) always takes slot 0
            BaseActor leader = GetActiveMember();
            if (leader == null) return;

            // Fill the history with the current physical locations of the party
            // index 0 = Leader, index 1 = First Follower, etc.
            for (int i = 0; i < partyMembers.Count; i++)
            {
                positionHistory.Add(partyMembers[i].GridPosition);
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
