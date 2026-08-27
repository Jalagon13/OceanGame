using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New TileitemSO", menuName = "OceanGame/Item/TileItemSO")]
    public class TileItemSO : ItemSO
    {
        [field: Header("Tile Item Settings")]
        [field: SerializeField] public TileConfigSO PlaceTileDataSO { get; private set; }

        public override void OnPrimaryActionStarted()
        {
            PlacingManager.Instance.StartPlacing(this);
        }

        public override void OnPrimaryActionHeld()
        {
            PlacingManager.Instance.TickPlacing(this);
        }

        public override void OnPrimaryActionRelease()
        {
            PlacingManager.Instance.StopPlacing();
        }
        
    }
}