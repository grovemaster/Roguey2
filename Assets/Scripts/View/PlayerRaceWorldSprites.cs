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
        [SerializeField] Sprite barbarianSprite;
        [SerializeField] Sprite dwarfSprite;
        [SerializeField] Sprite beastmanSprite;
        [SerializeField] Sprite dragonianSprite;
        [SerializeField] Sprite tieflingSprite;
        [SerializeField] Sprite undeadSprite;

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
                case Race.Barbarian:
                    return barbarianSprite;
                case Race.Dwarf:
                    return dwarfSprite;
                case Race.Beastman:
                    return beastmanSprite;
                case Race.Dragonian:
                    return dragonianSprite;
                case Race.Tiefling:
                    return tieflingSprite;
                case Race.Undead:
                    return undeadSprite;
                default:
                    return null;
            }
        }
    }
}
