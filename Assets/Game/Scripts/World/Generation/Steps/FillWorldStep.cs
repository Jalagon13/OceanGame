using System;
using System.Collections;
using UnityEngine;

namespace SketchLand
{
    [Serializable]
    public class FillWorldStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _grassTile;
        [SerializeField] private TileConfigSO _dirtTile;
        [SerializeField] private TileConfigSO _stoneTile;
        [SerializeField] private int _dirtDepth;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            return null;
        }

    }
}
