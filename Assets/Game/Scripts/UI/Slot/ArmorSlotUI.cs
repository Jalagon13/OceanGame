using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace SketchLand
{
    public class ArmorSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("Configuration")]
        [SerializeField] private ArmorType _armorType;
        
        [Header("UI References")]
        [SerializeField] private Image _itemIcon;
        [SerializeField] private Image _placeholderGhostIcon; // Optional faint icon indicating helmet/chest/pants
        
        private TooltipHover _hover;

        private void Awake()
        {
            _hover = GetComponent<TooltipHover>();
            _hover.OnHoverEnter = ShowEquippedItemTooltip;
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
            var slot = Player.Instance.Equipment.GetArmorSlot(_armorType);
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

        private void ShowEquippedItemTooltip()
        {
            if (Player.Instance == null || Player.Instance.Equipment == null) return;

            var slot = Player.Instance.Equipment.GetArmorSlot(_armorType);
            if (slot != null && !slot.IsEmpty)
            {
                Tooltip.ItemDescriptionDisplay(GameDataRegistry.Instance.GetItemSOFromItemId(slot.ItemId), slot.Amount);
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (Player.Instance.Character.Health.CurrentLifeState.Value == LifeState.Dead) return;
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                InventoryCursorManager.Instance.HandleArmorSlotLeftClick(_armorType);
                _hover.RefreshTooltip();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                InventoryCursorManager.Instance.HandleArmorSlotRightClick(_armorType);
                _hover.RefreshTooltip();
            }
        }
    }
}