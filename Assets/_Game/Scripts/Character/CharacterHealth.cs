using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace OceanGame
{
    [RequireComponent(typeof(ServerCharacter))]
    public class CharacterHealth : NetworkBehaviour
    {
        [HideInInspector]
        public NetworkVariable<int> CurrentHealth = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [HideInInspector]
        public NetworkVariable<LifeState> CurrentLifeState = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        public event EventHandler<HealthChangedArgs> OnHealthChanged;
        public class HealthChangedArgs : EventArgs
        {
            public int MaxHP { get; }
            public int CurrentHp { get; }
            public int PreviousHp { get; }
            
            public HealthChangedArgs(int maxHp, int currentHp, int previousHp)
            {
                MaxHP = maxHp;
                CurrentHp = currentHp;
                PreviousHp = previousHp;
            }
        }
        
        private ServerCharacter _character;
        private WaitForSeconds _iFrameDuration;

        private void Awake()
        {
            _character = GetComponent<ServerCharacter>();
            _iFrameDuration = new(_character.Data.BaseIFrameDuration);

            CurrentHealth.OnValueChanged += OnCurrentHealthChanged;
        }

        public override void OnDestroy()
        {
            CurrentHealth.OnValueChanged -= OnCurrentHealthChanged;
            
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            CurrentHealth.Value = _character.Data.BaseMaxHealth;
            CurrentLifeState.Value = LifeState.Alive;
        }

        private void OnCurrentHealthChanged(int previousValue, int newValue)
        {
            OnHealthChanged?.Invoke(this, new(_character.Data.BaseMaxHealth, CurrentHealth.Value, previousValue));
            
            if(CurrentHealth.Value <= 0)
            {
                CurrentLifeState.Value = LifeState.Dead;
            }
        }

        public void TakeDamage(int netDamage)
        {
            if(!IsServer || netDamage <= 0 || CurrentLifeState.Value == LifeState.IFrame) return;
        
            CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - netDamage);
            Debug.Log($"{name} Took {netDamage} Damage. {CurrentHealth.Value}/{_character.Data.BaseMaxHealth}");
            
            if(CurrentLifeState.Value == LifeState.Alive)
            {
                StartCoroutine(IFrameRoutine());
            }
        }

        private IEnumerator IFrameRoutine()
        {
            CurrentLifeState.Value = LifeState.IFrame;
            yield return _iFrameDuration;
            CurrentLifeState.Value = LifeState.Alive;
        }
    }
    
    
}