using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Core.Actor;
using UnityEngine;

namespace JRogue.Dialog
{
    /// <summary>
    /// Clerk behind a shop counter row: customer stands on <see cref="customerRowY"/> and talks across
    /// a registered counter tile directly north on <see cref="counterRowY"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NpcController))]
    public sealed class NpcCounterTalkBinding : MonoBehaviour
    {
        [SerializeField] int customerRowY = 4;
        [SerializeField] int counterRowY = 5;

        NpcController _npc;

        public NpcController Npc => _npc != null ? _npc : _npc = GetComponent<NpcController>();

        public void Configure(int customerRow, int counterRow)
        {
            customerRowY = customerRow;
            counterRowY = counterRow;
        }

        public bool IsEligibleForCounterTalk(BaseActor actor)
        {
            NpcController npc = Npc;
            if (npc == null)
                return false;

            return NpcTalkFacingUtility.IsEligibleForCounterTalk(
                actor,
                npc.GridPosition,
                customerRowY,
                counterRowY);
        }

        public bool TryGetCounterTalk(BaseActor actor, out INpcTalkTarget talkTarget)
        {
            talkTarget = null;
            if (actor == null)
                return false;

            NpcController npc = Npc;
            if (npc == null)
                return false;

            if (!NpcTalkFacingUtility.IsFacingAcrossShopCounter(
                    actor,
                    npc.GridPosition,
                    customerRowY,
                    counterRowY))
            {
                return false;
            }

            talkTarget = npc;
            return true;
        }
    }
}
