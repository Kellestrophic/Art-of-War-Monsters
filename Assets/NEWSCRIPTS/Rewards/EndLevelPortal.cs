using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EndLevelPortal : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LevelCompleteOrchestrator_Stats orchestrator;

    [SerializeField] private Behaviour[] componentsToDisableOnPlayer;
    private bool _triggered;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }
private void Awake()
{
    if (orchestrator == null)
        orchestrator = FindFirstObjectByType<LevelCompleteOrchestrator_Stats>();
}

    private async void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;

        var root = other.transform.root;
        if (!root.CompareTag(playerTag)) return;

        _triggered = true;

        var rb = root.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        foreach (var b in componentsToDisableOnPlayer)
            if (b) b.enabled = false;

        // ============================
        // 🔥 SAVE STATS ON LEVEL WIN
        // ============================
        var profile = ActiveProfileStore.Instance?.CurrentProfile;
        if (profile != null)
        {
            await ProfileUploader.SaveStatsBatch(profile.wallet, profile);
            Debug.Log("[EndLevelPortal] ✅ Stats saved on level completion");
        }

        if (orchestrator) orchestrator.CompleteLevel();
        else Debug.LogError("[EndLevelPortal] Orchestrator (_Stats) not assigned.");
    }
}
