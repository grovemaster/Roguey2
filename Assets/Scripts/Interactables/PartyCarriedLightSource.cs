namespace JRogue.Interactables
{
    /// <summary>v1 hook: lit accessory torches. Wall-torch v0 QA does not use this.</summary>
    public static class PartyCarriedLightSource
    {
        public static bool AnyMemberHasLitAccessoryEmitter()
        {
            // Carried torch v1 — see Docs/World/Lighting-QA-And-Torch-v0-Requirements.md §8.
            return false;
        }
    }
}
