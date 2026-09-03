using System;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class DoorIB : InteractBehavior
    {
        public override void Interact(int posX, int posY)
        {
            var doorTd = WorldManager.Instance.FgGrid.GetTileData(posX, posY);

            if (doorTd.State == 0)
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

            WorldManager.Instance.FgGrid.ChangeMultiTileData(posX, posY, doorTd, true);
        }
    }
}