using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New TileConfigSO", menuName = "OceanGame/TileConfig/TileConfigSO")]
    public class TileConfigSO : ScriptableObject
    {
        [field: Header("Base Tile Data")]
        [field: SerializeField] public int MaxHP { get; private set; } = 50;
        [field: SerializeField, Range(0, 1f)] public float LightLevel { get; private set; } = 0;
        [field: SerializeField] public bool Indestructible { get; private set; } = false;
        [field: SerializeField] public bool IsSolid { get; private set; } = true;
        [field: SerializeField] public Vector2Int Size { get; private set; } = new(1, 1);
        [field: SerializeField] public TileBase DrawTile { get; private set; }
        [field: SerializeField] public ItemSO DroppedItem { get; private set; }
        [field: SerializeReference]
        [field: SerializeField] public InteractBehavior InteractBehavior { get; private set; }

        public bool IsMultiTile => Size != new Vector2Int(1, 1);

        public ushort GetId()
        {
            return GameDataRegistry.Instance.GetTileIdFromTileDataSO(this);
        }

        public virtual TileBase GetStateInterpretedTileForRendering(byte state)
        {
            return DrawTile;
        }

        // In TileConfigSO.cs
        public virtual void OnTileDestroyed(TileGrid grid, int x, int y, TileData tileData, bool refreshCurrentBounds = false)
        {
            if (IsMultiTile)
            {
                // Default Rigid Multi-Tile behavior (e.g. standard multi-tile furniture)
                int rootX = x - tileData.OffsetX;
                int rootY = y - tileData.OffsetY;

                for (int ox = 0; ox < Size.x; ox++)
                {
                    for (int oy = 0; oy < Size.y; oy++)
                    {
                        int targetX = rootX + ox;
                        int targetY = rootY + oy;

                        if (grid.IsInBounds(targetX, targetY))
                        {
                            grid.SetTileDataDirect(targetX, targetY, TileData.Air);
                            grid.ClearDamage(targetX, targetY);
                        }
                    }
                }

                if (DroppedItem != null)
                {
                    Vector2 dropPos = new Vector2(rootX + 0.5f, rootY + 0.5f);
                    GameManager.Instance.SpawnItem(DroppedItem, 1, dropPos);
                }
            }
            else
            {
                // Standard 1x1 tile behavior
                grid.SetTileDataDirect(x, y, TileData.Air);
                grid.ClearDamage(x, y);

                if (DroppedItem != null)
                {
                    Vector2 dropPos = new Vector2(x + 0.5f, y + 0.5f);
                    GameManager.Instance.SpawnItem(DroppedItem, 1, dropPos);
                }
            }
        }
    }
}
