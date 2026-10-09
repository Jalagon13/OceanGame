using UnityEngine;

namespace OceanGame
{
    public class RadioIB : InteractBehavior
    {
        public override void Interact(int posX, int posY)
        {
            // End the game here
            GameManager.Instance.EndPrototype();
        }
    }
}
