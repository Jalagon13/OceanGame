using System.Collections;
using TMPro;
using UnityEngine;

namespace OceanGame
{
    [RequireComponent(typeof(TextMeshPro))]
    public class DamageNumberPopup : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _duration = 0.8f;
        [SerializeField] private float _riseDistance = 0.5f;

        private TextMeshPro _label;
        private Color _initialColor;

        private void Awake()
        {
            _label = GetComponent<TextMeshPro>();
        }

        public void Initialize(int damage, Color color, float fontSize)
        {
            _initialColor = color;

            _label.text = damage.ToString();
            _label.color = color;
            _label.fontSize = fontSize;
        }

        public void Play()
        {
            StartCoroutine(AnimateAndDestroy());
        }

        private IEnumerator AnimateAndDestroy()
        {
            Vector3 startPosition = transform.position;
            float duration = Mathf.Max(0.01f, _duration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float progress = elapsed / duration;

                transform.position = startPosition + Vector3.up * (_riseDistance * progress);

                _label.color = new Color(
                    _initialColor.r,
                    _initialColor.g,
                    _initialColor.b,
                    _initialColor.a * (1f - progress));

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}