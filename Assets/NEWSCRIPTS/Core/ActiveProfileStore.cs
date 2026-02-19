using System;
using System.Collections.Generic;
using UnityEngine;

public class ActiveProfileStore : MonoBehaviour
{
    public static ActiveProfileStore Instance { get; private set; }

    private bool _profileLocked = false;

    public NewProfileData CurrentProfile { get; private set; }
    public event Action<NewProfileData> OnProfileChanged;

    private EnemyBossLibrary enemyBossLib;
    private FrameLibrary frameLibrary;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load libraries once
        enemyBossLib = Resources.Load<EnemyBossLibrary>("EnemyBossLibrary");
        frameLibrary = Resources.Load<FrameLibrary>("FrameLibrary");

        if (enemyBossLib == null)
            Debug.LogError("[ActiveProfileStore] Missing EnemyBossLibrary in Resources!");

        if (frameLibrary == null)
            Debug.LogError("[ActiveProfileStore] Missing FrameLibrary in Resources!");
    }

    // ----------------------------------------------------------------------
    // APPLY PROFILE (ONLY ONCE PER SESSION)
    // ----------------------------------------------------------------------
    public void SetProfile(NewProfileData p)
    {
        if (_profileLocked)
        {
            Debug.LogWarning("[ActiveProfileStore] 🔒 Profile already locked — ignoring SetProfile.");
            return;
        }

        if (p == null)
        {
            Debug.LogError("[ActiveProfileStore] ❌ SetProfile called with NULL");
            return;
        }

        // Reject regressive profile overwrites
        if (CurrentProfile != null)
        {
            long incoming = StatsChecksum.Compute(p);
            long current  = StatsChecksum.Compute(CurrentProfile);

            if (incoming < current)
            {
                Debug.LogError("[ActiveProfileStore] ❌ Rejected regressive profile apply");
                return;
            }
        }

        // ----------------------------------------------------------
        // ENSURE COLLECTIONS EXIST (DO NOT REJECT)
        // ----------------------------------------------------------
        p.unlockedCosmetics ??= new List<string>();
        p.enemyKills ??= new Dictionary<string, int>();
        p.bossKills  ??= new Dictionary<string, int>();

        if (p.level <= 0)
            p.level = 1;

        // ----------------------------------------------------------
        // AUTO-FRAME UPGRADE (SAFE)
        // ----------------------------------------------------------
        if (frameLibrary != null)
        {
            string correctFrame = frameLibrary.GetBestKeyForLevel(p.level);
            if (!string.IsNullOrEmpty(correctFrame) && p.activeFrame != correctFrame)
            {
                p.activeFrame = correctFrame;
                ProfileUploader.UpdateActiveFrame(p.wallet, p.activeFrame);
            }
        }

        // ----------------------------------------------------------
        // DEFAULT COSMETICS (NON-DESTRUCTIVE)
        // ----------------------------------------------------------
        if (string.IsNullOrWhiteSpace(p.activeIcon))
            p.activeIcon = "default_icon";

        if (string.IsNullOrWhiteSpace(p.activeTitle))
            p.activeTitle = "scaredbaby_title";

        // ----------------------------------------------------------
        // APPLY + LOCK
        // ----------------------------------------------------------
       // ----------------------------------------------------------
// APPLY + LOCK
// ----------------------------------------------------------
CurrentProfile = p;
_profileLocked = true;

Debug.Log("[ActiveProfileStore] 🔒 PROFILE LOCKED & APPLIED — Frame: " + p.activeFrame);

// 🔥 FORCE STATS BOOTSTRAP (NO EVENTS, NO TIMING RACE)
if (StatsTracker.Instance != null)
// 🔥 BOOTSTRAP RUNTIME STATS (THIS WAS MISSING)
if (RuntimeStatsStore.Instance != null)
{
    RuntimeStatsStore.Instance.BootstrapFromProfile(p);
    Debug.Log("[ActiveProfileStore] ✅ RuntimeStatsStore bootstrapped");
}
else
{
    Debug.LogWarning("[ActiveProfileStore] ⚠ RuntimeStatsStore missing!");
}


else
{
    Debug.LogWarning("[ActiveProfileStore] ⚠ StatsTracker not ready during SetProfile");
}

OnProfileChanged?.Invoke(CurrentProfile);

    }

    // ----------------------------------------------------------------------
    // MANUAL RE-BROADCAST (UI refresh only — NO mutation)
    // ----------------------------------------------------------------------
    public void ForceBroadcast()
    {
        if (CurrentProfile != null)
            OnProfileChanged?.Invoke(CurrentProfile);
    }
}
