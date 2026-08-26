using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New DoorTileConfigSO", menuName = "OceanGame/TileConfig/DoorTileConfigSO")]
    public class DoorTileConfigSO : TileConfigSO, IInteractable
    {
        [field: Header("Door Tile Settings")]
        [field: SerializeField] public Sprite ClosedDoorSprite { get; private set; }
        [field: SerializeField] public Sprite OpenDoorSprite { get; private set; }

        public override TileBase GetStateInterpretedTileForRendering(byte state)
        {
            AutoTile drawTile = (AutoTile)Instantiate(DrawTile);
        
            switch(state)
            {
                case 0:
                    drawTile.m_DefaultSprite = ClosedDoorSprite;
                    return drawTile;
                case 1:
                    drawTile.m_DefaultSprite = OpenDoorSprite;
                    return drawTile;
                default:
                    return DrawTile;
            }
        }
        
        public void OnInteract(int x, int y)
        {
            var doorTd = WorldManager.Instance.FgLayer.GetTileData(x, y);
            
            if(doorTd.State == 0)
            {
                Debug.Log($"Opening door");
                doorTd.State = 1;
                doorTd.IsSolid = false;
            }
            else if (doorTd.State == 1)
            {
                Debug.Log($"Closing door");
                doorTd.State = 0;
                doorTd.IsSolid = true;   
            }
            
            WorldManager.Instance.FgLayer.ChangeMultiTileData(x, y, doorTd, true);
        }
    }
}
