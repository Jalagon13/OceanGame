using System;
using UnityEngine;

namespace OceanGame
{
    public class OxygenManager : MonoBehaviour
    {
        public static OxygenManager Instance { get; private set; }

        [Header("Starting Oxygen & Drain")]
        [Tooltip("Seconds to refill oxygen from 0 to full in air.")]
        [SerializeField] private float _airRefillDuration = 2.5f;
        
        [Tooltip("Oxygen consumed per second while underwater.")]
        [SerializeField] private float _underwaterDrainRate = 1f;

        [Header("Warning Threshold")]
        [Tooltip("Percentage threshold (0 to 1) where low oxygen warning fires. Example: 0.25 = 25%.")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _lowOxygenWarningPercentage = 0.25f;

        public Stat MaxOxygen { get; private set; }
        public float CurrentOxygen { get; private set; }
        public float OxygenPercentage => MaxOxygen != null && MaxOxygen.GetValue() > 0 ? CurrentOxygen / MaxOxygen.GetValue() : 0f;

        // Events
        public event Action<float, float> OnOxygenChanged; // (current, max)
        public event Action OnOxygenLowWarning; // Triggered once when dropping below warning threshold
        public event Action OnOxygenRefilled; // Triggered when oxygen recovers above warning threshold (to hide UI warnings)

        private Buff _wellOxygenatedBuff;
        private bool _hasTriggeredLowWarning;

        private void Awake()
        {
            Instance = this;
            MaxOxygen = new Stat(0);
            CurrentOxygen = 0f;
        }

        private void Start()
        {
            if (Player.Instance == null) return;

            Player.Instance.PlayerReady += OnPlayerReady;

            if (Player.Instance.Character != null)
            {
                OnPlayerReady(Player.Instance.Character);
            }
        }

        private void OnDestroy()
        {
            if (Player.Instance == null) return;

            if (Player.Instance.Character != null)
            {
                Player.Instance.Character.Health.CurrentLifeState.OnValueChanged -= OnLifeStateChanged;
            }

            Player.Instance.PlayerReady -= OnPlayerReady;
        }

        private void OnPlayerReady(ServerCharacter player)
        {
            player.Health.CurrentLifeState.OnValueChanged -= OnLifeStateChanged;
            player.Health.CurrentLifeState.OnValueChanged += OnLifeStateChanged;

            var tank = Player.Instance.Equipment != null ? Player.Instance.Equipment.CurrentOxygenTank : null;
            if (tank != null)
            {
                ApplyTankDuration(tank.OxygenDuration, tank.AdditionalHealth);
            }
            else
            {
                UpdateWellOxygenatedBuff();
            }

            OnOxygenChanged?.Invoke(CurrentOxygen, MaxOxygen.GetValue());
        }

        private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            if (previousValue == LifeState.Dead && newValue == LifeState.Alive)
            {
                RefillToMax();
            }
        }

        private void Update()
        {
            if (Player.Instance == null || Player.Instance.Character == null) return;
            if (!WorldManager.Instance.IsWorldReady) return;
            if (Player.Instance.Character.Health.CurrentLifeState.Value == LifeState.Dead) return;

            if (Player.Instance.Equipment == null || Player.Instance.Equipment.CurrentOxygenTank == null)
            {
                return;
            }

            int maxOxygen = MaxOxygen.GetValue();
            bool canBreathe = !Player.Instance.IsInWater() || Player.Instance.IsHeadInAir();

            if (canBreathe)
            {
                if (CurrentOxygen < maxOxygen)
                {
                    float refillSpeed = maxOxygen / Mathf.Max(0.1f, _airRefillDuration);
                    CurrentOxygen = Mathf.Min(maxOxygen, CurrentOxygen + refillSpeed * Time.deltaTime);
                    OnOxygenChanged?.Invoke(CurrentOxygen, maxOxygen);
                    UpdateWellOxygenatedBuff();

                    // Reset warning trigger flag once recovered above threshold
                    if (_hasTriggeredLowWarning && (CurrentOxygen / maxOxygen) > _lowOxygenWarningPercentage)
                    {
                        _hasTriggeredLowWarning = false;
                        OnOxygenRefilled?.Invoke();
                    }
                }
            }
            else
            {
                // Underwater: consume oxygen
                if (CurrentOxygen > 0f)
                {
                    CurrentOxygen = Mathf.Max(0f, CurrentOxygen - _underwaterDrainRate * Time.deltaTime);
                    OnOxygenChanged?.Invoke(CurrentOxygen, maxOxygen);
                    UpdateWellOxygenatedBuff();

                    // Check percentage threshold for warning
                    float percentage = CurrentOxygen / maxOxygen;
                    if (!_hasTriggeredLowWarning && percentage <= _lowOxygenWarningPercentage)
                    {
                        _hasTriggeredLowWarning = true;
                        OnOxygenLowWarning?.Invoke();
                    }

                }
            }
        }

        public void ApplyTankDuration(int oxygenDuration, int additionalHealth)
        {
            MaxOxygen = new Stat(Mathf.Max(0, oxygenDuration));
            CurrentOxygen = MaxOxygen.GetValue();
            _hasTriggeredLowWarning = false;

            UpdateWellOxygenatedBuff(additionalHealth);
            OnOxygenChanged?.Invoke(CurrentOxygen, MaxOxygen.GetValue());
            OnOxygenRefilled?.Invoke();
        }

        public void RemoveTankDuration()
        {
            MaxOxygen = new Stat(0);
            CurrentOxygen = 0f;
            _hasTriggeredLowWarning = false;

            UpdateWellOxygenatedBuff();
            OnOxygenChanged?.Invoke(CurrentOxygen, 0f);
            OnOxygenRefilled?.Invoke();
        }

        private void UpdateWellOxygenatedBuff(int? additionalHealth = null)
        {
            var character = Player.Instance != null ? Player.Instance.Character : null;
            if (character == null) return;

            var tank = Player.Instance.Equipment != null ? Player.Instance.Equipment.CurrentOxygenTank : null;
            int healthBonus = additionalHealth ?? (tank != null ? tank.AdditionalHealth : 0);
            bool shouldHaveBuff = tank != null && CurrentOxygen > 0f && healthBonus > 0;
            bool hasBuff = _wellOxygenatedBuff != null && _wellOxygenatedBuff.IsActive;

            if (hasBuff && (!shouldHaveBuff || _wellOxygenatedBuff.FlatAmount != healthBonus))
            {
                character.Stats.StopBuff(_wellOxygenatedBuff);
                _wellOxygenatedBuff = null;
                hasBuff = false;
            }

            if (!hasBuff && shouldHaveBuff)
            {
                _wellOxygenatedBuff = new Buff("Well Oxygenated", StatType.MaxHealth, flatAmount: healthBonus, icon: tank.DisplayIcon);
                character.Stats.StartBuff(_wellOxygenatedBuff);
            }
        }

        public void RefillToMax()
        {
            _hasTriggeredLowWarning = false;
            CurrentOxygen = MaxOxygen.GetValue();
            UpdateWellOxygenatedBuff();
            OnOxygenChanged?.Invoke(CurrentOxygen, MaxOxygen.GetValue());
            OnOxygenRefilled?.Invoke();
        }
    }
}