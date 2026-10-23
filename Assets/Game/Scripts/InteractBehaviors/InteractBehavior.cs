using System;
using System.Collections;
using UnityEngine;

namespace SketchLand
{
    [Serializable]
    public abstract class InteractBehavior
    {
        public abstract void Interact(int posX, int posY);
    }
}