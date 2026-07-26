using JRogue.UI.Gameplay;
using JRogue.World.Generation;
using JRogue.World.Rift;
using UnityEngine;

namespace JRogue.World.Altar
{
    /// <summary>Altar completion: open a rift portal at the altar cell (consumes offerings via runner).</summary>
    [CreateAssetMenu(
        fileName = "OpenRiftPortalAltarCompletionEffect",
        menuName = "JRogue/World/Altar/Effects/Open Rift Portal")]
    public sealed class OpenRiftPortalAltarCompletionEffect : AltarCompletionEffect
    {
        public RiftDefinition rift;

        public override bool CanExecute(AltarInstance instance, out string denyReason)
        {
            denyReason = null;
            if (instance == null)
            {
                denyReason = "The pedestal does not respond.";
                return false;
            }

            string hostFloorId = DungeonFloorInstanceManager.Instance
                ?.GetActiveFloorInstance()
                ?.Definition
                ?.FloorId;

            DungeonFloorDefinition host = DungeonFloorInstanceManager.Instance?.TryFindDefinition(hostFloorId);
            DungeonFloorRiftPolicy policy = host?.RiftPolicy;
            RiftSessionMeta meta = RiftSessionMeta.EnsureInstance();
            if (policy == null || !policy.HasRifts)
            {
                denyReason = "No rifts are available on this floor.";
                return false;
            }

            int day = DungeonTimeService.Instance == null
                ? 1
                : DungeonTimeService.Instance.ElapsedCycles + 1;

            return RiftPortalGateLogic.PassesPlayerTrigger(
                policy.HasRifts,
                day,
                policy.minDungeonDayToOpenPortal,
                meta.WasPortalConsumedThisRun(hostFloorId),
                meta.DungeonRunIndex,
                meta.GetLastPortalOpenedRun(hostFloorId),
                policy.minDungeonRunsBetweenPortals,
                out denyReason);
        }

        public override void Execute(AltarInstance instance)
        {
            if (instance == null)
                return;

            string hostFloorId = DungeonFloorInstanceManager.Instance
                ?.GetActiveFloorInstance()
                ?.Definition
                ?.FloorId;

            if (!RiftPortalService.TryOpenPlayerTriggeredPortal(
                    hostFloorId,
                    instance.Cell,
                    rift,
                    out string denyReason))
            {
                string msg = string.IsNullOrEmpty(denyReason)
                    ? "The pedestal does not respond."
                    : denyReason;
                GameLogService.ActiveSession.Append(msg);
                Debug.Log($"[Rift] Pedestal portal denied: {msg}");
                return;
            }

            Debug.Log($"[Rift] Pedestal at {instance.Cell} became a rift portal.");
        }
    }
}
