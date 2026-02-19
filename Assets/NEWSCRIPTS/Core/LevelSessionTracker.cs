using System.Collections.Generic;
using UnityEngine;

public static class LevelSessionTracker
{
    // Keep your coins, survival, etc.
    public static int mssBankedEarned = 0;
    public static float SurvivalTime = 0f;

    // These are now irrelevant but kept so your code does not break
    public static Dictionary<string, int> EnemyKills = new();
    public static Dictionary<string, int> BossKills = new();

    public static void Reset()
    {
        mssBankedEarned = 0;
        SurvivalTime = 0f;

        // ❗ DO NOT TRACK KILLS HERE ANYMORE
        EnemyKills.Clear();
        BossKills.Clear();
    }

    // ❗ Turn kill methods into empty no-op stubs
    public static void AddEnemyKill(string enemyName) { }
    public static void AddBossKill(string bossName) { }

    public static void AddmssBanked(int amount)
    {
        mssBankedEarned += amount;
    }

    public static void AddSurvivalTime(float delta)
    {
        SurvivalTime += delta;
    }
}
