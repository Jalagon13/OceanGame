using UnityEngine;

namespace OceanGame
{
    public class LightManager : MonoBehaviour
    {
        public static LightManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }
        
    }
}
