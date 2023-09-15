using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Controls a Networked Player.
/// Currently moves the player with server authoritative movement (Client Authoritative Movement is commented out).
/// Manages collision with other players.
/// </summary>
public class PlayerController : NetworkBehaviour
{
    [SerializeField, Tooltip("How fast the player should move")] private float speed = 3f;

    // Event shot out when the local client loses
    public static event System.Action GameOverEvent;
    
    #region Private Variables
    
    private Camera _mainCamera;
    private Vector3 _mouseInput = Vector3.zero;
    private PlayerLength _playerLength;
    private bool _canCollide = true;
    
    private readonly ulong[] _targetClientsArray = new ulong[1];
    
    #endregion Private Variables

    /// <summary>
    /// Initializes necessary parameters at start of player spawn.
    /// </summary>
    private void Initialize()
    {
        _mainCamera = Camera.main;
        _playerLength = GetComponent<PlayerLength>();
    }

    /// <summary>
    /// Called when the player spawns in the network.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Initialize();
    }

    /// <summary>
    /// Moves the player if they are the owner.
    /// </summary>
    private void Update()
    {
        if (!IsOwner || !Application.isFocused) return;
        MovePlayerServer();
    }

    /// <summary>
    /// Server Authoritative Movement. Send the player input to the server via a ServerRpc.
    /// </summary>
    private void MovePlayerServer()
    {
        _mouseInput.x = Input.mousePosition.x;
        _mouseInput.y = Input.mousePosition.y;
        _mouseInput.z = _mainCamera.nearClipPlane;
        Vector3 mouseWorldCoordinates = _mainCamera.ScreenToWorldPoint(_mouseInput);
        mouseWorldCoordinates.z = 0f;
        MovePlayerServerRpc(mouseWorldCoordinates);
    }

    /// <summary>
    /// Moves the player on the server.
    /// </summary>
    /// <param name="mouseWorldCoordinates">The input of the player. For a competitive game you might want to check the validity of the input.</param>
    [ServerRpc]
    private void MovePlayerServerRpc(Vector3 mouseWorldCoordinates)
    {
        // Moves the GameObject towards the mouse position in world coordinates
        transform.position = Vector3.MoveTowards(transform.position, mouseWorldCoordinates, Time.deltaTime * speed);
        
        // Rotate the GameObject towards the mouse position in world coordinates
        if (mouseWorldCoordinates != transform.position)
        {
            Vector3 targetDirection = mouseWorldCoordinates - transform.position;
            targetDirection.z = 0f;
            transform.up = targetDirection;
        }
    }
    
    /// <summary>
    /// Client Authoritative Movement
    /// Currently not being used. Call this in Update instead of MovePlayerServer() if you wish to have client authoritative movement.
    /// </summary>
    private void MovePlayerClient()
    {
        // Moves the GameObject towards the mouse position in world coordinates
        _mouseInput.x = Input.mousePosition.x;
        _mouseInput.y = Input.mousePosition.y;
        _mouseInput.z = _mainCamera.nearClipPlane;
        Vector3 mouseWorldCoordinates = _mainCamera.ScreenToWorldPoint(_mouseInput);
        mouseWorldCoordinates.z = 0f;
        transform.position = Vector3.MoveTowards(transform.position, mouseWorldCoordinates, Time.deltaTime * speed);
        
        // Rotate the GameObject towards the mouse position in world coordinates
        if (mouseWorldCoordinates != transform.position)
        {
            Vector3 targetDirection = mouseWorldCoordinates - transform.position;
            targetDirection.z = 0f;
            transform.up = targetDirection;
        }
    }

    /// <summary>
    /// Determines who is the winner of the battle by comparing the snake lengths.
    /// </summary>
    /// <param name="player1">Serialized struct with the first player data.</param>
    /// <param name="player2">Serialized struct with the second player data.</param>
    [ServerRpc]
    private void DetermineCollisionWinnerServerRpc(PlayerData player1, PlayerData player2)
    {
        if (player1.Length > player2.Length)
        {
            WinInformationServerRpc(player1.Id, player2.Id);
        }
        else
        {
            WinInformationServerRpc(player2.Id, player1.Id);
        }
    }

    /// <summary>
    /// Send the chosen clients ClientRpcs depending on their win state.
    /// If a winner, AtePlayerClientRpc is called.
    /// If a loser, GameOverClientRpc is called.
    /// A new ClientRpcParams is created and populated to call the ClientRpc on the correct client.
    /// </summary>
    /// <param name="winner"></param>
    /// <param name="loser"></param>
    [ServerRpc]
    private void WinInformationServerRpc(ulong winner, ulong loser)
    {
        _targetClientsArray[0] = winner;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = _targetClientsArray
            }
        };
        AtePlayerClientRpc(clientRpcParams);
        
        _targetClientsArray[0] = loser;
        clientRpcParams.Send.TargetClientIds = _targetClientsArray;
        GameOverClientRpc(clientRpcParams);
    }

    /// <summary>
    /// Called on the client that won the battle with the other snake.
    /// </summary>
    /// <param name="clientRpcParams">Used to only send information to a specific client.</param>
    [ClientRpc]
    private void AtePlayerClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner) return;
        Debug.Log("You Ate a Player!");
    }

    /// <summary>
    /// Called on the client that lost the battle with the other snake.
    /// </summary>
    /// <param name="clientRpcParams">Used to only send information to a specific client.</param>
    [ClientRpc]
    private void GameOverClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner) return;
        Debug.Log("You Lose!");
        GameOverEvent?.Invoke();
        NetworkManager.Singleton.Shutdown();
    }

    /// <summary>
    /// Coroutine that is called on player collision to avoid the OnCollisionEnter2D from being spammed and having too many Rpcs be called which can lead to race conditions.
    /// </summary>
    private IEnumerator CollisionCheckCoroutine()
    {
        _canCollide = false;
        yield return new WaitForSeconds(0.5f);
        _canCollide = true;
    }

    /// <summary>
    /// Called when players collide and determines who is the winner and who if the loser.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D col)
    {
        // Check if we are colliding with another player
        if (!col.gameObject.CompareTag("Player")) return;
        // Only the owner of the Networked Object can continue running the function
        if (!IsOwner) return;
        // Have we collided recently? If not, continue.
        if (!_canCollide) return;
        StartCoroutine(CollisionCheckCoroutine());
        
        // Head-on Collision
        if (col.gameObject.TryGetComponent(out PlayerLength playerLength))
        {
            // Populate the serialized structs to send Player Data to the ServerRpc
            var player1 = new PlayerData()
            {
                Id = OwnerClientId,
                Length = _playerLength.length.Value
            };
            var player2 = new PlayerData()
            {
                Id = playerLength.OwnerClientId,
                Length = playerLength.length.Value
            };
            // Called on the server from the client to determine the winner
            DetermineCollisionWinnerServerRpc(player1, player2);
        }
        // Collision with a tail GameObject. Results in a loss.
        else if (col.gameObject.TryGetComponent(out Tail tail))
        {
            WinInformationServerRpc(tail.networkedOwner.GetComponent<PlayerController>().OwnerClientId, OwnerClientId);
        }
    }

    /// <summary>
    /// An example of a serialized struct to send custom information across the server and clients. 
    /// </summary>
    struct PlayerData : INetworkSerializable
    {
        public ulong Id;
        public ushort Length;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Id);
            serializer.SerializeValue(ref Length);
        }
    }
}
