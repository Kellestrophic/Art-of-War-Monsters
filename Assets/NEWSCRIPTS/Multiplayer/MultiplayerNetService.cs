using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MultiplayerNetService
{
    public static event Action<string> OnJoinCodeCreated;
    public static event Action OnHostStarted;
    public static event Action OnClientStarted;
    public static event Action<string> OnError;

#if UNITY_WEBGL
    private const string CONN = "wss";  // WebGL needs secure websockets
#else
    private const string CONN = "dtls"; // native uses encrypted UDP
#endif

    private static bool _servicesReady;

    private static UnityTransport Utp =>
        NetworkManager.Singleton ? NetworkManager.Singleton.GetComponent<UnityTransport>() : null;

    public static async Task EnsureServicesAsync()
    {
        if (_servicesReady) return;
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            _servicesReady = true;
        }
        catch (Exception e)
        {
            Fail($"Services init failed: {e.Message}");
            throw;
        }
    }

    public static async Task<string> CreateJoinCodeAsync(int maxConnections = 1)
    {
        try
        {
            await EnsureServicesAsync();

            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string code = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            Utp.SetRelayServerData(new RelayServerData(alloc, CONN));

            OnJoinCodeCreated?.Invoke(code);
            return code;
        }
        catch (Exception e)
        {
            Fail($"CreateJoinCode failed: {e.Message}");
            return null;
        }
    }

    public static Task<bool> StartHostWithRelayAsync()
{
    try
    {
        // Relay must already be configured via CreateJoinCodeAsync
        bool ok = NetworkManager.Singleton.StartHost();
        if (!ok)
        {
            Fail("StartHost failed.");
            return Task.FromResult(false);
        }

        HookCallbacks();
        OnHostStarted?.Invoke();
        return Task.FromResult(true);
    }
    catch (Exception e)
    {
        Fail($"StartHost exception: {e.Message}");
        return Task.FromResult(false);
    }
}


    public static async Task<bool> StartClientWithCodeAsync(string joinCode)
    {
        if (string.IsNullOrWhiteSpace(joinCode)) { Fail("Join code empty."); return false; }

        try
        {
            await EnsureServicesAsync();

            JoinAllocation join = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Utp.SetRelayServerData(new RelayServerData(join, CONN));

            bool ok = NetworkManager.Singleton.StartClient();
            if (!ok) { Fail("StartClient failed."); return false; }

            HookCallbacks();
            OnClientStarted?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Fail($"Join by code failed: {e.Message}");
            return false;
        }
    }

    public static void LoadArena(string arenaSceneName)
    {
        if (!NetworkManager.Singleton) { Fail("No NetworkManager."); return; }
        if (!NetworkManager.Singleton.IsServer) return; // server loads; clients auto-follow

        NetworkManager.Singleton.SceneManager.LoadScene(arenaSceneName, LoadSceneMode.Single);
    }

    public static void ReturnTo(string sceneName)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public static void Shutdown()
    {
        if (!NetworkManager.Singleton) return;
        try
        {
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
                NetworkManager.Singleton.Shutdown();
            UnhookCallbacks();
        }
        catch (Exception e)
        {
            Fail($"Shutdown error: {e.Message}");
        }
    }

    // --- internals ---
    private static bool _hooked;
    private static void HookCallbacks()
    {
        if (_hooked || !NetworkManager.Singleton) return;
        _hooked = true;

        NetworkManager.Singleton.OnTransportFailure += () => Fail("Transport failure.");
        NetworkManager.Singleton.OnClientDisconnectCallback += id =>
        {
            Debug.Log($"Client disconnected: {id}");
        };
    }
    private static void UnhookCallbacks()
    {
        if (!_hooked || !NetworkManager.Singleton) return;
        _hooked = false;

        NetworkManager.Singleton.OnTransportFailure -= () => Fail("Transport failure.");
        // can't easily -= the lambda; fine to leave on shutdown
    }
    private static void Fail(string msg)
    {
        Debug.LogError($"[MultiplayerNetService] {msg}");
        OnError?.Invoke(msg);
    }
}
