using System;
using UnityEngine;

namespace OceanGame
{
    public class PlayerStatsDisplayUI : MonoBehaviour
    {
        [SerializeField] private StatBarUI _healthBar;
        
        private void Start() 
        {
            Player.Instance.Character.Health.OnHealthChanged += OnHealthChanged;
        }

        private void OnDestroy()
        {
            Player.Instance.Character.Health.OnHealthChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(object sender, CharacterHealth.HealthChangedArgs e)
        {
            _healthBar.UpdateBar(e.CurrentHp, e.MaxHP);
        }
    }
}