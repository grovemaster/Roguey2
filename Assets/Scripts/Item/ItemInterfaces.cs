using UnityEngine;

namespace JRogue.Item
{
    public interface IEquippable
    {
        void Equip(GameObject user);
        void Unequip(GameObject user);
    }

    public interface IUsable
    {
        void Use(GameObject user);
    }
}