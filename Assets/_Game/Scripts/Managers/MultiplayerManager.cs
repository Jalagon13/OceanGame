using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OceanGame
{
    public class MultiplayerManager : NetworkBehaviour
    {
        public static MultiplayerManager Instance { get; private set; }
        
        private void Awake() 
        {
            Instance = this;    
        }
        
        private void OnEnable() 
        {
            SceneManager.sceneLoaded += HandleNetworkLoad;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleNetworkLoad;
        }

        private void HandleNetworkLoad(Scene arg0, LoadSceneMode arg1)
        {
            if (NetworkManager.Singleton == null) return;
            
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            if (Loader.IsHost)
            {
                Debug.Log($"Starting game as host");
                NetworkManager.Singleton.StartHost();
                
            }
            else
            {
                Debug.Log($"Starting game as client");
                NetworkManager.Singleton.StartClient();
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            CharacterManager.Instance.RegisterSpawner(NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<ServerCharacter>());
        
            if (NetworkManager.LocalClientId != clientId) return;

            WorldManager.Instance.WorldGen.GenerateWorld();
        }

        private void OnClientDisconnected(ulong clientId)
        {
            CharacterManager.Instance.UnRegisterSpawner(NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<ServerCharacter>());
        }
    }
}
