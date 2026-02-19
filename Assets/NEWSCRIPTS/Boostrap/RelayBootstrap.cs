using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

public static class RelayBootstrapper
{
    /// <summary>
    /// Ensure UGS is initialized and we have an anonymous player id.
    /// Call this once before hosting/joining.
    /// </summary>
    public static async Task EnsureServicesAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    /// <summary>
    /// Host with Relay using WebSockets (wss). Returns the join code or null on failure.
    /// </summary>
    public static async Task<string> HostWithRelayWSSAsync(int maxConnections = 2, string region = null)
    {
        await EnsureServicesAsync();

        // 1) Create allocation (optionally pick a region; otherwise Relay auto-selects)
        Allocation alloc = string.IsNullOrWhiteSpace(region)
            ? await RelayService.Instance.CreateAllocationAsync(maxConnections)
            : await RelayService.Instance.CreateAllocationAsync(maxConnections, region);

        // 2) Get join code
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
        Debug.Log($"[Relay] JoinCode={joinCode} ConnType=wss");

        // 3) Build RelayServerData for WSS and apply to UnityTransport
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        // Make sure Use Web Sockets is checked in the Inspector too
        transport.SetRelayServerData(new RelayServerData(alloc, "wss"));

        // 4) Start host
        bool ok = NetworkManager.Singleton.StartHost();
        if (!ok)
        {
            Debug.LogError("[Relay][Host] StartHost() returned false");
            return null;
        }

        return joinCode;
    }

    /// <summary>
    /// Join with Relay using WebSockets (wss) and a join code.
    /// </summary>
    public static async Task<bool> JoinWithRelayWSSAsync(string joinCode)
    {
        await EnsureServicesAsync();

        // 1) Join allocation
        JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
        Debug.Log($"[Relay][Client] Joining with '{joinCode}' ConnType=wss");

        // 2) Build RelayServerData for WSS and apply to UnityTransport
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(new RelayServerData(joinAlloc, "wss"));

        // 3) Start client
        bool ok = NetworkManager.Singleton.StartClient();
        if (!ok)
        {
            Debug.LogError("[Relay][Client] StartClient() returned false");
            return false;
        }

        return true;
    }
}
