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

        [Header("Suffocation / DOT")]
        [Tooltip("Damage dealt per tick when oxygen reaches 0.")]
        [SerializeField] private int _drowningDamage = 5;
        
        [Tooltip("Interval in seconds between drowning damage ticks.")]
        [SerializeField] private float _drowningDamageInterval = 1f;
        
        [Tooltip("Debuff icon shown in BuffUI while suffocating.")]
        [SerializeField] private Sprite _drowningBuffIcon;

        public event Action<float, float> OnOxygenChanged; // (current, max)
        public Stat MaxOxygen { get; private set; }
        public float CurrentOxygen { get; private set; }

        private Buff _drowningBuff;
        private StatModifier _currentTankModifier;
        private float _drowningTimer;
        private bool _isDrowning; // Maybe make a property of this

        private void Awake()
        {
            Instance = this;
            
            MaxOxygen = new Stat(_baseMaxOxygen);
            CurrentOxygen = _baseMaxOxygen;
        }

        private void Start()
        {
            var player = Player.Instance;
        
            if (player == null) return;

            player.PlayerReady += OnPlayerReady;

            if (player.Character != null)
            {
                OnPlayerReady(Player.Instance.Character);
            }
        }

        private void OnDestroy()
        {
            var player = Player.Instance;

            if (player == null) return;

            if (player.Character != null)
            {
                player.Character.Health.CurrentLifeState.OnValueChanged -= OnLifeStateChanged;
            }

            player.PlayerReady -= OnPlayerReady;
        }

        private void OnPlayerReady(ServerCharacter player)
        {
            player.Health.CurrentLifeState.OnValueChanged -= OnLifeStateChanged;
            player.Health.CurrentLifeState.OnValueChanged += OnLifeStateChanged;

            // Broadcast initial state to UI
            OnOxygenChanged?.Invoke(CurrentOxygen, MaxOxygen.GetValue());
        }

        private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            if (newValue == LifeState.Dead)
            {
                StopDrowning();
            }
            else if (previousValue == LifeState.Dead && newValue == LifeState.Alive)
            {
                // Refill oxygen when player respawns
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

            if (canBreathe) // Surfaced or on land: stop suffocating and refill rapidly
            {
                if (_isDrowning)
                {
                    StopDrowning();
                }

                if (CurrentOxygen < maxOxygen)
                {
                    float refillSpeed = maxOxygen / Mathf.Max(0.1f, _airRefillDuration);
                    CurrentOxygen = Mathf.Min(maxOxygen, CurrentOxygen + refillSpeed * Time.deltaTime);
                    OnOxygenChanged?.Invoke(CurrentOxygen, maxOxygen);
                }
            }
            else // Submerged: drain oxygen
            {
                if (CurrentOxygen > 0f)
                {
                    CurrentOxygen = Mathf.Max(0f, CurrentOxygen - _underwaterDrainRate * Time.deltaTime);
                    OnOxygenChanged?.Invoke(CurrentOxygen, maxOxygen);
                }

                // Empty oxygen: suffocation debuff & DOT
                if (CurrentOxygen <= 0f)
                {
                    if (!_isDrowning)
                    {
                        StartDrowning();
                    }

                    _drowningTimer -= Time.deltaTime;
                    if (_drowningTimer <= 0f)
                    {
                        _drowningTimer = _drowningDamageInterval;
                        Player.Instance.Character.Health.TakeDamage(_drowningDamage, triggerIFrame: false);
                    }
                }
            }
        }

        private void StartDrowning()
        {
            _isDrowning = true;
            _drowningTimer = _drowningDamageInterval;

            if (_drowningBuff == null)
            {
                // Flat/percent 0 won't modify any stats, but registers the active debuff icon in BuffUI
                _drowningBuff = new Buff("Suffocating", StatType.MoveSpeed, flatAmount: 0, percentAmount: 0f, duration: -1f, icon: _drowningBuffIcon); // Could just cache this on awake or something but like its fine right now like this
            }

            Player.Instance.Character.Stats.StartBuff(_drowningBuff);
        }

        private void StopDrowning()
        {
            _isDrowning = false;
            _drowningTimer = 0f;

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

            // Clamp current oxygen so it doesn't exceed the lower max
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
            CurrentOxygen = MaxOxygen.GetValue();
            OnOxygenChanged?.Invoke(CurrentOxygen, MaxOxygen.GetValue());
        }
    }
}