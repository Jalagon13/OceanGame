using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    public class ChestDisplayUI : MonoBehaviour
    {
        [SerializeField] private GameObject _chestUIPanel;
        [SerializeField] private Transform _chestSlotsHolder;
        [SerializeField] private InventorySlotUI _inventorySlotPrefab;

        private readonly List<InventorySlotUI> _instantiatedSlots = new();

        private void Start()
        {
            ChestManager.Instance.OnChestOpened += ShowChestUI;
            ChestManager.Instance.OnChestClosed += HideChestUI;
            ChestManager.Instance.OnChestInventoryChanged += RefreshChestSlots;
            InventoryInputManager.Instance.OnInventoryOpenChanged += HandleInventoryToggled;

            HideChestUI();
        }

        private void OnDestroy()
        {
            if (ChestManager.Instance != null)
            {
                ChestManager.Instance.OnChestOpened -= ShowChestUI;
                ChestManager.Instance.OnChestClosed -= HideChestUI;
                ChestManager.Instance.OnChestInventoryChanged -= RefreshChestSlots;
            }

            if (InventoryInputManager.Instance != null)
            {
                InventoryInputManager.Instance.OnInventoryOpenChanged -= HandleInventoryToggled;
            }
        }

        private void Update()
        {
            // Proximity check: close if player moves beyond interact range
            if (ChestManager.Instance.CurrentOpenChest != null && _chestUIPanel.activeInHierarchy)
            {
                Vector2 chestPos = (Vector2)ChestManager.Instance.CurrentOpenChest.RootPosition + new Vector2(0.5f, 0.5f);
                float distance = Vector2.Distance(Player.Instance.Character.transform.position, chestPos);

                if (distance > Player.Instance.InteractRange)
                {
                    ChestManager.Instance.CloseChest();
                }
            }
        }

        private void ShowChestUI(ChestData chest)
        {
            // Clean up old slot objects
            for (int i = _chestSlotsHolder.childCount - 1; i >= 0; i--)
            {
                Destroy(_chestSlotsHolder.GetChild(i).gameObject);
            }
            _instantiatedSlots.Clear();

            // Populate slots for this chest
            for (int i = 0; i < chest.Slots.Length; i++)
            {
                var slotUI = Instantiate(_inventorySlotPrefab, _chestSlotsHolder);
                slotUI.Initialize(chest.Slots[i], OnSlotClicked);
                _instantiatedSlots.Add(slotUI);
            }

            _chestUIPanel.SetActive(true);
        }

        private void OnSlotClicked()
        {
            ChestManager.Instance.NotifyChestChanged();
        }

        private void RefreshChestSlots()
        {
            foreach (var slotUI in _instantiatedSlots)
            {
                slotUI.RefreshUI();
            }
        }

        private void HideChestUI()
        {
            _chestUIPanel.SetActive(false);
        }

        private void HandleInventoryToggled(bool isInventoryOpen)
        {
            if (!isInventoryOpen)
            {
                ChestManager.Instance.CloseChest();
            }
        }
    }
}