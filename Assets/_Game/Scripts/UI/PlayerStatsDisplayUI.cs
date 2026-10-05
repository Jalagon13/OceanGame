using System;
using UnityEngine;

namespace OceanGame
{
    public class PlayerStatsDisplayUI : MonoBehaviour
    {
        [SerializeField] private StatBarUI _healthBar;
        [SerializeField] private StatBarUI _oxygenBar; 

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
        
            if (player == null) return;

            if (player.Character != null)
            {
                player.Character.Health.OnHealthChanged -= OnHealthChanged;
                player.Character.Stats.OnBuffStarted -= UpdateStatBar;
                player.Character.Stats.OnBuffStopped -= UpdateStatBar;
            }

            if (OxygenManager.Instance != null)
            {
                OxygenManager.Instance.OnOxygenChanged -= OnOxygenChanged;
            }

            player.PlayerReady -= OnPlayerReady;
        }

        private void OnPlayerReady(ServerCharacter player)
        {
            player.Health.OnHealthChanged += OnHealthChanged;
            player.Stats.OnBuffStarted += UpdateStatBar;
            player.Stats.OnBuffStopped += UpdateStatBar;

            _healthBar.UpdateBar(player.Health.CurrentHealth.Value, player.Stats.MaxHealth.GetValue());
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
            _oxygenBar.UpdateBar(currentOxygen, maxOxygen);
        }
    }
}