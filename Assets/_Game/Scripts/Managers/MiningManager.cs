using UnityEngine;

namespace OceanGame
{
    public class MiningManager : MonoBehaviour
    {
        public static MiningManager Instance { get; private set; }

        private float _nextMineTime;
        private bool _isMining;

        private void Awake()
        {
            Instance = this;
        }

        public void StartMining(ToolItemSO tool)
        {
            _isMining = true;
            
            TryPerformMine(tool); // Instantly attempt a mine on the first click
        }

        public void TickMining(ToolItemSO tool)
        {
            if (!_isMining) return;

            TryPerformMine(tool);
        }

        public void StopMining()
        {
            _isMining = false;
        }

        private bool TryPerformMine(ToolItemSO tool)
        {
            if (tool == null) return false;
            if (Time.time < _nextMineTime) return false; // Enforce tool cooldown even when spam-clicking
            if (WorldManager.Instance == null || WorldManager.Instance.MouseOverUI) return false;

            Vector2 mouseWorldPos = WorldManager.MouseWorldPosition;
            Vector2Int mouseTilePos = WorldManager.MouseWorldTilePosition;

            float distanceToPlayer = Vector2.Distance(Player.Instance.transform.position, mouseWorldPos);
            if (distanceToPlayer > Player.Instance.InteractRange) return false;

            var layer = tool.TargetLayer == WorldManager.LayerType.Foreground ? WorldManager.Instance.FgGrid : WorldManager.Instance.BgGrid;
            var tileData = layer.GetTileData(mouseTilePos.x, mouseTilePos.y);

            if (tileData.HasTile)
            {
                WorldManager.Instance.DamageTile(mouseTilePos, tool.MiningDamage, tool.TargetLayer, refreshCurrentBounds: true);

                float interval = tool.MineTicksPerSecond > 0f ? 1f / tool.MineTicksPerSecond : 0.25f;
                _nextMineTime = Time.time + interval;
                return true;
            }

            return false;
        }
    }
}
