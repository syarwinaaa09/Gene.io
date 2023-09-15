using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Attached to the Networked Food object. 
/// </summary>
public class Food : NetworkBehaviour
{
    [Tooltip("The original prefab of this GameObject. Is set through the FoodSpawner script.")] public GameObject prefab;
    [SerializeField] private SpriteRenderer spriteRenderer;
    /// <summary>
    /// On collision with the player, adds a tail length and despawns the object from the scene.
    /// We check if we are the server before adding the tail length.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        if (!NetworkManager.Singleton.IsServer) return;

        if (col.TryGetComponent(out PlayerLength playerLength))
        {
            playerLength.colorOfCollidedFood = spriteRenderer.color;
            playerLength.AddLengthServerRpc();
        }
        //else if (col.TryGetComponent(out Tail tail))
        //{
        //    tail.networkedOwner.GetComponent<PlayerLength>().AddLengthServerRpc();
        //}
        NetworkObjectPool.Singleton.ReturnNetworkObject(NetworkObject, prefab);
        NetworkObject.Despawn();
    }
}
