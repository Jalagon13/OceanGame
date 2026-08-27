using UnityEngine;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New ItemSO", menuName = "OceanGame/Item/ItemSO")]
    public class ItemSO : ScriptableObject
    {
        [field: Header("Base Item")]
        [field: SerializeField] public string ItemName { get; private set; }
        [field: SerializeField, TextArea] public string Description { get; private set; }
        [field: SerializeField] public Sprite DisplayIcon { get; private set; }
        [field: SerializeField] public bool IsStackable { get; private set; } = true;
        
        public ushort GetId()
        {
            return GameDataRegistry.Instance.GetItemIdFromItemSO(this);
        }
        
        public virtual void OnPrimaryActionStarted() { }
        public virtual void OnPrimaryActionHeld() { }
        public virtual void OnPrimaryActionRelease() { }
        
        public virtual void OnSecondaryActionStarted() { }
        public virtual void OnSecondaryActionHeld() { }
        public virtual void OnSecondaryActionRelease() { }
    }
}