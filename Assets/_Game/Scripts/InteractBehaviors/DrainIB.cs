using UnityEngine;

namespace OceanGame
{
    public class DrainIB : InteractBehavior
    {
        public override void Interact(int posX, int posY)
        {
            Debug.Log($"Interacting with Drain");
        }
    }
}
