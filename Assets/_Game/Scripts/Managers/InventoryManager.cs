using UnityEngine;

namespace OceanGame
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }
        
        private void Awake() 
        {
            Instance = this;    
        }
        
        
    }
}
