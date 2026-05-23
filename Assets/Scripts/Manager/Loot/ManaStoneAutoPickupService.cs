using System.Collections.Generic;
using JRogue.Actors.Components;
using JRogue.Item;
using JRogue.Item.World;
using JRogue.Manager.Floor;
using JRogue.Manager.Party;
using JRogue.Stats;
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

            TryAutoPickupManaStonesAt(newPos);
            TryAutoPickupWorldItemsAt(mover, newPos);
        }

        static void TryAutoPickupWorldItemsAt(GridMover mover, Vector3Int tile)
        {
            WorldItem[] worldItems = Object.FindObjectsByType<WorldItem>();
            for (int i = 0; i < worldItems.Length; i++)
            {
                WorldItem item = worldItems[i];
                if (item == null || item.data is ManaStoneItemData)
                    continue;

                Vector3Int itemTile = Vector3Int.FloorToInt(item.transform.position - new Vector3(0.5f, 0.5f, 0f));
                if (itemTile != tile)
                    continue;

                InventoryCollector.TryCollectWorldItem(item, mover.gameObject);
            }
        }

        public void TryAutoPickupManaStonesAt(Vector3Int tile)
        {
            FloorItemPileService pile = FloorItemPileService.Instance;
            if (pile == null)
                return;

            IReadOnlyList<FloorItemEntry> entries = pile.GetManaStoneAutoPickupEntries(tile);
            if (entries.Count == 0)
                return;

            var pending = new List<FloorItemEntry>(entries);
            for (int i = 0; i < pending.Count; i++)
                TryPickupEntry(pile, pending[i]);
        }

        static bool TryPickupEntry(FloorItemPileService pile, FloorItemEntry entry)
        {
            if (entry?.instance == null || !(entry.instance.Definition is ManaStoneItemData ms))
                return false;

            PartyManaStoneLedger ledger = PartyManaStoneLedger.Instance;
            if (ledger == null)
            {
                Debug.LogWarning("[LOOT] Mana stone pickup failed: no PartyManaStoneLedger.");
                return false;
            }

            ledger.Add(ms.tier, entry.instance.ManaStoneSourceSpeciesId, entry.instance.Quantity);
            pile.RemoveEntry(entry.entryId);
            Debug.Log(
                $"[LOOT] Auto-picked Mana Stone T{ms.tier} ({entry.instance.ManaStoneSourceSpeciesId}).");
            return true;
        }
    }
}
