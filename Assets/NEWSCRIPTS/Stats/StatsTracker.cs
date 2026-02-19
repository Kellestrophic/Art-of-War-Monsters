using System;
using System.Collections.Generic;
using UnityEngine;

public class StatsTracker : MonoBehaviour
{
    public static StatsTracker Instance { get; private set; }

    public event Action OnStatsChanged;

    private bool _bootstrapped;
    public bool IsBootstrapped => _bootstrapped;

    private Dictionary<string, int> enemyTotals = new();
    private Dictionary<string, int> bossTotals  = new();

    private readonly List<string> pendingKills = new();

    public int AiWinsTotal   { get; private set; }
    public int MpWinsTotal   { get; private set; }
    public int MpLossesTotal { get; private set; }

    private EnemyBossLibrary lib;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        lib = FindFirstObjectByType<EnemyBossLibrary>();

        var store = ActiveProfileStore.Instance;
        if (store == null) return;

        store.OnProfileChanged += HandleProfileChanged;

        if (!_bootstrapped && store.CurrentProfile != null)
            BootstrapFromProfile(store.CurrentProfile);
    }

    private void OnDisable()
    {
        if (ActiveProfileStore.Instance != null)
            ActiveProfileStore.Instance.OnProfileChanged -= HandleProfileChanged;
    }

    private void HandleProfileChanged(NewProfileData profile)
    {
        if (_bootstrapped || profile == null) return;
        BootstrapFromProfile(profile);
    }

    public void BootstrapFromProfile(NewProfileData profile)
    {
        enemyTotals = new Dictionary<string, int>(profile.enemyKills ?? new());
        bossTotals  = new Dictionary<string, int>(profile.bossKills ?? new());

        AiWinsTotal   = profile.aiWins;
        MpWinsTotal   = profile.multiplayerWins;
        MpLossesTotal = profile.multiplayerLosses;

        _bootstrapped = true;

        foreach (var id in pendingKills)
            RecordKill(id);

        pendingKills.Clear();
        OnStatsChanged?.Invoke();
    }

public void RecordKill(string id)
{
    if (!_bootstrapped || string.IsNullOrEmpty(id))
        return;

    var store = RuntimeStatsStore.Instance;
    if (store == null || !store.IsBootstrapped)
    {
        Debug.LogWarning("[StatsTracker] RuntimeStatsStore not ready — kill ignored");
        return;
    }

    // 🔥 THIS WAS NOT HAPPENING
    store.RecordEnemyKill(id);

    // Optional UI mirror
    if (!enemyTotals.ContainsKey(id))
        enemyTotals[id] = 0;
    enemyTotals[id]++;

    Debug.Log($"[StatsTracker] Forwarded kill → RuntimeStatsStore ({id})");
    OnStatsChanged?.Invoke();
}
// ============================================================
// BOSS / ENEMY RECORDING (REQUIRED)
// ============================================================
public void RecordEnemyKill(string id)
{
    if (!_bootstrapped || string.IsNullOrEmpty(id))
        return;

    var store = RuntimeStatsStore.Instance;
    if (store == null || !store.IsBootstrapped)
    {
        Debug.LogWarning("[StatsTracker] RuntimeStatsStore not ready — enemy kill ignored");
        return;
    }

    store.RecordEnemyKill(id);

    if (!enemyTotals.ContainsKey(id))
        enemyTotals[id] = 0;
    enemyTotals[id]++;

    Debug.Log($"[StatsTracker] Enemy kill recorded → {id}");
    OnStatsChanged?.Invoke();
}

public void RecordBossKill(string id)
{
    if (!_bootstrapped || string.IsNullOrEmpty(id))
        return;

    var store = RuntimeStatsStore.Instance;
    if (store == null || !store.IsBootstrapped)
    {
        Debug.LogWarning("[StatsTracker] RuntimeStatsStore not ready — boss kill ignored");
        return;
    }

    store.RecordBossKill(id);

    if (!bossTotals.ContainsKey(id))
        bossTotals[id] = 0;
    bossTotals[id]++;

    Debug.Log($"[StatsTracker] 👑 Boss kill recorded → {id}");
    OnStatsChanged?.Invoke();
}




// ============================================================
// MATCH RESULTS (AI / MP)
// ============================================================
public void RecordMatchResult(bool aiWin, bool mpWin, bool mpLoss)
{
    if (!_bootstrapped) return;

    var store = RuntimeStatsStore.Instance;
    if (store == null || !store.IsBootstrapped)
    {
        Debug.LogWarning("[StatsTracker] RuntimeStatsStore not ready — match ignored");
        return;
    }

    store.RecordMatchResult(aiWin, mpWin, mpLoss);

    OnStatsChanged?.Invoke();
}



// ============================================================
// COMPATIBILITY GETTERS (READ-ONLY)
// ============================================================
public int GetEnemyCount(string id)
{
    var p = RuntimeStatsStore.Instance?.GetProfile();
    if (p == null || p.enemyKills == null) return 0;
    return p.enemyKills.TryGetValue(id, out var v) ? v : 0;
}

public int GetBossCount(string id)
{
    var p = RuntimeStatsStore.Instance?.GetProfile();
    if (p == null || p.bossKills == null) return 0;
    return p.bossKills.TryGetValue(id, out var v) ? v : 0;
}


    // ============================================================
    // ACCESSORS
    // ============================================================
    public Dictionary<string, int> GetEnemyTotals() => enemyTotals;
    public Dictionary<string, int> GetBossTotals()  => bossTotals;
}
