using UnityEngine;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New ItemSO", menuName = "OceanGame/ItemSO")]
    public class ItemSO : ScriptableObject
    {
        [field: SerializeField] public string ItemName { get; private set; }
        [field: SerializeField, TextArea] public string Description { get; private set; }
        [field: SerializeField] public Sprite DisplayIcon { get; private set; }
        [field: SerializeField] public bool IsStackable { get; private set; } = true;
        
        
    }
}