using UnityEngine;

namespace OceanGame
{
    public class SimulationManager : MonoBehaviour
    {
        public static SimulationManager Instance { get; private set; }
        
        private void Awake() 
        {
            Instance = this;
        }
        
        private void FixedUpdate() 
        {
            
        }
    }
}