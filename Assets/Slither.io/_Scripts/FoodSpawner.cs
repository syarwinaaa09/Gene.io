using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Spawns food when the server starts and continues spawning food as long as there is at least one player, and the max amount of food on the map does not exceed the MaxPrefabCount.
/// Uses a Network Object Pool for performance increase.
/// </summary>
public class FoodSpawner : MonoBehaviour
{
    [SerializeField, Tooltip("The prefab to spawn")] private GameObject[] prefabs;
    // Maximum amount of prefab objects to have active in the scene
    private const int MaxPrefabCount = 30;
    
    private void Start()
    {
        // When the server starts, spawn the food
        NetworkManager.Singleton.OnServerStarted += SpawnFoodStart;
    }

    /// <summary>
    /// Initializes the Network Object Pool, spawns the first initial set of food, and starts the Coroutine to continue spawning food over time.
    /// </summary>
    private void SpawnFoodStart()
    {
        NetworkManager.Singleton.OnServerStarted -= SpawnFoodStart;
        NetworkObjectPool.Singleton.InitializePool();
        for (int i = 0; i < MaxPrefabCount; ++i)
        {
            SpawnFood();
        }
        StartCoroutine(SpawnOverTime());
    }

    /// <summary>
    /// Retrieve the food from the object pool and spawn it on the scene.
    /// </summary>
    private void SpawnFood()
    {
        GameObject selectedPrefab = prefabs[Random.Range(0, prefabs.Length)];
        NetworkObject obj =
                NetworkObjectPool.Singleton.GetNetworkObject(selectedPrefab, GetRandomPositionOnMap(), Quaternion.identity);
        // obj.GetComponent<Food>().prefab = selectedPrefab;
        if (!obj.IsSpawned) obj.Spawn(true);
    }

    /// <summary>
    /// Returns a random position on the map for the food to be spawned.
    /// </summary>
    private Vector3 GetRandomPositionOnMap()
    {
        return new Vector3(Random.Range(-9f, 9f), Random.Range(-5f, 5f), 0f);
    }

    /// <summary>
    /// Spawns food over time. Checks if there is at least one client and that the max amount of food on the map does not exceed the MaxPrefabCount.
    /// </summary>
    private IEnumerator SpawnOverTime()
    {
        while (NetworkManager.Singleton.ConnectedClients.Count > 0)
        {
            yield return new WaitForSeconds(2f);
            GameObject selectedPrefab = prefabs[Random.Range(0, prefabs.Length)];
            if (NetworkObjectPool.Singleton.GetCurrentPrefabCount(selectedPrefab) < MaxPrefabCount)
                SpawnFood();
        }
    }
}
