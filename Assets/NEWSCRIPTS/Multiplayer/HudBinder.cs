// Assets/NEWSCRIPTS/Multiplayer/HudBinder.cs
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class HudBinder : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] requires; // e.g., HealthBarUI, StrikeBarUI (your components)

    private IEnumerator Start()
    {
        // Wait for NetworkManager to be listening and for Local player to spawn
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            yield return null;

        var localClient = NetworkManager.Singleton.LocalClient;
        while (localClient == null || localClient.PlayerObject == null)
            yield return null;

        var player = localClient.PlayerObject.gameObject;
        // Now safely set references on your UI scripts here.
        // Example:
        // GetComponent<HealthBarUI>().Bind(player.GetComponent<Health>());
        // GetComponent<StrikeBarUI>().Bind(player.GetComponent<StrikeMeter>());

        Debug.Log("[HudBinder] Bound to local player: " + player.name);
    }
}
