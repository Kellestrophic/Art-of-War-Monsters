using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(Damagable))]
public class EnemyKillReporter : MonoBehaviour
{
    [Header("Enemy Identification")]
    [Tooltip("Fallback key if no metadata is present")]
    public string enemyId = "";

    [Tooltip("Skip if this enemy is tagged as AI helper, dummy, or marker")]
    public bool skipIfAITagOrMarker = true;

    private Damagable dmg;
    private EnemyMetadata meta;
    private int dmgInstanceId;

    // Prevents double-counting the SAME enemy instance
    private static readonly HashSet<int> countedThisLife = new HashSet<int>();

    // ============================================================
    // SETUP
    // ============================================================
    private void Awake()
    {
        dmg = GetComponent<Damagable>();
        TryGetComponent(out meta);

        if (dmg != null)
        {
            dmgInstanceId = dmg.GetInstanceID();
            countedThisLife.Remove(dmgInstanceId); // reset on spawn
        }
    }

    private void OnEnable()
    {
        if (dmg == null)
        {
            Debug.LogWarning($"[EnemyKillReporter] ❌ No Damagable on '{name}'");
            return;
        }

        countedThisLife.Remove(dmgInstanceId);
        dmg.onDeath.AddListener(OnDeath);

        Debug.Log($"[EnemyKillReporter] Subscribed to death on '{ResolvePreviewKey()}'");
    }

    private void OnDisable()
    {
        if (dmg != null)
            dmg.onDeath.RemoveListener(OnDeath);
    }

    // ============================================================
    // DEATH HANDLER
    // ============================================================
private void OnDeath()
{
    if (dmg == null)
        return;

    // Prevent double-counting the same instance
    if (!countedThisLife.Add(dmgInstanceId))
    {
        Debug.LogWarning($"[EnemyKillReporter] ⚠ Duplicate death ignored ({ResolvePreviewKey()})");
        return;
    }

    // Resolve stats key (priority order)
    string key =
        !string.IsNullOrEmpty(dmg.metaKey) ? dmg.metaKey :
        (meta != null && !string.IsNullOrEmpty(meta.statsKey)) ? meta.statsKey :
        enemyId;

    if (string.IsNullOrEmpty(key))
    {
        Debug.LogWarning($"[EnemyKillReporter] ❌ Empty stats key on '{name}'");
        return;
    }

    // Normalize via boss/enemy library if present
    var lib = FindFirstObjectByType<EnemyBossLibrary>();
    if (lib != null)
        key = lib.NormalizeToKey(key);

    bool isBoss = (meta != null && meta.isBoss);

    // ============================================================
    // 1) RuntimeStatsStore (your new runtime system)
    // ============================================================
    var store = RuntimeStatsStore.Instance;
    if (store != null && store.IsBootstrapped)
    {
        if (isBoss) store.RecordBossKill(key);
        else        store.RecordEnemyKill(key);
    }
    else
    {
        Debug.LogWarning("[EnemyKillReporter] RuntimeStatsStore not ready — kill dropped");
    }

    // ============================================================
    // 2) StatsTracker (needed for RunAdapter + stats UI + old paths)
    // ============================================================
    var stats = StatsTracker.Instance;
    if (stats != null)
    {
        if (isBoss)
        {
            stats.RecordBossKill(key);
            Debug.Log($"[EnemyKillReporter] 👑 Boss kill recorded → {key}");
        }
        else
        {
            stats.RecordEnemyKill(key);
            Debug.Log($"[EnemyKillReporter] ☠ Enemy kill recorded → {key}");
        }
    }
    else
    {
        Debug.LogWarning($"[EnemyKillReporter] ⚠ No StatsTracker; adapter/UI may not update ({key})");
    }
}


    // ============================================================
    // DEBUG HELPERS
    // ============================================================
    private string ResolvePreviewKey()
    {
        if (dmg != null && !string.IsNullOrEmpty(dmg.metaKey))
            return dmg.metaKey;
        if (meta != null && !string.IsNullOrEmpty(meta.statsKey))
            return meta.statsKey;
        return enemyId;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var reporters = GetComponents<EnemyKillReporter>();
        if (reporters.Length > 1)
            Debug.LogWarning($"[EnemyKillReporter] ⚠ Multiple reporters on '{name}'. Keep only one.");
    }
#endif
}
