// Assets/NEWSCRIPTS/Multiplayer/NetBootstrap.cs
using Unity.Netcode;
using UnityEngine;
public class NetBootstrap : MonoBehaviour {
  void Awake() {
    var nm = GetComponent<NetworkManager>();
    if (NetworkManager.Singleton != null && NetworkManager.Singleton != nm) { Destroy(gameObject); return; }
    DontDestroyOnLoad(gameObject);
  }
}
