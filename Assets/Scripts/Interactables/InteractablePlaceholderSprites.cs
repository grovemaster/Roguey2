using UnityEngine;

namespace JRogue.Interactables
{
    /// <summary>Procedural lever placeholders until art is approved (§12).</summary>
    static class InteractablePlaceholderSprites
    {
        static Sprite _offRight;
        static Sprite _onLeft;

        public static Sprite OffRight => _offRight ??= Create(0.75f, 0.45f, 0.2f, "Lever_Off_Right");
        public static Sprite OnLeft => _onLeft ??= Create(0.2f, 0.55f, 0.75f, "Lever_On_Left");

        static Sprite Create(float r, float g, float b, string name)
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var fill = new Color(r, g, b, 1f);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = fill;

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
