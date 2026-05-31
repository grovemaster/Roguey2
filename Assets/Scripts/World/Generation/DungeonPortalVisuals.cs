using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>Loads the shared portal overlay sprite for dungeon generation.</summary>
    public static class DungeonPortalVisuals
    {
        const string ResourcesPath = "Dungeon/PortalSprite";

        static Sprite _cached;

        public static Sprite PortalSprite
        {
            get
            {
                if (_cached == null)
                    _cached = Resources.Load<Sprite>(ResourcesPath);

                return _cached;
            }
        }
    }
}
