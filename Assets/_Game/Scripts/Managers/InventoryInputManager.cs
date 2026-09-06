using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace OceanGame
{
    public class InventoryInputManager : MonoBehaviour
    {
        public static InventoryInputManager Instance { get; private set; }
        
        public event Action<bool> OnInventoryOpenChanged;
        public event Action<List<RecipeSO>, int, int> OnCraftTableInteract;
        public event Action<int> OnActiveHotbarIndexChanged;
        
        public int ActiveHotbarIndex { get; private set; }
        public bool IsInventoryOpen { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            GameInput.Instance.OnScrollWheel += OnScrollWheel;
            GameInput.Instance.OnSelectSlot += OnSelectSlot;
            GameInput.Instance.OnToggleInventory += OnToggleInventory;
            GameInput.Instance.OnPrimaryActionPressed += OnPrimaryActionPressed;
            GameInput.Instance.OnSecondaryActionPressed += OnSecondaryActionPressed;

        }

        private void OnDestroy()
        {
            GameInput.Instance.OnScrollWheel -= OnScrollWheel;
            GameInput.Instance.OnSelectSlot -= OnSelectSlot;
            GameInput.Instance.OnToggleInventory -= OnToggleInventory;
            GameInput.Instance.OnPrimaryActionPressed -= OnPrimaryActionPressed;
            GameInput.Instance.OnSecondaryActionPressed -= OnSecondaryActionPressed;
        }

        private void Update()
        {
            var activeSlot = GetActiveInvSlot();
            if(activeSlot.IsEmpty) return;
            
            if(GameInput.Instance.PrimaryActionHeld)
            {
                activeSlot.GetItemSO().OnPrimaryActionHeld();
            }
            else if(GameInput.Instance.SecondaryActionHeld)
            {
                activeSlot.GetItemSO().OnSecondaryActionHeld();
            }
        }

        private void OnPrimaryActionPressed(InputAction.CallbackContext context)
        {
            var activeSlot = GetActiveInvSlot();
            
            if(!activeSlot.IsEmpty)
            {
                if(context.phase == InputActionPhase.Started)
                {
                    if(WorldManager.Instance.MouseOverUI) return;
                    
                    activeSlot.GetItemSO().OnPrimaryActionStarted();
                }
                else if(context.phase == InputActionPhase.Canceled)
                {
                    activeSlot.GetItemSO().OnPrimaryActionRelease();
                }
                
            }
        }

        private void OnSecondaryActionPressed(InputAction.CallbackContext context)
        {
            var activeSlot = GetActiveInvSlot();
            
            if (!activeSlot.IsEmpty)
            {
                if (context.phase == InputActionPhase.Started)
                {
                    if (WorldManager.Instance.MouseOverUI) return;

                    activeSlot.GetItemSO().OnSecondaryActionStarted();
                }
                else if (context.phase == InputActionPhase.Canceled)
                {
                    activeSlot.GetItemSO().OnSecondaryActionRelease();
                }

            }
        }

        public InventorySlot GetActiveInvSlot()
        {
            var cursorSlot = InventoryCursorManager.Instance.CursorSlot;
            
            if(cursorSlot != null && !cursorSlot.IsEmpty)
            {
                return cursorSlot;
            }
            
            return InventoryManager.Instance.PlayerInventory[ActiveHotbarIndex];
        }

        public void OnInteractWithCraftingTable(List<RecipeSO> recipes, int x, int y)
        {
            if(!IsInventoryOpen)
            {
                OnToggleInventory();
            }
        
            OnCraftTableInteract?.Invoke(recipes, x, y);
        }

        private void OnToggleInventory()
        {
            IsInventoryOpen = !IsInventoryOpen;

            OnInventoryOpenChanged?.Invoke(IsInventoryOpen);
        }

        private void OnScrollWheel(InputAction.CallbackContext context)
        {
            Vector2 scrollDelta = context.ReadValue<Vector2>();
            int itemCount = InventoryManager.Instance.HotbarSize;
            if (itemCount == 0)
            {
                return;
            }

            int selectedSlotIndex = ActiveHotbarIndex;

            if (scrollDelta.y > 0f)
            {
                int upcomingIndex = selectedSlotIndex - 1;
                selectedSlotIndex = upcomingIndex < 0 ? itemCount - 1 : selectedSlotIndex - 1;
                SelectHotbarSlot(selectedSlotIndex);
            }
            else if (scrollDelta.y < 0f)
            {
                int upcomingIndex = selectedSlotIndex + 1;
                selectedSlotIndex = upcomingIndex >= itemCount ? 0 : selectedSlotIndex + 1;
                SelectHotbarSlot(selectedSlotIndex);
            }
        }

        private void OnSelectSlot(InputAction.CallbackContext context)
        {
            var control = context.control;

            if (control is KeyControl key)
            {
                int slotIndex = key.keyCode - Key.Digit1;
                if (slotIndex >= 0 && slotIndex < InventoryManager.Instance.HotbarSize)
                {
                    SelectHotbarSlot(slotIndex);
                }
            }
        }

        private void SelectHotbarSlot(int hotbarSlotIndex)
        {
            int newIndex = Mathf.Clamp(hotbarSlotIndex, 0, InventoryManager.Instance.HotbarSize - 1);

            if (newIndex == ActiveHotbarIndex) // Ignore same calls
            {
                return;
            }

            ActiveHotbarIndex = newIndex;
            OnActiveHotbarIndexChanged?.Invoke(ActiveHotbarIndex);
            // Debug.Log($"Active Hotbar Index is: {newIndex}");
        }

        
    }
}
