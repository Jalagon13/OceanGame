using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace OceanGame
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class ItemCollectWorldPopup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _itemText;
        [SerializeField] private Transform _plateToMove;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float _levitateHeight = 2f;
        [SerializeField, Min(0.01f)] private float _levitateDuration = 2f;
        [SerializeField, Min(0f)] private float _pauseDuration = 1f;
        [SerializeField, Min(0.01f)] private float _shrinkDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float _verticalSpacing = 0.5f;
        [SerializeField, Min(1)] private int _maxPlacementChecks = 100;

        private Collider2D _plateCollider;
        private Sequence _sequence;
        private Vector3 _initialPlateLocalPosition;
        private Vector3 _initialPlateLocalScale;
        private bool _hasPlayed;

        public int DisplayAmount { get; private set; }
        public event Action<ItemCollectWorldPopup> AnimationCompleted;

        private void Awake()
        {
            _plateCollider = GetComponent<Collider2D>();

            if (_plateToMove == null && transform.childCount > 0)
            {
                _plateToMove = transform.GetChild(0);
            }

            _initialPlateLocalPosition = _plateToMove.localPosition;
            _initialPlateLocalScale = _plateToMove.localScale;
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
            AnimationCompleted = null;
        }

        public void Initialize(ItemSO item, int amount, Color textColor, float fontSize)
        {
            if (item == null)
            {
                Debug.LogError("Cannot initialize an item collect popup with a null item.", this);
                return;
            }

            if (amount <= 0)
            {
                Debug.LogError($"Cannot initialize an item collect popup with amount {amount}.", this);
                return;
            }

            DisplayAmount = amount;
            
            _itemText.text = $"+{DisplayAmount} {item.ItemName}";
            _itemText.color = textColor;
            _itemText.fontSize = fontSize;
        }

        public void Play()
        {
            if (_hasPlayed)
            {
                return;
            }

            _hasPlayed = true;

            float verticalOffset = FindVerticalOffset();
            Vector3 targetLocalPosition = _initialPlateLocalPosition;

            transform.position += Vector3.up * verticalOffset;
            _plateToMove.localPosition = _initialPlateLocalPosition - Vector3.up * verticalOffset;
            _plateToMove.localScale = Vector3.zero;
            Physics2D.SyncTransforms();

            _sequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .Append(_plateToMove.DOScale(_initialPlateLocalScale, _levitateDuration))
                .Join(_plateToMove.DOLocalMove(targetLocalPosition, _levitateDuration))
                .AppendInterval(_pauseDuration)
                .Append(_plateToMove.DOScale(Vector3.zero, _shrinkDuration).SetEase(Ease.InFlash))
                .OnComplete(() =>
                {
                    AnimationCompleted?.Invoke(this);
                    Destroy(gameObject);
                });
        }

        private float FindVerticalOffset()
        {
            Vector3 spawnPosition = transform.position;
            Vector2 candidatePosition = spawnPosition + Vector3.up * _levitateHeight;
            Vector2 colliderSize = _plateCollider.bounds.size;

            for (int i = 0; i < _maxPlacementChecks; i++)
            {
                if (IsPlatePositionFree(candidatePosition, colliderSize))
                {
                    return candidatePosition.y - spawnPosition.y;
                }

                candidatePosition.y += _verticalSpacing;
            }

            Debug.LogWarning($"Could not find a free item popup position after {_maxPlacementChecks} checks. " + "The popup will use the last checked position.", this);

            return candidatePosition.y - spawnPosition.y;
        }

        private bool IsPlatePositionFree(Vector2 position, Vector2 size)
        {
            Collider2D[] overlappingColliders = Physics2D.OverlapBoxAll(position, size, 0f);

            foreach (Collider2D overlappingCollider in overlappingColliders)
            {
                ItemCollectWorldPopup otherPopup = overlappingCollider.GetComponentInParent<ItemCollectWorldPopup>();

                if (otherPopup != null && otherPopup != this)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
