using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NewProfileData
{
    // ============================================================
    // BASIC PROFILE
    // ============================================================
    public string wallet = "";
    public string playerName = "";

    public int level = 1;
    public int mssBanked = 0;

    public int totalXP = 0;
    public int totalMssEarned = 0;
    public long statsChecksum;

    // ============================================================
    // COSMETICS
    // ============================================================
    public string activeIcon = "default_icon";
    public string activeFrame = "bronze_frame";
    public string activeTitle = "scaredbaby_title";

    public List<string> unlockedCosmetics = new();

    // ============================================================
    // GAMEPLAY STATS
    // ============================================================
    public Dictionary<string, int> enemyKills = new();
    public Dictionary<string, int> bossKills = new();

    public int aiWins = 0;
    public int multiplayerWins = 0;
    public int multiplayerLosses = 0;

    public float longestSurvivalTime = 0f;

    // ============================================================
    // STATS LOOKUP (For Cosmetic Unlock Rules)
    // ============================================================
    public int GetStatByName(string statName)
    {
        return statName switch
        {
            "aiWins"            => aiWins,
            "multiplayerWins"   => multiplayerWins,
            "multiplayerLosses" => multiplayerLosses,
            "totalXP"           => totalXP,
            "mssBanked"         => mssBanked,
            "totalMssEarned"    => totalMssEarned,
            "level"             => level,
            _ => 0
        };
    }

    // ============================================================
    // DICTIONARY EXPANSION (Correct, permanent)
    // ============================================================
    public void ExpandAllKillDictionaries(EnemyBossLibrary lib)
    {
        if (enemyKills == null)
            enemyKills = new Dictionary<string, int>();

        if (bossKills == null)
            bossKills = new Dictionary<string, int>();

        // Add missing enemy keys WITHOUT overriding existing ones
        foreach (var key in lib.AllEnemyKeys())
        {
            if (!enemyKills.ContainsKey(key))
                enemyKills[key] = 0;
        }

        // Add missing boss keys WITHOUT overriding existing ones
        foreach (var key in lib.AllBossKeys())
        {
            if (!bossKills.ContainsKey(key))
                bossKills[key] = 0;
        }
    }

    public void EnsureEnemyKillKeys()
    {
        var lib = Resources.Load<EnemyBossLibrary>("EnemyBossLibrary");
        if (lib != null)
            ExpandAllKillDictionaries(lib);
    }

    public void EnsureBossKillKeys()
    {
        var lib = Resources.Load<EnemyBossLibrary>("EnemyBossLibrary");
        if (lib != null)
            ExpandAllKillDictionaries(lib);
    }
}
