using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ArenaSpawnSystem : MonoBehaviour
{
    [Header("Spawn Points (assign in Inspector if possible)")]
    [SerializeField] private Transform spawnA; // Host/Server
    [SerializeField] private Transform spawnB; // First Client

    void Awake() => ResolveSpawns();

    void OnEnable()
    {
        var nm = NetworkManager.Singleton;
        if (!nm) return;

        nm.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        nm.OnServerStarted += OnServerStarted;
        nm.OnClientConnectedCallback += OnClientConnected;
    }

    void OnDisable()
    {
        var nm = NetworkManager.Singleton;
        if (!nm) return;

        nm.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        nm.OnServerStarted -= OnServerStarted;
        nm.OnClientConnectedCallback -= OnClientConnected;
    }

    // When host/server starts (already in the arena scene)
    private void OnServerStarted()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        ResolveSpawns();
        PlaceAllConnectedPlayers();
    }

    // When Netcode finishes loading THIS arena scene for everyone
    private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode,
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (sceneName != gameObject.scene.name) return; // only react for this scene

        ResolveSpawns();
        PlaceAllConnectedPlayers();
    }

    // Late join / reconnect
    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        ResolveSpawns();
        PlacePlayer(clientId);
    }

    private void PlaceAllConnectedPlayers()
    {
        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
            PlacePlayer(kv.Key);
    }

    private void PlacePlayer(ulong clientId)
    {
        var cc = NetworkManager.Singleton.ConnectedClients[clientId];
        var player = cc?.PlayerObject;
        if (!player) return;

        Transform target = (clientId == NetworkManager.ServerClientId) ? spawnA : spawnB;
        if (!target)
        {
            Debug.LogWarning("[ArenaSpawnSystem] Missing spawn transform.");
            return;
        }

        player.transform.SetPositionAndRotation(target.position, target.rotation);

        // kill any falling momentum
        if (player.TryGetComponent<Rigidbody2D>(out var rb2d))
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
        }

        // Optional: face inwards
        var s = player.transform.localScale;
        s.x = (clientId == NetworkManager.ServerClientId) ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        player.transform.localScale = s;
    }

    private void ResolveSpawns()
    {
        if (spawnA && spawnB) return;

        // Prefer a "SpawnPoints" root if you have one
        var root = GameObject.Find("SpawnPoints");
        if (root)
        {
            if (!spawnA) spawnA = root.transform.Find("Spawn_A");
            if (!spawnB) spawnB = root.transform.Find("Spawn_B");
        }

        // Fallback: find by name (no "/" paths!)
        if (!spawnA) spawnA = GameObject.Find("Spawn_A")?.transform;
        if (!spawnB) spawnB = GameObject.Find("Spawn_B")?.transform;
    }
}
