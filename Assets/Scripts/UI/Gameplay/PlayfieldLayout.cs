using UnityEngine;

namespace JRogue.UI.Gameplay
{
    public static class PlayfieldLayout
    {
        public const float ConsoleHeightPixels = 100f;
        public const float HotbarHeightPixels = 128f;
        public const float HotbarWidthPixels = 1040f;
        public const float PartyStripHeightPixels = 144f;
        public const float PartyStripWidthPixels = 980f;
        public const float ReferenceScreenHeight = 1080f;

        public static float GetConsoleHeightPixels() =>
            ConsoleHeightPixels * Scale;

        public static float GetHotbarHeightPixels() =>
            HotbarHeightPixels * Scale;

        public static float GetPartyStripHeightPixels() =>
            PartyStripHeightPixels * Scale;

        public static float GetTopHudHeightPixels() =>
            GetPartyStripHeightPixels();

        public static float GetBottomHudHeightPixels() =>
            GetConsoleHeightPixels() + GetHotbarHeightPixels();

        public static float GetVerticalHudHeightPixels() =>
            GetTopHudHeightPixels() + GetBottomHudHeightPixels();

        public static float GetPlayfieldHeightPixels()
        {
            if (Screen.height <= 0)
                return ReferenceScreenHeight;

            return Screen.height - GetVerticalHudHeightPixels();
        }

        static float Scale => Screen.height > 0 ? Screen.height / ReferenceScreenHeight : 1f;

        /// <summary>
        /// World-space Y offset applied to the camera so the follow target sits at the playfield center between top and bottom HUD rails.
        /// </summary>
        public static float GetCameraVerticalOffsetWorld(Camera camera)
        {
            if (camera == null || !camera.orthographic || Screen.height <= 0)
                return 0f;

            float bottomFraction = GetBottomHudHeightPixels() / Screen.height;
            float topFraction = GetTopHudHeightPixels() / Screen.height;
            return -camera.orthographicSize * (bottomFraction - topFraction);
        }
    }
}
