using UnityEngine;

namespace JRogue.UI.Gameplay
{
    public static class PlayfieldLayout
    {
        public const float ConsoleHeightPixels = 100f;
        public const float ReferenceScreenHeight = 1080f;

        public static float GetConsoleHeightPixels() =>
            ConsoleHeightPixels * (Screen.height > 0 ? Screen.height / ReferenceScreenHeight : 1f);

        /// <summary>
        /// World-space Y offset applied to the camera so the follow target sits at the playfield center above the console.
        /// </summary>
        public static float GetCameraVerticalOffsetWorld(Camera camera)
        {
            if (camera == null || !camera.orthographic || Screen.height <= 0)
                return 0f;

            float consoleFraction = GetConsoleHeightPixels() / Screen.height;
            return -camera.orthographicSize * consoleFraction;
        }
    }
}
