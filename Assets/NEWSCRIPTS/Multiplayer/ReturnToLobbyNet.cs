using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Handles "Continue" after victory: gathers votes (if online), unpauses, and loads the lobby.
public class ReturnToLobbyNet : NetworkBehaviour
{
    public static ReturnToLobbyNet Instance { get; private set; }

    [Header("Scene")]
    [Tooltip("Exact scene name as shown in Project & Build Settings (e.g., Main_Menu).")]
    [SerializeField] private string lobbySceneName = "Main_Menu";

    [Tooltip("Optional path inside project (e.g., Assets/Scenes/Main_Menu.unity). Used to resolve build index robustly.")]
    [SerializeField] private string lobbyScenePath = "Assets/Scenes/Main_Menu.unity";

    [Header("Flow")]
    [Tooltip("If ON: everyone must press Continue. If OFF: any one press returns.")]
    [SerializeField] private bool requireAllPlayers = true;

    private readonly HashSet<ulong> votes = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Hook this to the Victory banner's Continue button.</summary>
    public void OnContinueButton()
    {
        // Always unpause
        Time.timeScale = 1f;

        // OFFLINE / not listening → load menu locally (robust by index or name)
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            LoadMenuLocally();
            return;
        }

        // ONLINE → send a vote to the server to return to the lobby
        RequestReturnVoteServerRpc();
    }

    // === SERVER SIDE ===

    [ServerRpc(RequireOwnership = false)]
    private void RequestReturnVoteServerRpc(ServerRpcParams rpc = default)
    {
        var cid = rpc.Receive.SenderClientId;
        votes.Add(cid);

        if (!requireAllPlayers || HasAllVotes())
            BeginReturnToLobby();
    }

    private bool HasAllVotes()
    {
        int connected = 0;
        foreach (var _ in NetworkManager.Singleton.ConnectedClientsIds) connected++;
        return votes.Count >= connected; // kicked/AFK clients aren’t counted
    }

    private void BeginReturnToLobby()
    {
        votes.Clear();
        UnpauseAllClientsClientRpc();

        // Prefer Netcode scene loading when the scene is in the build list
        if (TryResolveSceneIndex(out int buildIndex, out string resolvedName))
        {
            // Netcode SceneManager expects a scene NAME (not path). Use the resolved name.
            NetworkManager.SceneManager.LoadScene(resolvedName, LoadSceneMode.Single);
            return;
        }

        // Fallback: force all clients (and server) to load locally and shut down networking cleanly
        ForceAllClientsLocalLoadClientRpc(lobbySceneName); // <-- renamed to end with ClientRpc
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene(lobbySceneName);
    }

    [ClientRpc]
    private void UnpauseAllClientsClientRpc() { Time.timeScale = 1f; }

    // MUST end with ClientRpc for ILPP
    [ClientRpc]
    private void ForceAllClientsLocalLoadClientRpc(string sceneName)
    {
        // Clients: shut down netcode and load menu locally
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        // Try by build index first (if we can resolve it)
        if (TryResolveSceneIndex(out int idx, out _))
        {
            SceneManager.LoadScene(idx);
            return;
        }

        // Fall back to name
        SceneManager.LoadScene(sceneName);
    }

    // === Helpers ===

    private void LoadMenuLocally()
    {
        // Best-effort: try by build index (fast & safest)
        if (TryResolveSceneIndex(out int idx, out _))
        {
            SceneManager.LoadScene(idx);
            return;
        }

        // Fallback: by name
        if (!string.IsNullOrEmpty(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
        }
        else
        {
            Debug.LogError("[ReturnToLobbyNet] No lobbySceneName set; cannot load menu.");
        }
    }

    /// <summary>
    /// Resolve the lobby scene from Build Settings. Returns true if found, along with its build index and name.
    /// Works with either a correct Name or a correct Path.
    /// </summary>
    private bool TryResolveSceneIndex(out int buildIndex, out string resolvedName)
    {
        buildIndex = -1;
        resolvedName = lobbySceneName;

        // Try by explicit path first (fast/robust if set)
        if (!string.IsNullOrEmpty(lobbyScenePath))
        {
            int idx = SceneUtility.GetBuildIndexByScenePath(lobbyScenePath);
            if (idx >= 0)
            {
                buildIndex = idx;
                // Derive the scene name from the path (strip folder + .unity)
                resolvedName = System.IO.Path.GetFileNameWithoutExtension(lobbyScenePath);
                return true;
            }
        }

        // Try to find by NAME in the build list
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path)) continue;
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, lobbySceneName))
            {
                buildIndex = i;
                resolvedName = name;
                return true;
            }
        }

        // Not found in build list
        return false;
    }
}
