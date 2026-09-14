using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OceanGame
{
    public class DeathScreenUI : MonoBehaviour
    {
        [SerializeField] private GameObject _deathScreen;
        [SerializeField] private Button _respawnButton;
        [SerializeField] private float _durationTillDeathScreen = 2.5f;
        
        private WaitForSeconds _timer;
    
        private void Awake() 
        {
            _timer = new(_durationTillDeathScreen);
            _respawnButton.onClick.AddListener(() => 
            {
                OnRespawnButtonPressed();
            });

            HideDeathScreen();
        }


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
                Player.Instance.Character.Health.CurrentLifeState.OnValueChanged -= OnLifeValueChanged;
                
            Player.Instance.PlayerReady -= OnPlayerReady;
        }

        private void OnPlayerReady(ServerCharacter player)
        {
            player.Health.CurrentLifeState.OnValueChanged += OnLifeValueChanged;
        }

        private void OnLifeValueChanged(LifeState previousValue, LifeState newValue)
        {
            if(newValue == LifeState.Dead)
            {
                StartCoroutine(DeathScreenRoutine());
            }
        }

        private IEnumerator DeathScreenRoutine()
        {
            yield return _timer;

            ShowDeathScreen();
        }

        private void OnRespawnButtonPressed()
        {
            HideDeathScreen();
            Player.Instance.Character.Health.CurrentLifeState.Value = LifeState.Alive;
        }

        private void ShowDeathScreen()
        {
            _deathScreen.SetActive(true);
        }

        private void HideDeathScreen()
        {
            _deathScreen.SetActive(false);
        }
    }
}
