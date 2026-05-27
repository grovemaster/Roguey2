using System;
using JRogue.Actors;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Hazards
{
    /// <summary>Pre-move confirmation for persistent hazards (poison gas).</summary>
    public static class HazardMoveGate
    {
        public static bool TryInterceptMove(
            BaseActor mover,
            Vector3Int destination,
            bool isEnemyBump,
            Action onConfirmedMove)
        {
            if (isEnemyBump || mover == null || onConfirmedMove == null)
                return false;

            HazardService hazards = HazardService.Instance;
            if (hazards == null || !hazards.RequiresEnterConfirm(destination))
                return false;

            EnvironmentalHazardDefinition def = hazards.GetHazardAt(destination);
            if (def == null)
                return false;

            HazardConfirmDialogUI dialog = HazardConfirmDialogUI.EnsureInstance();
            dialog.Show(mover, def, onConfirmedMove);
            return true;
        }
    }
}
