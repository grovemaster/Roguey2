namespace JRogue.Core.Actor
{
    /// <summary>
    /// Anything that can emit acoustic noise into the world. Implemented by
    /// <see cref="JRogue.Actors.BaseActor"/> and consumed by ability data
    /// (which lives in a leaf assembly and cannot depend on BaseActor directly).
    /// </summary>
    public interface INoiseProducer
    {
        void ProduceNoise(int volume);
    }
}
