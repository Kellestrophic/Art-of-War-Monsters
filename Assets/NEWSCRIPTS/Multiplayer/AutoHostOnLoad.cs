using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class AutoHostOnLoad : MonoBehaviour
{
    [Header("Optional")]
    public string ip = "127.0.0.1";
    public ushort port = 7777;

    void Awake()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[AutoHostOnLoad] No NetworkManager in scene.");
            return;
        }

        var nm = NetworkManager.Singleton;
        var utp = nm.GetComponent<UnityTransport>();
        if (utp != null)
        {
            utp.ConnectionData.Address = ip;
            utp.ConnectionData.Port = port;
        }

        if (!nm.IsListening)
        {
            Debug.Log("[AutoHostOnLoad] Starting Host for AI match…");
            nm.StartHost();
        }
    }
}
