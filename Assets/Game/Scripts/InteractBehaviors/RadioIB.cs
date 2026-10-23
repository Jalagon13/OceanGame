using UnityEngine;

namespace SketchLand
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
