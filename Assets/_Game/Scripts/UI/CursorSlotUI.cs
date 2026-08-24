using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OceanGame
{
    public class CursorSlotUI : MonoBehaviour
    {
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TextMeshProUGUI _stackText;
        
        private void Start() 
        {
            _itemIcon.enabled = false;
            _stackText.enabled = false;
            _stackText.text = string.Empty;

            InventoryCursorManager.Instance.OnCursorSlotChanged += RefreshUI;
        }

        private void OnDestroy()
        {
            InventoryCursorManager.Instance.OnCursorSlotChanged -= RefreshUI;
        }
        
        private void Update() 
        {
            transform.position = Mouse.current.position.ReadValue();    
        }

        private void RefreshUI()
        {
            var cursorSlot = InventoryCursorManager.Instance.CursorSlot;
        
            if (cursorSlot.IsEmpty)
            {
                _itemIcon.enabled = false;
                _stackText.enabled = false;
                _stackText.text = string.Empty;
            }
            else
            {
                _itemIcon.enabled = true;
                _stackText.enabled = true;
                _itemIcon.sprite = GameDataRegistry.Instance.GetItemSOFromItemId(cursorSlot.ItemId).DisplayIcon;
                _stackText.text = cursorSlot.CurrentAmount > 1 ? cursorSlot.CurrentAmount.ToString() : string.Empty;
            }
        }

    }
}
