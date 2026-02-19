// Assets/NEWSCRIPTS/Multiplayer/DirectMatchRelayUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using Unity.Netcode.Transports.UTP;        // Unity Transport
using Unity.Networking.Transport.Relay;    // RelayServerData (Transport 2.x)

public class DirectMatchRelayUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text hostCodeText;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button createCodeButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button copyButton;

    [Header("Options")]
    [SerializeField] private int  maxConnections   = 1;   // 1v1
    [SerializeField] private bool autoStartWhenTwo = true;
    [SerializeField] private string relayRegion    = "";  // leave blank to auto-select

    // Force WebSockets everywhere (Editor + WebGL)
    private static string RelayConnType => "wss";

    private string rawHostJoinCode;

    private void OnEnable()
    {
        if (hostCodeText) hostCodeText.text = "";
        if (statusText)   statusText.text   = "";
    }

    private void Awake()
    {
        // Ensure UI events are wired once
        if (createCodeButton) { createCodeButton.onClick.RemoveAllListeners(); createCodeButton.onClick.AddListener(() => _ = HostCreateCodeAsync()); }
        if (joinButton)       { joinButton.onClick.RemoveAllListeners();       joinButton.onClick.AddListener(() => _ = JoinWithCodeAsync(joinCodeInput ? joinCodeInput.text : "")); }
        if (copyButton)       { copyButton.onClick.RemoveAllListeners();       copyButton.onClick.AddListener(CopyCodeToClipboard); }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // HOST
    private async System.Threading.Tasks.Task HostCreateCodeAsync()
    {
        try
        {
            if (!await EnsureStopped()) return;

            string why = PreflightNetworkManager(out var ut);
            if (why != null) { Fail("Host preflight: " + why); return; }

            // Always use WebSockets with Relay (works in Editor + WebGL)
            ut.UseWebSockets = true;

            await EnsureServicesAsync();

            Allocation alloc = string.IsNullOrWhiteSpace(relayRegion)
                ? await RelayService.Instance.CreateAllocationAsync(maxConnections)
                : await RelayService.Instance.CreateAllocationAsync(maxConnections, relayRegion);

            rawHostJoinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
            Debug.Log($"[Relay][Host] JoinCode={rawHostJoinCode} ConnType={RelayConnType}");

            if (hostCodeText) hostCodeText.text = rawHostJoinCode;
            if (statusText)   statusText.text   = "Hosting… waiting for a player";

            // Build RelayServerData for WebSockets and apply to transport
            ut.SetRelayServerData(new RelayServerData(alloc, RelayConnType));

            if (!NetworkManager.Singleton.StartHost())
            {
                Fail("Failed to start host (see console)");
                Debug.LogError("[Relay][Host] StartHost() returned false");
                return;
            }

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedStartWhenTwo;
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            Fail(ExplainRelayError(e));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // CLIENT
    private async System.Threading.Tasks.Task JoinWithCodeAsync(string input)
    {
        try
        {
            if (!await EnsureStopped()) return;

            string why = PreflightNetworkManager(out var ut, checkPlayerPrefab:false);
            if (why != null) { Fail("Client preflight: " + why); return; }

            // Always use WebSockets
            ut.UseWebSockets = true;

            await EnsureServicesAsync();

            string code = Sanitize(input);
            if (string.IsNullOrEmpty(code)) { Fail("Enter a code."); return; }

            Debug.Log($"[Relay][Client] Joining with '{code}' ConnType={RelayConnType}");
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(code);

            // Apply Relay WSS config to transport
            ut.SetRelayServerData(new RelayServerData(joinAlloc, RelayConnType));

            Info("Joining…");
            if (!NetworkManager.Singleton.StartClient())
            {
                Fail("Failed to start client (see console)");
                Debug.LogError("[Relay][Client] StartClient() returned false");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            Fail(ExplainRelayError(e));
        }
    }

    // Auto-begin match when 2 players connected (host only)
    private void OnClientConnectedStartWhenTwo(ulong _)
    {
        if (!NetworkManager.Singleton.IsHost) return;
        int count = NetworkManager.Singleton.ConnectedClientsIds.Count;
        Debug.Log("[Relay][Host] Connected clients: " + count);
        if (count >= 2)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedStartWhenTwo;
            Info("Player joined!");
            if (autoStartWhenTwo) BeginMatch();
        }
    }

   private void BeginMatch()
{
    if (!NetworkManager.Singleton.IsHost) return; // host triggers the load

    const string sceneName = "Arena_GraveyardDM"; // <-- put your actual gameplay scene name here

    // Make sure this scene is added to Build Settings → Scenes In Build
    // and NetworkManager persists (DontDestroyOnLoad)
    Debug.Log("[Relay] BeginMatch() -> loading " + sceneName);

    // Use Netcode’s scene manager so all clients load in sync
    NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
}


    // ───────────────────────────────── Helpers ───────────────────────────────────
    private async System.Threading.Tasks.Task EnsureServicesAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    // Returns error string if something is wrong; otherwise sets ut and returns null
    private string PreflightNetworkManager(out UnityTransport ut, bool checkPlayerPrefab = true)
    {
        ut = null;

        var nm = NetworkManager.Singleton;
        if (!nm) return "NetworkManager.Singleton is null (ensure one active NetworkManager)";
        if (nm.IsListening) return "a session is already running";

        ut = nm.NetworkConfig.NetworkTransport as UnityTransport;
        if (!ut) return "NetworkManager.Transport is not UnityTransport";

        if (checkPlayerPrefab)
        {
            var player = nm.NetworkConfig.PlayerPrefab;
            if (!player) return "Default Player Prefab is not assigned on NetworkManager";
            if (!player.TryGetComponent<NetworkObject>(out _))
                return "Default Player Prefab is missing NetworkObject on the ROOT";
        }

        return null;
    }

    private static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char c in s) if (c != '-' && !char.IsWhiteSpace(c)) sb.Append(c);
        return sb.ToString().ToUpperInvariant(); // Relay codes are 6 chars
    }

    private static string ExplainRelayError(System.Exception e)
    {
        var m = e.Message ?? "";
        if (m.Contains("join code not found") || m.Contains("Allocation does not exist") || m.Contains("404"))
            return "No host for that code.";
        if (m.Contains("Unauthorized") || m.Contains("401"))
            return "Auth failed (Relay).";
        if (m.Contains("Timed out"))
            return "Connection timed out.";
        return "Relay error.";
    }

    private void CopyCodeToClipboard()
    {
        GUIUtility.systemCopyBuffer = rawHostJoinCode ?? "";
        Info("Code copied");
    }

    private void Info(string msg) { if (statusText) statusText.text = msg; }
    private void Fail(string msg) { if (statusText) statusText.text = msg; Debug.LogWarning(msg); }

    private async System.Threading.Tasks.Task<bool> EnsureStopped()
    {
        var nm = NetworkManager.Singleton;
        if (!nm) return false;
        if (!nm.IsListening) return true;
        Info("Stopping previous session…");
        nm.Shutdown();
        await System.Threading.Tasks.Task.Delay(150);
        return !nm.IsListening;
    }
}
