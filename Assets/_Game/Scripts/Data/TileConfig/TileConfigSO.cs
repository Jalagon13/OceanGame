using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New TileConfigSO", menuName = "OceanGame/TileConfig/TileConfigSO")]
    public class TileConfigSO : ScriptableObject
    {
        [field: Header("Base Tile Data")]
        [field: SerializeField] public int MaxHP { get; private set; } = 50;
        [field: SerializeField] public bool Indestructible { get; private set; } = false;
        [field: SerializeField] public Vector2Int Size { get; private set; } = new(1, 1);
        [field: SerializeField] public TileBase DrawTile { get; private set; }
        [field: SerializeField] public ItemSO DroppedItem { get; private set; }
        [field: SerializeField] public bool IsSolid { get; private set; } = true;

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
