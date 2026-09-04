using UnityEngine;

namespace OceanGame
{
    public class DrainIB : InteractBehavior
    {
        [SerializeField] private int _drainLimit = 16;
    
        public override void Interact(int posX, int posY)
        {
            AirPocketManager.Instance.TryToDrain(posX, posY, _drainLimit);
        }
    }
}
