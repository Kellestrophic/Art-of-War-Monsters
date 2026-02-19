// Assets/NEWSCRIPTS/Multiplayer/ReadySync.cs
using UnityEngine;
using Unity.Netcode;

public class ReadySync : NetworkBehaviour
{
    // Local-owner singleton: only the local player's ReadySync sets Instance.
    public static ReadySync Instance { get; private set; }

    // Local player's ready flag (owned by that player)
    public readonly NetworkVariable<bool> IsReady =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

    // UI helper: am I (the local owner) ready?
    public bool LocalReady => IsOwner && IsReady.Value;

    public override void OnNetworkSpawn()
    {
        // Capture singleton only for the local owner's ReadySync
        if (IsOwner)
        {
            Instance = this;
            IsReady.Value = false; // start unready when spawned
        }

        // Optional: debug when this local player's ready changes
        IsReady.OnValueChanged += (oldV, newV) =>
        {
            if (IsOwner)
                Debug.Log($"[ReadySync] Local ready -> {newV}");
        };
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && Instance == this)
            Instance = null;
    }

    /// <summary>Flip local ready (owner only).</summary>
    public void ToggleReady()
    {
        if (!IsOwner)
        {
            Debug.LogWarning("[ReadySync] ToggleReady called by non-owner; ignored.");
            return;
        }
        IsReady.Value = !IsReady.Value;
    }

    /// <summary>Set local ready explicitly (owner only).</summary>
    public void SetReady(bool value)
    {
        if (!IsOwner)
        {
            Debug.LogWarning("[ReadySync] SetReady called by non-owner; ignored.");
            return;
        }
        IsReady.Value = value;
    }

    public override void OnDestroy()
    {
        if (IsOwner && Instance == this)
            Instance = null;

        base.OnDestroy();
    }
}
