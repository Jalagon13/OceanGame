using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    public class WorldTextPopupManager : MonoBehaviour
    {
        public static WorldTextPopupManager Instance { get; private set; }

        [Header("Damage Number")]
        [SerializeField] private DamageNumberPopup _damageNumberPrefab;
        [SerializeField] private Color _damageNumberColor = Color.red;
        [SerializeField, Min(1f)] private float _damageNumberFontSize = 24f;

        [Header("Item Collect Popup")]
        [SerializeField] private ItemCollectWorldPopup _itemCollectPopupPrefab;
        [SerializeField] private Color _itemCollectTextColor = Color.white;
        [SerializeField, Min(1f)] private float _itemCollectFontSize = 20f;

        private readonly Dictionary<ushort, ItemCollectWorldPopup> _activeItemPopups = new();

        private void Awake()
        {
            Instance = this;
        }

        public void SpawnDamageNumbers(Vector3 worldPosition, int damage)
        {
            if (_damageNumberPrefab == null)
            {
                Debug.LogError("Damage Number Prefab is not assigned.", this);
                return;
            }

            if (!CanSpawnAt(worldPosition))
            {
                return;
            }
            
            DamageNumberPopup popup = Instantiate(_damageNumberPrefab, worldPosition, Quaternion.identity);

            popup.Initialize(damage, _damageNumberColor, _damageNumberFontSize);
            popup.Play();
            Debug.Log($"Spawned damage number popup: {damage}");
        }

        public void SpawnItemCollectPopup(ItemSO item, int amount)
        {
            if (item == null)
            {
                Debug.LogError("Cannot spawn an item collect popup for a null item.", this);
                return;
            }

            if (amount <= 0)
            {
                Debug.LogError($"Cannot spawn an item collect popup for amount {amount}.", this);
                return;
            }

            Vector3 spawnPosition = Player.Instance.Character.transform.position;

            if (!CanSpawnAt(spawnPosition))
            {
                return;
            }

            ushort itemId = item.GetId();
            int totalAmount = amount;

            if (_activeItemPopups.TryGetValue(itemId, out ItemCollectWorldPopup existingPopup))
            {
                _activeItemPopups.Remove(itemId);

                if (existingPopup != null)
                {
                    totalAmount += existingPopup.DisplayAmount;
                    Destroy(existingPopup.gameObject);
                }
            }

            ItemCollectWorldPopup popup = Instantiate(_itemCollectPopupPrefab, spawnPosition, Quaternion.identity);

            popup.Initialize(item, totalAmount, _itemCollectTextColor, _itemCollectFontSize);
            popup.AnimationCompleted += completedPopup =>
            {
                if (_activeItemPopups.TryGetValue(itemId, out ItemCollectWorldPopup currentPopup) && currentPopup == completedPopup)
                {
                    _activeItemPopups.Remove(itemId);
                }
            };

            _activeItemPopups.Add(itemId, popup);
            popup.Play();
        }

        private bool CanSpawnAt(Vector3 worldPosition)
        {
            if (PlayerCamera.Instance == null)
            {
                Debug.LogError("World Renderer is not assigned on WorldTextPopupManager.", this);
                return false;
            }

            return PlayerCamera.Instance.IsPositionInBounds(worldPosition);
        }
    }
}