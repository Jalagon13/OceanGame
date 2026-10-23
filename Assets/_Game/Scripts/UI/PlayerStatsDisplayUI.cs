using System;
using UnityEngine;

namespace OceanGame
{
    public class PlayerStatsDisplayUI : MonoBehaviour
    {
        [SerializeField] private StatBarUI _healthBar;

        private PlayerEquipment _equipment;

        private void Start()
        {
            var player = Player.Instance;
        
            if (player == null) return;

            player.PlayerReady += OnPlayerReady;

            if (player.Character != null)
                OnPlayerReady(player.Character);

        }

        private void OnDestroy()
        {
            var player = Player.Instance;

            if (player != null && player.Character != null)
            {
                player.Character.Health.OnHealthChanged -= OnHealthChanged;
                player.Character.Stats.OnBuffStarted -= UpdateStatBar;
                player.Character.Stats.OnBuffStopped -= UpdateStatBar;
            }

            if (player != null)
            {
                player.PlayerReady -= OnPlayerReady;
            }
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
    }
}