using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace OceanGame
{
    public class MultiplayerManager : NetworkBehaviour
    {
        public static MultiplayerManager Instance { get; private set; }

        private readonly Dictionary<ulong, ServerCharacter> _clientCharacters = new();

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
            if (arg0.name != Loader.Scene.GameScene.ToString()) return;
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsListening) return;
            
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
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
            {
                Debug.LogWarning($"Could not find the player object for client {clientId}.");
                return;
            }

            ServerCharacter character = client.PlayerObject.GetComponent<ServerCharacter>();

            if (character == null)
            {
                Debug.LogError($"Client {clientId}'s player object has no ServerCharacter component.");
                return;
            }

            _clientCharacters[clientId] = character;
            CharacterManager.Instance.RegisterSpawner(character);

            if (NetworkManager.LocalClientId != clientId) return;

            WorldManager.Instance.WorldGen.GenerateWorld();
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!_clientCharacters.TryGetValue(clientId, out var character)) return;

            if (CharacterManager.Instance != null)
            {
                CharacterManager.Instance.UnRegisterSpawner(character);
            }

            _clientCharacters.Remove(clientId);
        }
    }
}
