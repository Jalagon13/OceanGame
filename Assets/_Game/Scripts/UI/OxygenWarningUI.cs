using DG.Tweening;
using TMPro;
using UnityEngine;

namespace OceanGame
{
    public class OxygenWarningUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text or panel showing 'OXYGEN LOW'.")]
        [SerializeField] private CanvasGroup _warningCanvasGroup;
        [SerializeField] private TextMeshProUGUI _warningText;

        [Header("Animation Settings")]
        [SerializeField] private float _flashDuration = 0.5f;
        [SerializeField] private int _flashLoops = 6; // Flashes for 3 seconds

        private Tween _fadeTween;

        private void Awake()
        {
            if (_warningCanvasGroup != null)
            {
                _warningCanvasGroup.alpha = 0f;
            }
        }

        private void Start()
        {
            if (OxygenManager.Instance != null)
            {
                OxygenManager.Instance.OnOxygenLowWarning += ShowLowOxygenWarning;
                OxygenManager.Instance.OnOxygenRefilled += HideWarning;
            }
        }

        private void OnDestroy()
        {
            if (OxygenManager.Instance != null)
            {
                OxygenManager.Instance.OnOxygenLowWarning -= ShowLowOxygenWarning;
                OxygenManager.Instance.OnOxygenRefilled -= HideWarning;
            }

            _fadeTween?.Kill();
        }

        private void ShowLowOxygenWarning()
        {
            if (_warningCanvasGroup == null) return;

            _fadeTween?.Kill();

            if (_warningText != null)
            {
                _warningText.text = "OXYGEN LOW";
            }

            // Flashes the canvas group alpha between 0 and 1 using DOTween
            _warningCanvasGroup.alpha = 0f;
            _fadeTween = _warningCanvasGroup.DOFade(1f, _flashDuration)
                .SetLoops(_flashLoops, LoopType.Yoyo)
                .OnComplete(() => _warningCanvasGroup.alpha = 0f);
        }

        private void HideWarning()
        {
            _fadeTween?.Kill();
            if (_warningCanvasGroup != null)
            {
                _warningCanvasGroup.alpha = 0f;
            }
        }
    }
}