using UnityEngine;

namespace OceanGame
{
    public class CharacterManager : MonoBehaviour
    {
        public static CharacterManager Instance { get; private set; }
        
        private void Awake() 
        {
            Instance = this;    
        }

        
    }
}
