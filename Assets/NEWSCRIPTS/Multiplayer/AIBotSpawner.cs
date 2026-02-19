using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class AIBotSpawner : MonoBehaviour
{
    [Header("Bot Prefab (must have NetworkObject)")]
    [SerializeField] private NetworkObject botPrefab;

    [Header("Spawns")]
    [SerializeField] private Transform spawnA; // left
    [SerializeField] private Transform spawnB; // right

    private NetworkObject botInstance;

    void Start()
    {
        StartCoroutine(WaitThenSpawn());
    }

    IEnumerator WaitThenSpawn()
    {
        // Wait until NetworkManager exists and we're the server/host
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || !NetworkManager.Singleton.IsListening)
            yield return null;

        // Find spawns by name if not wired
        if (!spawnA || !spawnB)
        {
            var root = GameObject.Find("SpawnPoints");
            if (root)
            {
                var a = root.transform.Find("Spawn_A");
                var b = root.transform.Find("Spawn_B");
                if (a) spawnA = a;
                if (b) spawnB = b;
            }
        }

        // If 2 humans are connected, no bot needed
        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= 2)
            yield break;

        // Put human on A (your ArenaSpawnSystem), so spawn bot on B
        Vector3 pos = spawnB ? spawnB.position : Vector3.zero;
        Quaternion rot = spawnB ? spawnB.rotation : Quaternion.identity;

        botInstance = Instantiate(botPrefab, pos, rot);
        botInstance.Spawn(true); // server-owned
        botInstance.gameObject.tag = "Player"; // optional: so damage systems treat it like a player
        Debug.Log("[AIBotSpawner] Spawned server-owned bot at Spawn_B");
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton && NetworkManager.Singleton.IsServer)
        {
            if (botInstance && botInstance.IsSpawned)
                botInstance.Despawn();
        }
    }
}
