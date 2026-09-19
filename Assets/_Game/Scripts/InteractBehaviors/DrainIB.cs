using System;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class DrainIB : InteractBehavior
    {
        [SerializeField] private int _drainLimit = 16;
    
        public override void Interact(int posX, int posY)
        {
            if (DrainManager.Instance != null)
            {
                DrainManager.Instance.InteractWithDrain(posX, posY, _drainLimit);
            }
        }
    }
}
