using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Attached to the main head Networked Player Object.
/// </summary>
public class PlayerLength : NetworkBehaviour
{
    [SerializeField, Tooltip("The tail prefab that will be spawned when the player eats the food.")] private GameObject tailPrefab;
    internal Color colorOfCollidedFood;
    
    // Synced Variable that keeps track of the player snake length.
    public NetworkVariable<ushort> length = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Called when the length has changed. This is more for local client-side updates.
    public static event System.Action<ushort> ChangedLengthEvent;
    
    #region Private Variables
    
    private List<GameObject> _tails;
    private Transform _lastTail;
    private Collider2D _collider2D;

    #endregion Private Variables
    
    /// <summary>
    /// Called when the object has been spawned in the network.
    /// Initializes values.
    /// Subscribes to the length changed event to be notified to increase tail length.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _tails = new List<GameObject>();
        _lastTail = transform;
        _collider2D = GetComponent<Collider2D>();
        if (!IsServer) length.OnValueChanged += LengthChangedEvent;
        // If there was another player already in the match, the beginning tails of them won't be updated. These lines check the length of the snake and spawn the tails of the other clients accordingly.
        if (IsOwner) return;
        for (int i = 0; i < length.Value - 1; ++i)
            InstantiateTail();
    }

    /// <summary>
    /// When the player is despawned, destroy the remaining tails.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        DestroyTails();
    }

    /// <summary>
    /// Destroys the tails from the scene.
    /// </summary>
    private void DestroyTails()
    {
        while (_tails.Count != 0)
        {
            GameObject tail = _tails[0];
            _tails.RemoveAt(0);
            Destroy(tail);
        }
    }
    
    /// <summary>
    /// Adds a length to the NetworkVariable.
    /// This will only be called on the server.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AddLengthServerRpc()
    {
        length.Value += 1;
        LengthChanged();
    }

    /// <summary>
    /// Called when the NetworkVariable length has changed.
    /// Instantiates tails on the other clients to be synchronized.
    /// </summary>
    private void LengthChanged()
    {
        InstantiateTail();

        if (!IsOwner) return;
        ChangedLengthEvent?.Invoke(length.Value);
        ClientMusicPlayer.Instance.PlayNomAudioClip();
    }

    /// <summary>
    /// Called when the NetworkVariable length has changed.
    /// </summary>
    /// <param name="previousValue">Mandatory callback parameter. Not used.</param>
    /// <param name="newValue">Mandatory callback parameter. Not used.</param>
    private void LengthChangedEvent(ushort previousValue, ushort newValue)
    {
        Debug.Log("LengthChanged Callback");
        LengthChanged();
    }

    /// <summary>
    /// Creates a new tail object.
    /// </summary>
    private void InstantiateTail()
    {
        GameObject tailGameObject = Instantiate(tailPrefab, transform.position, Quaternion.identity);
        SpriteRenderer tailRenderer = tailGameObject.GetComponent<SpriteRenderer>();
        tailRenderer.sortingOrder = -length.Value;
        tailRenderer.color = colorOfCollidedFood;
        if (tailGameObject.TryGetComponent(out Tail tail))
        {
            tail.networkedOwner = transform;
            tail.followTransform = _lastTail;
            _lastTail = tailGameObject.transform;
            Physics2D.IgnoreCollision(tailGameObject.GetComponent<Collider2D>(), _collider2D);
        }
        _tails.Add(tailGameObject);
    }
}
