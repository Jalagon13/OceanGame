using System.Collections;
using UnityEngine;

namespace OceanGame
{
    public class BaseTerrainGenStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _sandSO;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            
        }
        
        
    }
}
