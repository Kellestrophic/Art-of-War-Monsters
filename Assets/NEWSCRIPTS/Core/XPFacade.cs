using System;
using UnityEngine;

public static class XPFacade
{
    public static event Action<int, int, float> OnXPChanged;

    private static bool _initialized;
    private static int _cachedTotalXP;

    // --------------------------------------------------------------
    // LOAD FROM PROFILE (READ-ONLY)
    // --------------------------------------------------------------
    public static void InitializeFromProfileIfAvailable()
    {
        var p = ActiveProfileStore.Instance?.CurrentProfile;
        if (p == null) return;

        _cachedTotalXP = Mathf.Max(0, p.totalXP);
        _initialized = true;

        Notify();
    }

    public static void LoadFromProfile(NewProfileData p)
    {
        if (p == null) return;

        _cachedTotalXP = Mathf.Max(0, p.totalXP);
        _initialized = true;

        Notify();
    }

    // --------------------------------------------------------------
    // GETTERS (SAFE)
    // --------------------------------------------------------------
    public static int GetTotalXP()
    {
        if (!_initialized) InitializeFromProfileIfAvailable();
        return _cachedTotalXP;
    }
public static void AddXP(int amount, string reason = "")
{
    if (amount <= 0) return;

    if (!_initialized)
        InitializeFromProfileIfAvailable();

    _cachedTotalXP += amount;

    // 🔥 Forward to persistence
    if (RuntimeStatsStore.Instance != null && RuntimeStatsStore.Instance.IsBootstrapped)
    {
        RuntimeStatsStore.Instance.AddXP(amount, reason);
    }
    else
    {
        Debug.LogWarning("[XPFacade] RuntimeStatsStore not ready — XP not persisted");
    }

    Notify();

    Debug.Log($"[XPFacade] +{amount} XP from {reason}");
}

    public static int GetLevel()
    {
        if (!_initialized) InitializeFromProfileIfAvailable();
        return XPLevelCalculator.GetLevelFromTotalXP(_cachedTotalXP);
    }

public static void SyncFromProfile(NewProfileData p)
{
    if (p == null) return;
    _cachedTotalXP = Mathf.Max(0, p.totalXP);
    _initialized = true;
    Notify();
}


    // --------------------------------------------------------------
    // FORCE SET (EDITOR / DEBUG ONLY)
    // --------------------------------------------------------------
    public static void ForceSetRuntime(int absolute)
    {
        _cachedTotalXP = Mathf.Max(0, absolute);
        _initialized = true;

        Notify();
    }

    // --------------------------------------------------------------
    // COMMIT CALLED BY StatsTracker ONLY
    // --------------------------------------------------------------
    public static int ConsumeRuntimeXP()
    {
        int xp = _cachedTotalXP;
        _cachedTotalXP = 0;
        return xp;
    }

    // --------------------------------------------------------------
    // NOTIFY UI LISTENERS
    // --------------------------------------------------------------
    private static void Notify()
    {
        XPLevelCalculator.GetProgressInLevel(
            _cachedTotalXP,
            out var lvl,
            out _,
            out _,
            out var into,
            out var need,
            out var pct
        );

        OnXPChanged?.Invoke(lvl, into, pct);
    }
}
