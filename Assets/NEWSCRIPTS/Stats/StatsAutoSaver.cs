using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

public class StatsAutosaver : MonoBehaviour
{
    [SerializeField] private float saveInterval = 10f;
    private float timer;
    private bool isSaving;

  public static StatsAutosaver Instance { get; private set; }

private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);

    Debug.Log("🟢 [StatsAutosaver] Awake (singleton)");
}
private bool hasSeenMutation;

public void MarkMutated()
{
    hasSeenMutation = true;
}

    private void Update()
    {
        var store = RuntimeStatsStore.Instance;
        if (store == null || !store.IsBootstrapped || !store.IsDirty)
            return;
if (!hasSeenMutation)
    return;

        timer += Time.deltaTime;
        if (timer < saveInterval)
            return;

        timer = 0f;

        if (!isSaving)
            _ = SaveAsync();
    }

    private async Task SaveAsync()
    {
        var store = RuntimeStatsStore.Instance;
        if (store == null || !store.IsBootstrapped || !store.IsDirty)
            return;

        isSaving = true;

        // ✅ SINGLE SOURCE OF TRUTH
        var profile = store.GetProfile();
        if (profile == null || string.IsNullOrEmpty(profile.wallet))
        {
            isSaving = false;
            return;
        }

        Debug.Log(
    $"[StatsAutosaver] 💾 Saving XP={profile.totalXP} " +
    $"enemyKillsTotal={profile.enemyKills.Values.Sum()}"
);
        bool ok = await ProfileUploader.SaveStatsBatch(profile.wallet, profile);

        if (ok)
        {
            store.ClearDirty();
            Debug.Log("[StatsAutosaver] ✅ Save complete, dirty cleared");
        }
        else
        {
            Debug.LogError("[StatsAutosaver] ❌ Save failed — will retry");
        }

        isSaving = false;
    }
}
