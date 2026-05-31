using JRogue.Actors;
using JRogue.Manager.Essence;
using JRogue.Manager.Floor;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Input
{
    public static class EssenceMoveGate
    {
        public static bool TryInterceptMove(
            BaseActor mover,
            Vector3Int destination,
            bool isEnemyBump,
            System.Action onConfirmedMove)
        {
            if (isEnemyBump || mover == null || onConfirmedMove == null)
                return false;

            FloorEssenceService service = FloorEssenceService.Instance;
            if (service == null || !service.TryGetAt(destination, out FloorEssenceEntry entry))
                return false;

            bool canGain = EssencePickupEligibility.CanGain(
                mover,
                entry.essenceData,
                out string reason);

            string body = EssencePickupEligibility.BuildMoveDialogBody(
                mover,
                entry.essenceData,
                canGain,
                reason);

            EssencePickupConfirmDialogUI.EnsureInstance().Show(body, onConfirmedMove);
            return true;
        }
    }
}
