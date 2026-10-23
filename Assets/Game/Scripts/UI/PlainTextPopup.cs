using System.Collections;
using TMPro;
using UnityEngine;

namespace SketchLand
{
    [RequireComponent(typeof(TextMeshPro))]
    public class PlainTextPopup : MonoBehaviour
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
            Initialize(damage.ToString(), color, fontSize);
        }

        public void Initialize(string text, Color color, float fontSize)
        {
            _initialColor = color;
            _label.text = text;
            _label.color = new Color(color.r, color.g, color.b, 0.5f);
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

                _label.color = new Color(_initialColor.r, _initialColor.g, _initialColor.b, Mathf.Lerp(0.75f, 1f, progress));

                elapsed += Time.deltaTime;
                yield return null;
            }

            _label.color = new Color(_initialColor.r, _initialColor.g, _initialColor.b, 1f);
            Destroy(gameObject);
        }
    }
}