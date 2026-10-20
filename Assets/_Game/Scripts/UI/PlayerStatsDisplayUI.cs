using System;
using UnityEngine;

namespace OceanGame
{
    public class PlayerStatsDisplayUI : MonoBehaviour
    {
        [SerializeField] private StatBarUI _healthBar;
        [SerializeField] private StatBarUI _oxygenBar; 

        private PlayerEquipment _equipment;

        private void Awake()
        {
            if (_oxygenBar != null)
            {
                _oxygenBar.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            var player = Player.Instance;
        
            if (player == null) return;

            player.PlayerReady += OnPlayerReady;

            if (player.Character != null)
                OnPlayerReady(player.Character);

            if (OxygenManager.Instance != null)
            {
                OxygenManager.Instance.OnOxygenChanged += OnOxygenChanged;
                if (_oxygenBar != null)
                {
                    _oxygenBar.UpdateBar(OxygenManager.Instance.CurrentOxygen, OxygenManager.Instance.MaxOxygen.GetValue());
                }
            }
        }

        private void OnDestroy()
        {
            var player = Player.Instance;

            if (_equipment != null)
            {
                _equipment.OnEquipmentChanged -= RefreshOxygenBarVisibility;
            }

            if (player != null && player.Character != null)
            {
                player.Character.Health.OnHealthChanged -= OnHealthChanged;
                player.Character.Stats.OnBuffStarted -= UpdateStatBar;
                player.Character.Stats.OnBuffStopped -= UpdateStatBar;
            }

            if (OxygenManager.Instance != null)
            {
                OxygenManager.Instance.OnOxygenChanged -= OnOxygenChanged;
            }

            if (player != null)
            {
                player.PlayerReady -= OnPlayerReady;
            }
        }

        private void OnPlayerReady(ServerCharacter player)
        {
            if (_equipment != null)
            {
                _equipment.OnEquipmentChanged -= RefreshOxygenBarVisibility;
            }

            _equipment = Player.Instance.Equipment;
            if (_equipment != null)
            {
                _equipment.OnEquipmentChanged += RefreshOxygenBarVisibility;
            }

            player.Health.OnHealthChanged += OnHealthChanged;
            player.Stats.OnBuffStarted += UpdateStatBar;
            player.Stats.OnBuffStopped += UpdateStatBar;

            _healthBar.UpdateBar(player.Health.CurrentHealth.Value, player.Stats.MaxHealth.GetValue());
            RefreshOxygenBarVisibility();
        }

        private void RefreshOxygenBarVisibility()
        {
            if (_oxygenBar == null) return;

            bool hasTank = _equipment != null && !_equipment.EquippedOxygenTank.IsEmpty;
            _oxygenBar.gameObject.SetActive(hasTank);

            if (hasTank && OxygenManager.Instance != null)
            {
                _oxygenBar.UpdateBar(OxygenManager.Instance.CurrentOxygen, OxygenManager.Instance.MaxOxygen.GetValue());
            }
        }

        private void UpdateStatBar(Buff buff)
        {
            _healthBar.UpdateBar(Player.Instance.Character.Health.CurrentHealth.Value, Player.Instance.Character.Stats.MaxHealth.GetValue());
        }

        private void OnHealthChanged(object sender, CharacterHealth.HealthChangedArgs e)
        {
            _healthBar.UpdateBar(e.CurrentHp, e.MaxHP);
        }

        private void OnOxygenChanged(float currentOxygen, float maxOxygen)
        {
            if (_oxygenBar != null)
            {
                _oxygenBar.UpdateBar(currentOxygen, maxOxygen);
            }
        }
    }
}