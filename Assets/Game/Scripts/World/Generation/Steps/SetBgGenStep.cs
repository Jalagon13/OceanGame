using System.Collections;
using UnityEngine;

namespace SketchLand
{
    public class SetBgGenStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _dirtTile;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            return null;
        }
    }
}