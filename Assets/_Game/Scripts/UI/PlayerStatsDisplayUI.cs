using System;
using UnityEngine;

namespace OceanGame
{
    public class PlayerStatsDisplayUI : MonoBehaviour
    {
        [SerializeField] private StatBarUI _healthBar;
        
        private void Start()
        {
            if (Player.Instance == null) return;

            Player.Instance.PlayerReady += OnPlayerReady;

            if (Player.Instance.Character != null)
                OnPlayerReady(Player.Instance.Character);
        }

        private void OnDestroy()
        {
            if (Player.Instance == null) return;

            if(Player.Instance.Character != null)
                Player.Instance.Character.Health.OnHealthChanged -= OnHealthChanged;
                
            Player.Instance.PlayerReady -= OnPlayerReady;
            Player.Instance.Character.Stats.OnBuffStarted -= UpdateStatBar;
            Player.Instance.Character.Stats.OnBuffStopped -= UpdateStatBar;
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