namespace JRogue.Service.Loot
{
    public interface ILootRandom
    {
        float NextFloat();
    }

    public sealed class UnityLootRandom : ILootRandom
    {
        public static readonly UnityLootRandom Default = new UnityLootRandom();

        public float NextFloat() => UnityEngine.Random.value;
    }

    public sealed class SeededLootRandom : ILootRandom
    {
        readonly System.Random _rng;

        public SeededLootRandom(int seed) => _rng = new System.Random(seed);

        public float NextFloat() => (float)_rng.NextDouble();
    }
}
