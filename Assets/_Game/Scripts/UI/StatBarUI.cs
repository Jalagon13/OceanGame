using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OceanGame
{
    public class StatBarUI : MonoBehaviour
    {
        [SerializeField] private Image _barFg;
        [SerializeField] private TextMeshProUGUI _barText;
        
        public void UpdateBar(float currentValue, float maxValue)
        {
            float fill = Mathf.Clamp01(currentValue / maxValue);
            _barFg.fillAmount = fill;

            if(_barText != null)
            {
                int current = Mathf.RoundToInt(currentValue);
                int max = Mathf.RoundToInt(maxValue);
            
                _barText.text = $"{current}/{max}";
            }
        }
    }
}
