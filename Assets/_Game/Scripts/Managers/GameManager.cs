using UnityEngine;

namespace OceanGame
{
    public class GameManager : MonoBehaviour
    {
       public static GameManager Instance { get; private set; }
       
       [SerializeField] private Item _itemPrefab;
       
       private void Awake() 
       {
            Instance = this; 
       }
       
       public void SpawnItem(ItemSO itemSO, int amount, Vector2 position, Vector2 startingVector = default)
       {
          //   Debug.Log($"Spawning item");
            Item itemToSpawn = Instantiate(_itemPrefab, position, Quaternion.identity);
            itemToSpawn.InitializeItem(new(itemSO, amount), startingVector);
       }
    }
}
