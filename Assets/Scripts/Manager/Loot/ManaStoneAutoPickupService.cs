using System.Collections.Generic;
using JRogue.Actors.Components;
using JRogue.Manager.Floor;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.Manager.Loot
{
    public sealed class ManaStoneAutoPickupService : MonoBehaviour
    {
        public static ManaStoneAutoPickupService Instance { get; private set; }

        readonly HashSet<GridMover> _subscribed = new HashSet<GridMover>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start() => SubscribePartyMembers();

        void OnEnable() => SubscribePartyMembers();

        public void SubscribePartyMembers()
        {
            PartyManager party = PartyManager.Instance;
            if (party == null || party.partyMembers == null)
                return;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                var member = party.partyMembers[i];
                if (member == null)
                    continue;

                GridMover mover = member.GetComponent<GridMover>();
                if (mover == null || _subscribed.Contains(mover))
                    continue;

                mover.Moved += (oldPos, newPos) => OnPartyMemberMoved(mover, oldPos, newPos);
                _subscribed.Add(mover);
            }
        }

        void OnPartyMemberMoved(GridMover mover, Vector3Int oldPos, Vector3Int newPos)
        {
            if (oldPos == newPos || mover == null)
                return;

            TryAutoPickupManaStonesAt(newPos, mover.gameObject);
        }

        public void TryAutoPickupManaStonesAt(Vector3Int tile, GameObject picker = null) =>
            FloorPickupService.PickupSilentAt(tile, picker);
    }
}
