namespace JRogue.Core.Actor
{
    public enum FacingDirection
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest,
    }

    public enum FootprintLayout
    {
        Rectangle = 0,
        SnakeHeadBody = 1,
    }

    public enum EnemyAttackProfileKind
    {
        AdjacentSingle = 0,
        AdjacentSideSweep = 1,
    }
}
