using System;

namespace JRogue.Racial
{
    sealed class SoulBeastModifierSource : IEquatable<SoulBeastModifierSource>
    {
        public string SoulBeastId { get; }
        public int Level { get; }

        public SoulBeastModifierSource(string soulBeastId, int level)
        {
            SoulBeastId = soulBeastId;
            Level = level;
        }

        public bool Equals(SoulBeastModifierSource other) =>
            other != null && SoulBeastId == other.SoulBeastId && Level == other.Level;

        public override bool Equals(object obj) => obj is SoulBeastModifierSource o && Equals(o);

        public override int GetHashCode() => HashCode.Combine(SoulBeastId, Level);
    }
}
