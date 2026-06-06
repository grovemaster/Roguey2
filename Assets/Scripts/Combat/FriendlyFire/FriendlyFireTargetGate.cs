using System;
using JRogue.Actors;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Combat.FriendlyFire
{
    public static class FriendlyFireTargetGate
    {
        public static bool TryInterceptConfirm(
            BaseActor caster,
            in TargetedActionContext context,
            Vector3Int primaryTile,
            Action onConfirmedExecute)
        {
            if (caster == null || onConfirmedExecute == null)
                return false;

            FriendlyFirePreview.Result preview = FriendlyFirePreview.Evaluate(caster, context, primaryTile);
            if (!preview.WouldHarmAllies)
                return false;

            FriendlyFireConfirmDialogUI dialog = FriendlyFireConfirmDialogUI.EnsureInstance();
            dialog.Show(
                caster,
                preview.ActionLabel,
                primaryTile,
                preview.AffectedAllies,
                onConfirmedExecute);
            return true;
        }
    }
}
