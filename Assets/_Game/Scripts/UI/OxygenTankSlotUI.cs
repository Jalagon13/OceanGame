using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OceanGame
{
    public class OxygenTankSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _itemIcon;

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
            if (_itemIcon == null) return;

            if (Player.Instance == null || Player.Instance.Equipment == null)
            {
                _itemIcon.enabled = false;
                return;
            }

            var slot = Player.Instance.Equipment.EquippedOxygenTank;
            
            if (slot == null || slot.IsEmpty)
            {
                _itemIcon.enabled = false;
            }
            else
            {
                _itemIcon.enabled = true;
                _itemIcon.sprite = GameDataRegistry.Instance.GetItemSOFromItemId(slot.ItemId).DisplayIcon;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Player.Instance.Character.Health.CurrentLifeState.Value == LifeState.Dead) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                InventoryCursorManager.Instance.HandleOxygenTankSlotLeftClick();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                InventoryCursorManager.Instance.HandleOxygenTankSlotRightClick();
            }
        }
    }
}
