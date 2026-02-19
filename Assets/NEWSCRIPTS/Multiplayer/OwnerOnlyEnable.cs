// Assets/NEWSCRIPTS/Multiplayer/OwnerOnlyEnable.cs
using Unity.Netcode;
using UnityEngine;

public class OwnerOnlyEnable : NetworkBehaviour
{
    [SerializeField] private Behaviour[] components; // PlayerInput, CameraFollow, HUD updaters, etc.
    [SerializeField] private GameObject[] objects;   // any local-only objects

    public override void OnNetworkSpawn()
    {
        bool enable = IsOwner;
        if (components != null) foreach (var c in components) if (c) c.enabled = enable;
        if (objects != null) foreach (var g in objects) if (g) g.SetActive(enable);
    }
}
