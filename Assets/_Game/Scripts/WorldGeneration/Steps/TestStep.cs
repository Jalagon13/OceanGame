using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class TestStep : WorldGenStep
    {
        [SerializeField] private float StepTimer;
        
        private WaitForSeconds _waitForSeconds3;
    
        public override IEnumerator Execute(WorldGenContext context)
        {
            _waitForSeconds3 = new(StepTimer);
            
            yield return _waitForSeconds3;
        }
    }
}
