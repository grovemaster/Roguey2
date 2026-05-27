using System;
using JRogue.Actors;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Traps
{
    public static class TrapMoveGate
    {
        public static bool TryInterceptMove(
            BaseActor mover,
            Vector3Int destination,
            bool isEnemyBump,
            Action onConfirmedMove)
        {
            if (isEnemyBump || mover == null || onConfirmedMove == null)
                return false;

            TrapService traps = TrapService.Instance;
            if (traps == null || !traps.RequiresEnterConfirm(destination))
                return false;

            if (!traps.TryGetEnterConfirmTrap(destination, out TrapInstance instance)
                || instance.Definition == null)
            {
                return false;
            }

            TrapConfirmDialogUI dialog = TrapConfirmDialogUI.EnsureInstance();
            dialog.Show(mover, instance.Definition, onConfirmedMove);
            return true;
        }
    }
}
