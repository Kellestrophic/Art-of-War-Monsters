using System.Collections.Generic;
using UnityEngine;

public class AuthoritativeStatsService : MonoBehaviour
{
    public static AuthoritativeStatsService Instance { get; private set; }

    private bool hydrated = false;

    private Dictionary<string, int> enemyKills = new();
    private Dictionary<string, int> bossKills  = new();

    private string wallet;

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

    public void HydrateFromProfile(NewProfileData profile)
    {
        if (profile == null || hydrated)
            return;

        wallet = profile.wallet;

        enemyKills = profile.enemyKills != null
            ? new Dictionary<string, int>(profile.enemyKills)
            : new Dictionary<string, int>();

        bossKills = profile.bossKills != null
            ? new Dictionary<string, int>(profile.bossKills)
            : new Dictionary<string, int>();

        hydrated = true;

        Debug.Log("[AuthStats] ✅ Hydrated from Firebase");
    }

    public void RecordEnemyKill(string id)
    {
        if (!hydrated)
        {
            Debug.LogWarning("[AuthStats] Kill ignored — not hydrated yet");
            return;
        }

        enemyKills.TryAdd(id, 0);
        enemyKills[id]++;
    }

    public void RecordBossKill(string id)
    {
        if (!hydrated)
            return;

        bossKills.TryAdd(id, 0);
        bossKills[id]++;
    }

    public int GetEnemyCount(string id)
        => enemyKills.TryGetValue(id, out var v) ? v : 0;

    public int GetBossCount(string id)
        => bossKills.TryGetValue(id, out var v) ? v : 0;
}
