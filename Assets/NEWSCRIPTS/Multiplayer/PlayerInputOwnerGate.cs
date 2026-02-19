// Assets/NEWSCRIPTS/Multiplayer/PlayerInputOwnerGate.cs
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputOwnerGate : NetworkBehaviour
{
    private PlayerInput _pi;

    private void Awake() => _pi = GetComponent<PlayerInput>();

    public override void OnNetworkSpawn()
    {
        if (_pi) _pi.enabled = IsOwner; // only local owner reads input
    }
}
