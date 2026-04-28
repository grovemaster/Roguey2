
namespace JRogue.Stats
{
    [System.Serializable]
    public class StatModifier
    {
        public int Value;
        public object Source; // This will hold the 'this' reference of the PassiveEffect

        public StatModifier(int value, object source)
        {
            Value = value;
            Source = source;
        }
    }
}