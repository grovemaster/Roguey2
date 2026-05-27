namespace JRogue.Traps
{
    public enum TrapId
    {
        None = 0,
        Spike = 1,
        Bear = 2,
        Dart = 3,
    }

    public enum TrapPlacement
    {
        Floor = 0,
        Wall = 1,
    }

    public enum TrapVisibility
    {
        Visible = 0,
        Invisible = 1,
    }

    public enum TrapTriggerLimit
    {
        Once = 0,
        Finite = 1,
        Infinite = 2,
    }
}
