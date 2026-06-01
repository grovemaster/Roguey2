using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Manager.Party;
using JRogue.World.MapInteract;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Activates dungeon portals when any party member enters the portal cell (step-on).
    /// Transports the whole party via <see cref="DungeonFloorInstanceManager"/>.
    /// </summary>
    public sealed class PortalEntryService : MonoBehaviour
    {
        public static PortalEntryService Instance { get; private set; }

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

        void OnEnable() => SubscribePartyMembers();

        public void SubscribePartyMembers()
        {
            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null)
                return;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null)
                    continue;

                GridMover mover = member.GetComponent<GridMover>();
                if (mover == null || _subscribed.Contains(mover))
                    continue;

                mover.Moved += OnPartyMemberMovedHandler;
                _subscribed.Add(mover);
            }
        }

        void OnPartyMemberMovedHandler(Vector3Int oldPos, Vector3Int newPos)
        {
            if (oldPos == newPos)
                return;

            foreach (GridMover mover in _subscribed)
            {
                if (mover == null || mover.GridPosition != newPos)
                    continue;

                BaseActor actor = mover.GetComponent<BaseActor>();
                if (actor != null)
                    TryActivatePortalAt(actor, newPos);

                return;
            }
        }

        public static bool TryActivatePortalAt(BaseActor partyMember, Vector3Int cell)
        {
            if (partyMember == null)
                return false;

            PartyManager party = PartyManager.Instance;
            if (party == null || party.partyMembers == null || !party.partyMembers.Contains(partyMember))
                return false;

            AdjacentMapInteractableService mapInteract = AdjacentMapInteractableService.Instance;
            if (mapInteract == null || !mapInteract.TryGetAtCell(cell, out IAdjacentMapInteractable interactable))
                return false;

            if (interactable is TownToDungeonPortalInteractable townPortal)
                return townPortal.TryActivate(partyMember);

            if (interactable is PortalInteractable portal)
                return portal.TryActivatePartyTeleport(partyMember);

            return false;
        }
    }
}
