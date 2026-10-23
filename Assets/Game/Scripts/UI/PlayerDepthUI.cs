using System;
using TMPro;
using UnityEngine;

namespace SketchLand
{
    public class PlayerDepthUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _depthText;

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

            Player.Instance.PlayerReady -= OnPlayerReady;
            Player.Instance.DepthTracker.OnDepthChanged -= OnDepthChanged;
        }

        private void OnPlayerReady(ServerCharacter player)
        {
            Player.Instance.DepthTracker.OnDepthChanged += OnDepthChanged;
        }

        private void OnDepthChanged(int depthMeters)
        {
            _depthText.text = depthMeters == 0 ? $"Surface" : $"Depth: {depthMeters}m";
        }
    }
}