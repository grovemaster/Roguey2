using JRogue.Stats.Racial;

namespace JRogue.Stats
{
    [System.Serializable]
    public class StatModifier
    {
        public int Value;
        public object Source;
        public ModifierSourceLayer Layer;

        public StatModifier(int value, object source, ModifierSourceLayer layer = ModifierSourceLayer.Temporary)
        {
            Value = value;
            Source = source;
            Layer = layer;
        }
    }
}