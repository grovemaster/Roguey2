using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Manager.Party;
using JRogue.World.MapInteract;
using JRogue.World.Town;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Activates floor portals when a party member steps onto the portal cell.
    /// With formation off, any member can trigger; with formation on, only the active
    /// member triggers so followers rushing onto a door tile do not hijack the move.
    /// Transports the whole party via <see cref="DungeonFloorInstanceManager"/>.
    /// </summary>
    public sealed class PortalEntryService : MonoBehaviour
    {
        public const string DebugTag = "[HolyLandPortal]";

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

        void LateUpdate()
        {
            JRogue.World.Rift.RiftPortalService.TickHostPortalEntryArming();
        }

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

                SubscribeMover(member.GetComponent<GridMover>());
            }
        }

        void SubscribeMover(GridMover mover)
        {
            if (mover == null || _subscribed.Contains(mover))
                return;

            mover.Moved += (oldPos, newPos) => OnPartyMemberMoved(mover, oldPos, newPos);
            _subscribed.Add(mover);
        }

        void OnPartyMemberMoved(GridMover mover, Vector3Int oldPos, Vector3Int newPos)
        {
            if (oldPos == newPos || mover == null)
                return;

            BaseActor actor = mover.GetComponent<BaseActor>();
            if (actor == null)
            {
                Debug.LogWarning($"{DebugTag} Mover at {newPos} has no BaseActor.");
                return;
            }

            PartyManager party = PartyManager.Instance;
            if (!CanMemberTriggerStepOnPortal(actor, party))
                return;

            if (HolyLandNexusLayout.IsHolyLandExitActivationCell(newPos))
            {
                Debug.Log(
                    $"{DebugTag} Step on holy-land exit cell {newPos} — actor={actor.name} " +
                    $"race={actor.stats?.race} activeFloor={GetActiveFloorId()}");
            }

            bool activated = TryActivatePortalAt(actor, newPos);
            if (HolyLandNexusLayout.IsHolyLandExitActivationCell(newPos) && !activated)
                Debug.LogWarning($"{DebugTag} Exit cell {newPos} — portal activation FAILED for {actor.name}.");
        }

        static string GetActiveFloorId() =>
            DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance()?.FloorId ?? "(none)";

        public static bool CanMemberTriggerStepOnPortal(BaseActor partyMember, PartyManager party)
        {
            if (partyMember == null || party?.partyMembers == null || !party.partyMembers.Contains(partyMember))
                return false;

            if (party.IsFormationActive)
                return party.GetActiveMember() == partyMember;

            return true;
        }

        public static bool TryActivatePortalAt(BaseActor partyMember, Vector3Int cell)
        {
            if (partyMember == null)
            {
                Debug.LogWarning($"{DebugTag} TryActivatePortalAt — partyMember is null at {cell}.");
                return false;
            }

            DungeonFloorInstanceManager floorManager = DungeonFloorInstanceManager.Instance;
            if (floorManager != null && floorManager.IsPortalTransitionInProgress)
            {
                Debug.LogWarning(
                    $"{DebugTag} TryActivatePortalAt blocked — transition in progress at {cell} for {partyMember.name}.");
                return false;
            }

            PartyManager party = PartyManager.Instance;
            if (!CanMemberTriggerStepOnPortal(partyMember, party))
            {
                BaseActor active = party?.GetActiveMember();
                Debug.LogWarning(
                    $"{DebugTag} TryActivatePortalAt blocked — {partyMember.name} cannot trigger step-on portal at {cell}. " +
                    $"formationActive={party?.IsFormationActive ?? false} activeMember={active?.name ?? "(none)"}.");
                return false;
            }

            AdjacentMapInteractableService mapInteract = AdjacentMapInteractableService.Instance;
            if (mapInteract == null)
            {
                Debug.LogError($"{DebugTag} TryActivatePortalAt — AdjacentMapInteractableService.Instance is null.");
                return false;
            }

            // Most stepped-on cells are ordinary floor — not an error.
            if (!mapInteract.TryGetAtCell(cell, out IAdjacentMapInteractable interactable))
                return false;

            Debug.Log(
                $"{DebugTag} Interactable at {cell}: {interactable.GetType().Name} label='{interactable.ListLabel}'");

            if (interactable is TownToDungeonPortalInteractable townPortal)
                return townPortal.TryActivate(partyMember);

            if (interactable is PortalInteractable portal)
                return portal.TryActivatePartyTeleport(partyMember);

            Debug.LogWarning(
                $"{DebugTag} Interactable at {cell} is {interactable.GetType().Name}, not a floor portal.");
            return false;
        }
    }
}
