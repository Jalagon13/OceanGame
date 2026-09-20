using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace OceanGame
{
    public class PlayerArmHandler : MonoBehaviour
    {
        [SerializeField] private Transform _armPivot;
        [SerializeField] private Transform _hitboxPoint;
        [SerializeField] private SpriteRenderer _swingToolSr;

        public bool IsSwinging { get; private set; }
        public bool IsActionHeld { get; private set; }

        private Sequence _swingSequence;
        private readonly HashSet<Entity> _hitEntitiesThisSwing = new();
        private bool _isHitboxActive;
        private ToolItemSO _currentTool;

        private void Awake() 
        {
            _swingToolSr.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _swingSequence?.Kill();
        }

        private void Update()
        {
            if (!_isHitboxActive || _currentTool == null) return;
            
            CheckWeaponHits();
        }

        private void OnDrawGizmos()
        {
            if (_hitboxPoint != null && _isHitboxActive && _currentTool != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(_hitboxPoint.position, _currentTool.HitboxSize);
            }
        }

        public void StartPrimaryAction(ToolItemSO tool)
        {
            IsActionHeld = true;
            ExecuteSwing(tool);
        }
        
        public void HoldPrimaryAction(ToolItemSO tool)
        {
            ExecuteSwing(tool);
        }
        
        public void ReleasePrimaryAction(ToolItemSO tool)
        {
            IsActionHeld = false;
        }
        
        private void ExecuteSwing(ToolItemSO tool)
        {
            if (IsSwinging) return;
            // Debug.Log($"Swing started");
            
            IsSwinging = true;

            // Swing Initializaitons
            Vector2 aimDirection = (WorldManager.MouseWorldPosition - (Vector2)transform.position).normalized;
            bool isAimingRight = aimDirection.x > 0;
            float centerAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            float halfArc = tool.SwingArcAngle * 0.5f;
            float startAngle = isAimingRight ? centerAngle + halfArc : centerAngle - halfArc;
            float endAngle = isAimingRight ? centerAngle - halfArc : centerAngle + halfArc;
            float swingEndAngle = isAimingRight ? -tool.SwingEndArcAngle : tool.SwingEndArcAngle;

            // Setup
            _swingToolSr.sprite = tool.DisplayIcon;
            _hitboxPoint.localPosition = new Vector2(tool.HitboxOffset, 0);
            _armPivot.rotation = Quaternion.Euler(0f, 0f, startAngle);
            _swingToolSr.gameObject.SetActive(true);
            _currentTool = tool;

            // Entity Detection
            _hitEntitiesThisSwing.Clear();
            _isHitboxActive = true;

            // Start sequence
            _swingSequence?.Kill();
            _swingSequence = DOTween.Sequence();
            _swingSequence.Append(_armPivot.DORotate(new Vector3(0f, 0f, endAngle), tool.SwingDuration).SetEase(Ease.Linear));
            _swingSequence.AppendCallback(() => _isHitboxActive = false); // Stop damage during recovery
            _swingSequence.Append(_armPivot.DORotate(new Vector3(0f, 0f, swingEndAngle), tool.SwingEndDuration).SetRelative().SetEase(Ease.Linear));

            _swingSequence.OnComplete(() =>
            {
                EndSwing();
            });

            _swingSequence.OnKill(() =>
            {
                EndSwing();
            });
        }

        private void EndSwing()
        {
            _isHitboxActive = false;
            _currentTool = null;
            _hitEntitiesThisSwing.Clear();
            _swingToolSr.gameObject.SetActive(false);
            
            IsSwinging = false;
            // Debug.Log($"Swing completed");
        }

        private void CheckWeaponHits()
        {
            Vector2 hitCenter = _hitboxPoint.position;

            var entities = EntityManager.Instance.Entities;

            for (int i = 0; i < entities.Count; i++)
            {
                Entity entity = entities[i];
                if (entity == null) continue;

                // Skip self (Player) and already-hit targets
                if (entity == Player.Instance.Character) continue;
                if (entity is not ServerCharacter) continue;
                if (_hitEntitiesThisSwing.Contains(entity)) continue;

                // Check AABB overlap
                bool isOverlapping = GridPhysics.IsOverlapping(hitCenter, _currentTool.HitboxSize, entity.transform.position, entity.ColliderSize);

                if (isOverlapping)
                {
                    Debug.Log($"Hitting {entity.name} for {_currentTool.MeleeDamage} damage");
                    _hitEntitiesThisSwing.Add(entity);

                    if (entity is ServerCharacter enemy && enemy.DamageReceiver != null)
                    {
                        var hitData = new SyncHitData(_currentTool.MeleeDamage, _currentTool.KnockbackForce, Player.Instance.Character.transform.position);
                        enemy.DamageReceiver.ReceiveHit(hitData);
                    }
                }
            }
        }
    }
}
