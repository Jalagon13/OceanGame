using System;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class ChestData
    {
        public Vector2Int RootPosition { get; private set; }
        public InventorySlot[] Slots { get; private set; }

        public ChestData(Vector2Int rootPosition, int size = 20)
        {
            RootPosition = rootPosition;
            Slots = new InventorySlot[size];
            for (int i = 0; i < size; i++)
            {
                Slots[i] = new InventorySlot();
            }
        }

        public bool IsEmpty()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (!Slots[i].IsEmpty) return false;
            }
            return true;
        }
    }
}