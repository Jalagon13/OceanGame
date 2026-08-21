using System;
using UnityEngine;

namespace OceanGame
{
    public class PlayerCamera : MonoBehaviour
    {
        public static PlayerCamera Instance { get; private set; }
    
        public event Action<RectInt, RectInt> OnVisibleTileBoundsChanged;

        [SerializeField] private GameObject _player;
        [SerializeField] private Camera _camera;
        [SerializeField] private int _padding = 4;

        [Header("Movement Settings")]
        [SerializeField, Range(0.01f, 1f)] private float _smoothSpeed = 0.125f; // Lower numbers mean smoother, delayed tracking. 1f means instant locking.

        public RectInt CurrentVisibleTileBounds { get; private set; }
        
        private void Awake() 
        {
            Instance = this;    
        }

        private void LateUpdate()
        {
            ClampCameraToWorld();

            int padding = _padding;
            Vector2 bottomLeft = _camera.ViewportToWorldPoint(new Vector2(0, 0));
            Vector2 topRight = _camera.ViewportToWorldPoint(new Vector2(1, 1));

            int minX = Mathf.FloorToInt(bottomLeft.x) - padding;
            int minY = Mathf.FloorToInt(bottomLeft.y) - padding;
            int maxX = Mathf.CeilToInt(topRight.x) + padding;
            int maxY = Mathf.CeilToInt(topRight.y) + padding;

            RectInt visibleBounds = new(minX, minY, Mathf.Max(0, maxX - minX), Mathf.Max(0, maxY - minY));

            if (visibleBounds == CurrentVisibleTileBounds)
            {
                return;
            }

            RectInt previousBounds = CurrentVisibleTileBounds;

            CurrentVisibleTileBounds = visibleBounds;
            OnVisibleTileBoundsChanged?.Invoke(previousBounds, visibleBounds);
        }

        private void ClampCameraToWorld()
        {
            Vector3 targetPos = new(_player.transform.position.x, _player.transform.position.y, transform.position.z);

            var world = WorldManager.Instance;

            float camHeight = _camera.orthographicSize;
            float camWidth = camHeight * _camera.aspect;

            float minX = camWidth;
            float maxX = world.WorldWidth - camWidth;

            float minY = camHeight;
            float maxY = world.WorldHeight - camHeight;

            // Special case: If the world is smaller than the camera viewport, center it
            if (world.WorldWidth <= camWidth * 2)
            {
                targetPos.x = world.WorldWidth / 2f;
            }
            else
            {
                targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            }

            if (world.WorldHeight <= camHeight * 2)
            {
                targetPos.y = world.WorldHeight / 2f;
            }
            else
            {
                targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
            }

            transform.position = Vector3.Lerp(transform.position, targetPos, _smoothSpeed);
        }

        public void InvokeCurrentBoundsRefresh()
        {
            OnVisibleTileBoundsChanged?.Invoke(CurrentVisibleTileBounds, CurrentVisibleTileBounds);
        }
        
        public bool PositionExistsInBounds(int x, int y)
        {
            Vector2Int positionToCheck = new Vector2Int(x, y);
            return CurrentVisibleTileBounds.Contains(positionToCheck);
        }

        
    }
}
