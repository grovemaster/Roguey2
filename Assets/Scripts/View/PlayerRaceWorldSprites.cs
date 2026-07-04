using JRogue.Stats;
using UnityEngine;

namespace JRogue.View
{
    [CreateAssetMenu(fileName = "PlayerRaceWorldSprites", menuName = "JRogue/View/Player Race World Sprites")]
    public sealed class PlayerRaceWorldSprites : ScriptableObject
    {
        const string DefaultResourcePath = "Player/PlayerRaceWorldSprites";

        [SerializeField] Sprite humanSprite;
        [SerializeField] Sprite elfSprite;

        static PlayerRaceWorldSprites _cached;

        public static PlayerRaceWorldSprites LoadDefault()
        {
            if (_cached == null)
                _cached = Resources.Load<PlayerRaceWorldSprites>(DefaultResourcePath);
            return _cached;
        }

        public Sprite GetSpriteForRace(Race race)
        {
            switch (race)
            {
                case Race.Human:
                    return humanSprite;
                case Race.Elf:
                    return elfSprite;
                default:
                    return null;
            }
        }
    }
}
