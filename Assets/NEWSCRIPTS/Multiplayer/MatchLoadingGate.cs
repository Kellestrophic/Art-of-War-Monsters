using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MatchLoadingGate : NetworkBehaviour
{
    [Header("Optional curtain (CanvasGroup fades)")]
    [SerializeField] private CanvasGroup curtain;
    [SerializeField] private Text       waitingText;
    [SerializeField] private float      maxWaitSeconds = 15f;

    public override void OnNetworkSpawn()
    {
        if (IsServer) StartCoroutine(ServerRoutine());
        Show(true); // local visual
    }

    // Called by server when it decides to start
    [ClientRpc]
    private void BeginMatchClientRpc()
    {
        Show(false);
        // Unfreeze everything local
        foreach (var f in FindObjectsByType<FreezeUntilStart>(FindObjectsSortMode.None))
            f.SetFrozen(false);
    }

    private IEnumerator ServerRoutine()
    {
        float t = 0f;
        while (t < maxWaitSeconds)
        {
            var ids = FindObjectsByType<PlayerIdentityNet>(FindObjectsSortMode.None)
                     .Where(i => i.IsSpawned).ToList();

            int target = NetworkManager.Singleton.ConnectedClientsList.Count; // 2 in direct match
            int ready  = ids.Count(i => i.IdentityReady.Value);

            if (waitingText) waitingText.text = $"Waiting for players… {ready}/{target}";

            if (target > 0 && ready >= target) break; // all ready
            t += Time.deltaTime;
            yield return null;
        }

        BeginMatchClientRpc();
    }

    // Small visual helper the scene can call or we call locally
    public void Show(bool show)
    {
        if (!curtain) return;
        curtain.alpha = show ? 1f : 0f;
        curtain.blocksRaycasts = show;
        curtain.interactable   = show;
    }
}
