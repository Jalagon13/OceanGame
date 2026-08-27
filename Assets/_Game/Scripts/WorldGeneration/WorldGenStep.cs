using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public abstract class WorldGenStep
    {
        public bool RunStep = true;

        public abstract IEnumerator Execute(WorldGenContext context);
    }
}