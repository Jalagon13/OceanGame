using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New Recipe", menuName = "OceanGame/RecipeSO")]
    public class RecipeSO : ScriptableObject
    {
        [field: SerializeField] public ItemSO OutputItem { get; private set; }
        [field: SerializeField] public int OutputAmonut { get; private set; }
        [field: SerializeField] public List<Ingredient> Recipe { get; private set; }
    }

    [Serializable]
    public struct Ingredient 
    {
        [SerializeField] private ItemSO _ingredient;
        [SerializeField] private int _amount;

        public readonly ItemSO Item => _ingredient;
        public readonly int Amount => _amount;

        public Ingredient(ItemSO ingredient, int amount)
        {
            _ingredient = ingredient;
            _amount = amount;
        }
    }
}
