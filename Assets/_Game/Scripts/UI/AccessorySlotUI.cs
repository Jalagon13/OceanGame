using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace OceanGame
{
    public class AccessorySlotUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI References")]
        [SerializeField] private Image _itemIcon;
        [SerializeField] private Image _placeholderGhostIcon;
        private int _slotIndex;
        private void Awake()
        {
            // Automatically determines its slot index from its sibling position in the hierarchy
            _slotIndex = transform.GetSiblingIndex();
        }
        
        private void Start()
        {
            if (Player.Instance == null) return;
            
            Player.Instance.PlayerReady += OnPlayerReady;
            
            if (Player.Instance.Character != null && Player.Instance.Equipment != null)
            {
                OnPlayerReady(Player.Instance.Character);
            }
        }
        
        private void OnDestroy()
        {
            if (Player.Instance == null) return;
            
            Player.Instance.PlayerReady -= OnPlayerReady;
            
            if (Player.Instance.Equipment != null)
            {
                Player.Instance.Equipment.OnEquipmentChanged -= RefreshUI;
            }
        }
        
        private void OnPlayerReady(ServerCharacter character)
        {
            if (Player.Instance.Equipment != null)
            {
                // Ensure player equipment has this slot ready
                Player.Instance.Equipment.EnsureAccessorySlotCount(_slotIndex + 1);
                Player.Instance.Equipment.OnEquipmentChanged -= RefreshUI;
                Player.Instance.Equipment.OnEquipmentChanged += RefreshUI;
                
                RefreshUI();
            }
        }
        
        private void RefreshUI()
        {
            if (Player.Instance == null || Player.Instance.Equipment == null)
            {
                if (_itemIcon != null) _itemIcon.enabled = false;
                if (_placeholderGhostIcon != null) _placeholderGhostIcon.enabled = true;
                return;
            }
            
            var slot = Player.Instance.Equipment.GetAccessorySlot(_slotIndex);
            bool hasItem = slot != null && !slot.IsEmpty;
            
            if (_itemIcon != null)
            {
                _itemIcon.enabled = hasItem;
                if (hasItem)
                {
                    _itemIcon.sprite = GameDataRegistry.Instance.GetItemSOFromItemId(slot.ItemId).DisplayIcon;
                }
            }
            if (_placeholderGhostIcon != null)
            {
                _placeholderGhostIcon.enabled = !hasItem;
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (Player.Instance.Character.Health.CurrentLifeState.Value == LifeState.Dead) return;
            
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                InventoryCursorManager.Instance.HandleAccessorySlotLeftClick(_slotIndex);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                InventoryCursorManager.Instance.HandleAccessorySlotRightClick(_slotIndex);
            }
        }
    }
}