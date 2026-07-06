using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Manager.Grid;
using JRogue.Manager.Party;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Tracks party members parked off the active floor during Holy Land splits.
    /// Parked actors stay in <see cref="PartyManager.partyMembers"/> but are hidden and off the grid.
    /// </summary>
    public sealed class PartyFloorPresenceService : MonoBehaviour
    {
        public static PartyFloorPresenceService Instance { get; private set; }

        readonly HashSet<BaseActor> _parked = new HashSet<BaseActor>();
        Transform _parkRoot;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsureParkRoot();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool IsParked(BaseActor member) => member != null && _parked.Contains(member);

        public bool HasParkedMembers => _parked.Count > 0;

        public List<BaseActor> GetPresentMembers()
        {
            var present = new List<BaseActor>();
            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null)
                return present;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || IsParked(member))
                    continue;

                present.Add(member);
            }

            return present;
        }

        public List<BaseActor> GetParkedMembers()
        {
            var parked = new List<BaseActor>(_parked.Count);
            foreach (BaseActor member in _parked)
            {
                if (member != null)
                    parked.Add(member);
            }

            return parked;
        }

        public void ParkAllExcept(IReadOnlyList<BaseActor> presentMembers, string waitFloorId, Vector3Int waitAnchor)
        {
            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null || presentMembers == null)
                return;

            var present = new HashSet<BaseActor>();
            for (int i = 0; i < presentMembers.Count; i++)
            {
                if (presentMembers[i] != null)
                    present.Add(presentMembers[i]);
            }

            var toPark = new List<BaseActor>();
            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || present.Contains(member) || IsParked(member))
                    continue;

                if (member.stats != null && member.stats.currentHP <= 0)
                    continue;

                toPark.Add(member);
            }

            if (toPark.Count > 0)
                ParkMembers(toPark, waitFloorId, waitAnchor);
        }

        public void ParkMembers(IReadOnlyList<BaseActor> members, string waitFloorId, Vector3Int waitAnchor)
        {
            if (members == null || members.Count == 0)
                return;

            EnsureParkRoot();
            GridManager grid = GridManager.Instance;

            for (int i = 0; i < members.Count; i++)
            {
                BaseActor member = members[i];
                if (member == null || _parked.Contains(member))
                    continue;

                grid?.UnregisterActor(member.GridPosition);
                member.gameObject.transform.SetParent(_parkRoot, true);
                member.gameObject.SetActive(false);
                _parked.Add(member);
            }

            DungeonGenerationLog.Info(
                $"Parked {members.Count} member(s) for floor '{waitFloorId}' near {waitAnchor}.");
        }

        public void UnparkAll()
        {
            if (_parked.Count == 0)
                return;

            var toRestore = new List<BaseActor>(_parked);
            _parked.Clear();

            PartyManager party = PartyManager.Instance;
            Transform partyRoot = party != null ? party.transform : null;

            for (int i = 0; i < toRestore.Count; i++)
            {
                BaseActor member = toRestore[i];
                if (member == null)
                    continue;

                if (partyRoot != null)
                    member.gameObject.transform.SetParent(partyRoot, true);

                member.gameObject.SetActive(true);
            }

            DungeonGenerationLog.Info($"Unparked {toRestore.Count} member(s).");
        }

        public static List<BaseActor> CollectLivingBarbarians(PartyManager party) =>
            CollectLivingByRace(party, Race.Barbarian);

        public static List<BaseActor> CollectLivingElves(PartyManager party) =>
            CollectLivingByRace(party, Race.Elf);

        public static List<BaseActor> CollectLivingByRace(PartyManager party, Race race)
        {
            var members = new List<BaseActor>();
            if (party?.partyMembers == null)
                return members;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || member.stats == null || member.stats.currentHP <= 0)
                    continue;

                if (member.stats.race == race)
                    members.Add(member);
            }

            return members;
        }

        public static List<BaseActor> CollectLivingNonBarbarians(PartyManager party)
        {
            var others = new List<BaseActor>();
            if (party?.partyMembers == null)
                return others;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || member.stats == null || member.stats.currentHP <= 0)
                    continue;

                if (member.stats.race != Race.Barbarian)
                    others.Add(member);
            }

            return others;
        }

        void EnsureParkRoot()
        {
            if (_parkRoot != null)
                return;

            var go = new GameObject("PartyParkRoot");
            go.transform.SetParent(transform, false);
            _parkRoot = go.transform;
        }
    }
}
