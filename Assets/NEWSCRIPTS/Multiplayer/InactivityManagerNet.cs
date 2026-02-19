using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// Server-side AFK watchdog. Kicks idle clients and flags MCC to be disabled for this match.
public class InactivityManagerNet : NetworkBehaviour
{
    public static InactivityManagerNet Instance { get; private set; }

    [Header("AFK (server)")]
    [SerializeField] private float graceSeconds = 10f;     // ignore AFK for first N seconds
    [SerializeField] private float idleKickSeconds = 30f;  // after grace, kick if no activity for N seconds
    [SerializeField] private bool declareWinnerOnKick = true;

    private readonly Dictionary<ulong, float> _lastActiveAt = new();
    private readonly HashSet<ulong> _kicked = new();
    private float _roundStart;
    private bool _mccDisabledThisMatch;

    public bool MCCDisabledThisMatch => _mccDisabledThisMatch;

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        _roundStart = Time.unscaledTime;

        foreach (var cid in NetworkManager.Singleton.ConnectedClientsIds)
            _lastActiveAt[cid] = Time.unscaledTime;

        NetworkManager.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        if (Instance == this) Instance = null;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        _lastActiveAt[clientId] = Time.unscaledTime;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        _lastActiveAt.Remove(clientId);
        // if only one remains, award win automatically (optional)
        MaybeAutoWin();
    }

    /// Called by ActivityMonitorNet via ServerRpc when a client shows input/motion.
    public void NoteActive(ulong clientId)
    {
        if (!IsServer) return;
        _lastActiveAt[clientId] = Time.unscaledTime;
    }

    private void Update()
    {
        if (!IsServer) return;
        if (MatchStateNet.Instance == null) return;
        if (MatchStateNet.Instance.Phase.Value != MatchPhase.Playing) return;

        if (Time.unscaledTime < _roundStart + graceSeconds) return;

        foreach (var cid in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (_kicked.Contains(cid)) continue;

            float last = _lastActiveAt.TryGetValue(cid, out var t) ? t : _roundStart;
            if (Time.unscaledTime - last >= idleKickSeconds)
            {
                _kicked.Add(cid);
                _mccDisabledThisMatch = true; // disable MCC for *everyone* this match
                Debug.LogWarning($"[AFK] Disconnecting client {cid} due to inactivity.");
                NetworkManager.Singleton.DisconnectClient(cid);

                if (declareWinnerOnKick) MaybeAutoWin();
            }
        }
    }

    private void MaybeAutoWin()
    {
        if (!IsServer) return;
        if (MatchStateNet.Instance == null) return;
        if (MatchStateNet.Instance.Phase.Value != MatchPhase.Playing) return;

        // If exactly one client remains connected, declare them winner.
        var alive = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        if (alive.Count < 1) return;

        // Pick the first connected client that wasn't kicked
        ulong winner = 0;
        foreach (var cid in alive)
        {
            if (!_kicked.Contains(cid))
            {
                winner = cid;
                break;
            }
        }
        if (winner != 0)
            MatchStateNet.Instance.ServerDeclareWinner(winner);
    }
}
