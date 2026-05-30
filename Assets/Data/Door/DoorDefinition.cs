using UnityEngine;

namespace JRogue.Data.Door
{
    public enum DoorState
    {
        Closed = 0,
        Open = 1,
        Broken = 2,
    }

    public enum DoorOrientation
    {
        Horizontal = 0,
        Vertical = 1,
    }

    [CreateAssetMenu(fileName = "Door", menuName = "JRogue/Doors/Door Definition")]
    public sealed class DoorDefinition : ScriptableObject
    {
        public string doorId;
        public string displayName = "Door";
        public DoorOrientation orientation = DoorOrientation.Horizontal;

        [Header("Initial state")]
        public bool startsLocked;
        public bool startsOpen;

        [Header("Durability")]
        public bool canBeBroken = true;
        [Min(1)] public int breakHitPoints = 1;

        [Header("Enemies")]
        public EnemyDoorCapability defaultEnemyCapability = EnemyDoorCapability.None;

        [Header("Sprites (H = horizontal passage, V = vertical)")]
        public Sprite closedHorizontal;
        public Sprite openHorizontal;
        public Sprite brokenHorizontal;
        public Sprite closedVertical;
        public Sprite openVertical;
        public Sprite brokenVertical;

        public Sprite GetSprite(DoorState state, DoorOrientation orient)
        {
            bool vertical = orient == DoorOrientation.Vertical;
            return state switch
            {
                DoorState.Open => vertical ? openVertical : openHorizontal,
                DoorState.Broken => vertical ? brokenVertical : brokenHorizontal,
                _ => vertical ? closedVertical : closedHorizontal,
            };
        }

        void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(doorId))
                doorId = name;

            if (startsLocked)
                startsOpen = false;
        }
    }
}
