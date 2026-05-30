using UnityEngine;

namespace JRogue.Manager.Door
{
    /// <summary>Procedural door placeholders until DCSS art import (Door-Requirements §14).</summary>
    public static class DoorPlaceholderSprites
    {
        public static Sprite ClosedHorizontal => Get(ref _closedH, 0.55f, 0.35f, 0.2f);
        public static Sprite OpenHorizontal => Get(ref _openH, 0.35f, 0.55f, 0.25f);
        public static Sprite BrokenHorizontal => Get(ref _brokenH, 0.4f, 0.4f, 0.4f);
        public static Sprite ClosedVertical => Get(ref _closedV, 0.45f, 0.3f, 0.22f);
        public static Sprite OpenVertical => Get(ref _openV, 0.3f, 0.5f, 0.28f);
        public static Sprite BrokenVertical => Get(ref _brokenV, 0.42f, 0.42f, 0.42f);

        static Sprite _closedH, _openH, _brokenH, _closedV, _openV, _brokenV;

        static Sprite Get(ref Sprite cache, float r, float g, float b)
        {
            if (cache != null)
                return cache;

            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
            };
            var fill = new Color(r, g, b, 1f);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = fill;
            tex.SetPixels(pixels);
            tex.Apply();
            cache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            return cache;
        }
    }
}
