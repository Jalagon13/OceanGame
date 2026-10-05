using System;
using UnityEngine;

namespace OceanGame
{
    public class OxygenManager : MonoBehaviour
    {
        public static OxygenManager Instance { get; private set; }

        [Header("Starting Oxygen & Drain")]
        [Tooltip("Base oxygen capacity when no tank is equipped.")]
        [SerializeField] private int _baseMaxOxygen = 45;
        
        [Tooltip("Seconds to refill oxygen from 0 to full in air.")]
        [SerializeField] private float _airRefillDuration = 2.5f;
        
        [Tooltip("Oxygen consumed per second while underwater.")]
        [SerializeField] private float _underwaterDrainRate = 1f;

        [Header("Warning Threshold")]
        [Tooltip("Percentage threshold (0 to 1) where low oxygen warning fires. Example: 0.25 = 25%.")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _lowOxygenWarningPercentage = 0.25f;

        [Header("Suffocation & Grace Period")]
        [Tooltip("Grace period in seconds after oxygen reaches 0 before damage/suffocation begins.")]
        [SerializeField] private float _suffocationGraceDuration = 3f;
        
        [Tooltip("Damage dealt per tick once the grace period expires.")]
        [SerializeField] private int _drowningDamage = 5;
        
        [Tooltip("Interval in seconds between drowning damage ticks.")]
        [SerializeField] private float _drowningDamageInterval = 1f;
        
        [Tooltip("Debuff icon shown in BuffUI while suffocating.")]
        [SerializeField] private Sprite _drowningBuffIcon;

        public Stat MaxOxygen { get; private set; }
        public float CurrentOxygen { get; private set; }
        public float OxygenPercentage => MaxOxygen != null && MaxOxygen.GetValue() > 0 ? CurrentOxygen / MaxOxygen.GetValue() : 0f;

        // Events
        public event Action<float, float> OnOxygenChanged; // (current, max)
        public event Action OnOxygenLowWarning; // Triggered once when dropping below warning threshold
        public event Action OnOxygenRefilled; // Triggered when oxygen recovers above warning threshold (to hide UI warnings)

        private Buff _drowningBuff;
        private StatModifier _currentTankModifier;
        private float _drowningDamageTimer;
        private float _graceTimer;
        private bool _isDrowning;
        private bool _hasTriggeredLowWarning;

        private void Awake()
        {
            Instance = this;
            MaxOxygen = new Stat(_baseMaxOxygen);
            CurrentOxygen = _baseMaxOxygen;
            _graceTimer = _suffocationGraceDuration;
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

            OnOxygenChanged?.Invoke(CurrentOxygen, MaxOxygen.GetValue());
        }

        private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            if (newValue == LifeState.Dead)
            {
                StopDrowning();
                ResetGracePeriod();
            }
            else if (previousValue == LifeState.Dead && newValue == LifeState.Alive)
            {
                RefillToMax();
            }
        }

        private void Update()
        {
            if (Player.Instance == null || Player.Instance.Character == null) return;
            if (!WorldManager.Instance.IsWorldReady) return;
            if (Player.Instance.Character.Health.CurrentLifeState.Value == LifeState.Dead) return;

            int maxOxygen = MaxOxygen.GetValue();
            bool canBreathe = !Player.Instance.IsInWater() || Player.Instance.IsHeadInAir();

            if (canBreathe)
            {
                // In air: stop drowning, reset timers
                if (_isDrowning)
                {
                    StopDrowning();
                }
                
                ResetGracePeriod();

                if (CurrentOxygen < maxOxygen)
                {
                    float refillSpeed = maxOxygen / Mathf.Max(0.1f, _airRefillDuration);
                    CurrentOxygen = Mathf.Min(maxOxygen, CurrentOxygen + refillSpeed * Time.deltaTime);
                    OnOxygenChanged?.Invoke(CurrentOxygen, maxOxygen);

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

                    // Check percentage threshold for warning
                    float percentage = CurrentOxygen / maxOxygen;
                    if (!_hasTriggeredLowWarning && percentage <= _lowOxygenWarningPercentage)
                    {
                        _hasTriggeredLowWarning = true;
                        OnOxygenLowWarning?.Invoke();
                    }

                    // While player still has oxygen, keep grace period timer reset
                    ResetGracePeriod();
                }
                else
                {
                    // Oxygen reached 0: Count down grace period
                    if (_graceTimer > 0f)
                    {
                        _graceTimer -= Time.deltaTime;
                    }
                    else
                    {
                        // Grace period reached 0 -> Start DOT and Buff
                        if (!_isDrowning)
                        {
                            StartDrowning();
                        }

                        _drowningDamageTimer -= Time.deltaTime;
                        if (_drowningDamageTimer <= 0f)
                        {
                            _drowningDamageTimer = _drowningDamageInterval;
                            Player.Instance.Character.Health.TakeDamage(_drowningDamage, triggerIFrame: false);
                        }
                    }
                }
            }
        }

        private void ResetGracePeriod()
        {
            _graceTimer = _suffocationGraceDuration;
        }

        private void StartDrowning()
        {
            _isDrowning = true;
            _drowningDamageTimer = _drowningDamageInterval;

            if (_drowningBuff == null)
            {
                _drowningBuff = new Buff("Suffocating", StatType.MoveSpeed, flatAmount: 0, percentAmount: 0f, duration: -1f, icon: _drowningBuffIcon);
            }

            Player.Instance.Character.Stats.StartBuff(_drowningBuff);
        }

        private void StopDrowning()
        {
            _isDrowning = false;
            _drowningDamageTimer = 0f;

            if (_drowningBuff != null && Player.Instance.Character != null)
            {
                Player.Instance.Character.Stats.StopBuff(_drowningBuff);
            }
        }

        public void ApplyTankModifier(int additionalOxygen)
        {
            RemoveTankModifier();

            if (additionalOxygen > 0)
            {
                _currentTankModifier = new StatModifier(StatModifierType.Flat, additionalOxygen);
                MaxOxygen.AddModifier(_currentTankModifier);
            }

            OnOxygenChanged?.Invoke(CurrentOxygen, MaxOxygen.GetValue());
        }

        public void RemoveTankModifier()
        {
            if (_currentTankModifier.Value > 0)
            {
                MaxOxygen.RemoveModifier(_currentTankModifier);
                _currentTankModifier = default;
            }

            int max = MaxOxygen.GetValue();
            if (CurrentOxygen > max)
            {
                CurrentOxygen = max;
            }

            OnOxygenChanged?.Invoke(CurrentOxygen, max);
        }

        public void RefillToMax()
        {
            StopDrowning();
            ResetGracePeriod();
            _hasTriggeredLowWarning = false;
            CurrentOxygen = MaxOxygen.GetValue();
            OnOxygenChanged?.Invoke(CurrentOxygen, MaxOxygen.GetValue());
            OnOxygenRefilled?.Invoke();
        }
    }
}