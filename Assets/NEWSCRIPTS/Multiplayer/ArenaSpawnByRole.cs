using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ArenaSpawnByRole : MonoBehaviour
{
    [SerializeField] private Transform spawnHost;   // left
    [SerializeField] private Transform spawnJoiner; // right

    void OnEnable()
    {
        if (!NetworkManager.Singleton) return;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }
    void OnDisable()
    {
        if (!NetworkManager.Singleton) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        // Host/server decides all spawns
        if (!NetworkManager.Singleton.IsServer) return;
        StartCoroutine(PlaceWhenReady(clientId));
    }

    private IEnumerator PlaceWhenReady(ulong clientId)
    {
        var nm = NetworkManager.Singleton;

        // Declare cc OUTSIDE the while so we can use it after.
        NetworkClient cc;
        while (!nm.ConnectedClients.TryGetValue(clientId, out cc) || cc.PlayerObject == null)
            yield return null;

        var po = cc.PlayerObject;
        if (!po || !po.IsSpawned) yield break;

        // Host == server. Static constant must be qualified with the type name:
        bool isHostClient = clientId == NetworkManager.ServerClientId;

        var t = po.transform;
        if (isHostClient && spawnHost)
            t.SetPositionAndRotation(spawnHost.position, spawnHost.rotation);
        else if (!isHostClient && spawnJoiner)
            t.SetPositionAndRotation(spawnJoiner.position, spawnJoiner.rotation);
    }
}
