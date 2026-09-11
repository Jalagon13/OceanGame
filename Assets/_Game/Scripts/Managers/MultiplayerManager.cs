using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OceanGame
{
    public class MultiplayerManager : MonoBehaviour
    {
        public static MultiplayerManager Instance { get; private set; }
        
        private void Awake() 
        {
            Instance = this;    
        }
        
        private void Start() 
        {
            SceneManager.sceneLoaded += HandleNetworkLoad;
        }
        
        private void OnDestroy() 
        {
            SceneManager.sceneLoaded -= HandleNetworkLoad;
        }

        private void HandleNetworkLoad(Scene arg0, LoadSceneMode arg1)
        {
            if (Loader.IsHost)
            {
                Debug.Log($"Starting game as host");
                NetworkManager.Singleton.StartHost();
                WorldManager.Instance.WorldGen.GenerateWorld();
            }
            else
            {
                Debug.Log($"Starting game as client");
                NetworkManager.Singleton.StartClient();
            }
        }
    }
}
