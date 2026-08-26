using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New DoorTileConfigSO", menuName = "OceanGame/TileConfig/DoorTileConfigSO")]
    public class DoorTileConfigSO : TileConfigSO
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
        
        public void SetTileData(int x, int y, bool refreshCurrentBounds = false)
        {
            var fgLayer = WorldManager.Instance.FgLayer;
            var currentTileData = fgLayer.GetTileData(x, y);
            var newTileData = currentTileData;
            
            if(currentTileData.State == 0) // 0 is closed so turn it to open
            {
                newTileData.State = 1;
                newTileData.IsSolid = false;
            }
            else if(currentTileData.State == 1) // 1 is open so close it
            {
                newTileData.State = 0;
                newTileData.IsSolid = true;
            }
            
            fgLayer.SetMultiTileData(x, y, newTileData, refreshCurrentBounds);
        }

        // public void OnInteract(TileData td)
        // {
        //     Debug.Log($"Interacting with door");
        //     if(td.State == 0) 
        //     {
        //         WorldManager.Instance.FgLayer.SetMultiTileState()
        //     }
        //     else if(td.State == 1) td.State = 0;
        // }
    }
}
