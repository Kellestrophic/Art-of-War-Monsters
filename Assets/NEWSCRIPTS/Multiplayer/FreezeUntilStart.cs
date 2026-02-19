using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro; // optional – safe if you don't assign waitingText

public class FreezeUntilStart : NetworkBehaviour
{
    [Header("UI (optional)")]
    [SerializeField] private CanvasGroup curtain;     // assign your LoadingCurtain CanvasGroup
    [SerializeField] private TMP_Text    waitingText; // "Waiting for players..." (optional)

    [Header("Rules")]
    [SerializeField] private int   minPlayers  = 2;   // start when at least this many players are ready
    [SerializeField] private float maxWaitSecs = 30f; // placeholder if you add a timeout later

    private readonly HashSet<ulong> _ready = new();
    private bool _started;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        _ready.Clear();
        _started = false;

        // Show the curtain for everyone at start
        ShowCurtainClientRpc(true);

        NetworkManager.OnClientConnectedCallback  += OnClientConnected;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback  -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // If match hasn't started, make sure the newcomer sees the curtain
        if (!_started)
        {
            TargetShowCurtainClientRpc(true, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            });
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        _ready.Remove(clientId);
    }

    // === Called by your ProfileSyncOnSpawn ===
    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(ServerRpcParams rpc = default)
    {
        if (_started) return;

        var sender = rpc.Receive.SenderClientId;
        _ready.Add(sender);
        TryStartMatch();
    }

    private void TryStartMatch()
    {
        if (_started) return;

        int connected = NetworkManager.ConnectedClientsIds.Count;
        if (connected < minPlayers) return;
        if (_ready.Count < minPlayers) return;

        _started = true;
        ShowCurtainClientRpc(false); // unfreeze everyone
    }

    // === NEW: match-gate API expected by MatchLoadingGate ===
    /// <summary>Freeze/unfreeze all clients. Safe to call from host or a client.</summary>
    public void SetFrozen(bool frozen)
    {
        if (IsServer)
            ShowCurtainClientRpc(frozen);
        else
            ToggleFrozenServerRpc(frozen); // forward to host
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleFrozenServerRpc(bool frozen)
    {
        ShowCurtainClientRpc(frozen);
    }

    // ----- UI helpers (run on clients) -----
    [ClientRpc]
    private void ShowCurtainClientRpc(bool show)
    {
        ApplyCurtain(show);
    }

    [ClientRpc]
    private void TargetShowCurtainClientRpc(bool show, ClientRpcParams rpcParams = default)
    {
        ApplyCurtain(show);
    }

    private void ApplyCurtain(bool show)
    {
        if (curtain != null)
        {
            curtain.alpha = show ? 1f : 0f;
            curtain.blocksRaycasts = show;
            curtain.interactable = show;
        }

        if (waitingText != null)
            waitingText.gameObject.SetActive(show);
    }
}
