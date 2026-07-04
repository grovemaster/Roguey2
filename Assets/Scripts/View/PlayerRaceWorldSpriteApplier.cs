using JRogue.Stats;
using UnityEngine;

namespace JRogue.View
{
    public static class PlayerRaceWorldSpriteApplier
    {
        public static void Apply(GameObject playerRoot)
        {
            if (playerRoot == null)
                return;

            SpriteRenderer renderer = playerRoot.GetComponent<SpriteRenderer>();
            CharacterStats stats = playerRoot.GetComponent<CharacterStats>();
            if (renderer == null || stats == null)
                return;

            PlayerRaceWorldSprites catalog = PlayerRaceWorldSprites.LoadDefault();
            if (catalog == null)
                return;

            Sprite sprite = catalog.GetSpriteForRace(stats.race);
            if (sprite != null)
                renderer.sprite = sprite;
        }
    }
}
