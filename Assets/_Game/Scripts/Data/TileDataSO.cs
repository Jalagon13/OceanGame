using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New TileDataSO", menuName = "OceanGame/TileData/TileDataSO")]
    public class TileDataSO : ScriptableObject
    {
        [field: Header("Base Tile Data")]
        [field: SerializeField] public Vector2Int Size { get; private set; } = new(1, 1);
        [field: SerializeField] public TileBase DrawTile { get; private set; }
        [field: SerializeField] public ItemSO DroppedItem { get; private set; }

        public bool IsMultiTile => Size != new Vector2Int(1, 1);

        public ushort GetId()
        {
            return GameDataRegistry.Instance.GetTileIdFromTileDataSO(this);
        }

        public virtual TileBase GetStateInterpretedTileForRendering(byte state)
        {
            return DrawTile;
        }
    }
}
