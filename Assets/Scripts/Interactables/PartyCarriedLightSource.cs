namespace JRogue.Interactables
{
    /// <summary>Lit accessory / helmet emitters carried by the party.</summary>
    public static class PartyCarriedLightSource
    {
        public static bool AnyMemberHasLitAccessoryEmitter() =>
            JRogue.World.Lighting.PartyLightEmitterBridge.AnyMemberHasActiveCarriedEmitter();
    }
}
