// Assets/NEWSCRIPTS/Multiplayer/OwnerOnlyCanvas.cs
using Unity.Netcode;
using UnityEngine;

public class OwnerOnlyCanvas : NetworkBehaviour
{
    [SerializeField] private Canvas[] canvases;   // your player HUD canvases
    [SerializeField] private GameObject[] alsoToggle; // e.g., local-only UI roots, camera rigs

    public override void OnNetworkSpawn()
    {
        bool enable = IsOwner;
        if (canvases != null)
            foreach (var c in canvases) if (c) c.enabled = enable;
        if (alsoToggle != null)
            foreach (var g in alsoToggle) if (g) g.SetActive(enable);
    }
}
