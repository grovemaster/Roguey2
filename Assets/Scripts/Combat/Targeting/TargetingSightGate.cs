using UnityEngine;

namespace JRogue.Combat.Targeting
{
    public static class TargetingSightGate
    {
        public const string LogPrefix = "[Targeting:Sight]";

        public static bool IsPrimaryTileDesignatable(Vector3Int primaryTile)
        {
            VisibilityManager visibility = Object.FindAnyObjectByType<VisibilityManager>();
            if (visibility == null)
                return false;

            primaryTile.z = 0;
            return visibility.IsVisible(primaryTile);
        }

        public static bool TryAllowConfirm(Vector3Int primaryTile, out string denyReason)
        {
            if (IsPrimaryTileDesignatable(primaryTile))
            {
                denyReason = null;
                return true;
            }

            primaryTile.z = 0;
            denyReason = $"Cannot designate {primaryTile.x},{primaryTile.y}: tile is out of sight.";
            return false;
        }
    }
}
