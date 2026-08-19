using System;
using System.Collections;
using UnityEngine;

namespace LittleGuyGamePrototype
{
    public class HealthHandler : MonoBehaviour
    {
        public event Action OnDeath;
        public event EventHandler<DamageEventArgs> OnDamage;
        public class DamageEventArgs : EventArgs
        {
            public GameObject Inflicter;
            public int DamageDone;
            public bool PlayKnockback;
            public float KnockbackForce;
            
            public DamageEventArgs(GameObject inflicter, int damageDone, bool playKnockback, float knockbackForce = -1)
            {
                Inflicter = inflicter;
                DamageDone = damageDone;
                PlayKnockback = playKnockback;
                KnockbackForce = knockbackForce;
            }
        }

        public enum LifeState { Alive, Dead, Invincible }

        private int _maxHealth;
        public int MaxHealth => _maxHealth;
        
        public int CurrentHealth { get; private set; }
        public LifeState CurrentLifeState { get; private set; } = LifeState.Alive;
        
        private float _iFrameDuration;

        public void Initialize(int maxHealthValue, float iFrameDuration)
        {
            _maxHealth = maxHealthValue;
            _iFrameDuration = iFrameDuration;
            CurrentHealth = _maxHealth;
            CurrentLifeState = LifeState.Alive;
        }

        public void TryTakeDamage(GameObject inflicter, int amount, bool playknockback, float knockbackPower = -1)
        {
            if (CurrentLifeState == LifeState.Dead || CurrentLifeState == LifeState.Invincible) return;
            if (amount <= 0) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            Debug.Log($"{gameObject.name} took {amount} damage. Health: {CurrentHealth}/{_maxHealth}");
            
            OnDamage?.Invoke(this, new DamageEventArgs(inflicter, amount, playknockback, knockbackPower));

            if (CurrentHealth <= 0)
            {
                Die();
            }
            else
            {
                // Knockback stuff here
            
                StartCoroutine(IFrameHandling());
            }
        }

        private IEnumerator IFrameHandling()
        {
            CurrentLifeState = LifeState.Invincible;
            float elapsed = 0f;

            while (elapsed < _iFrameDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            CurrentLifeState = LifeState.Alive;
        }

        public void Heal(int amount)
        {
            if (CurrentLifeState == LifeState.Dead) return;
            if (amount <= 0) return;

            int oldHealth = CurrentHealth;
            CurrentHealth = Mathf.Min(_maxHealth, CurrentHealth + amount);
        }

        public void SetLifeState(LifeState newState)
        {
            if (CurrentLifeState == LifeState.Dead) return;
            CurrentLifeState = newState;
        }

        private void Die()
        {
            CurrentLifeState = LifeState.Dead;
            OnDeath?.Invoke();
        }
    }
}