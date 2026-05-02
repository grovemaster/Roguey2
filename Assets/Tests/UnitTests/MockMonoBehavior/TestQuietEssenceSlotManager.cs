using System;
using System.Reflection;
using JRogue.Manager.Essence;
using UnityEngine;

namespace JRogue.Tests.UnitTests.MockMonoBehavior
{
    /// <summary>
    /// Test double for production <see cref="EssenceSlotManager"/>: its Awake iterates <c>equippedEssences</c> while that
    /// array is still null when the component is added in code (no inspector setup), which throws.
    /// NSubstitute cannot stand in for a Unity <see cref="MonoBehaviour"/> attached to a GameObject, so a thin subclass
    /// initializes the backing array and leaves other behavior to the base type.
    /// </summary>
    public sealed class TestQuietEssenceSlotManager : EssenceSlotManager
    {
        private void Awake()
        {
            int slots = totalSlots > 0 ? totalSlots : 3;
            FieldInfo equippedField =
                typeof(EssenceSlotManager).GetField("equippedEssences", BindingFlags.Instance | BindingFlags.NonPublic);
            if (equippedField == null || equippedField.FieldType.GetElementType() == null)
                return;

            Array existing = equippedField.GetValue(this) as Array;
            if (existing != null && existing.Length >= slots)
                return;

            Array arr = Array.CreateInstance(equippedField.FieldType.GetElementType(), slots);
            equippedField.SetValue(this, arr);
        }
    }
}
