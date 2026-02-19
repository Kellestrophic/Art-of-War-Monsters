// Assets/NEWSCRIPTS/Rewards/StatsTrackerRunAdapter.cs
using System.Collections.Generic;
using UnityEngine;

public class StatsTrackerRunAdapter : MonoBehaviour
{
    private bool _captured;

    private Dictionary<string, int> startEnemyKills = new();
    private Dictionary<string, int> startBossKills  = new();

    public void CaptureStart()
    {
        if (_captured)
        {
            Debug.Log("[StatsRunAdapter] CaptureStart skipped (already captured)");
            return;
        }

        var store = RuntimeStatsStore.Instance;
        if (store == null || !store.IsBootstrapped)
        {
            Debug.LogWarning("[StatsRunAdapter] RuntimeStatsStore not ready at CaptureStart");
            return;
        }

        _captured = true;

        // ✅ Snapshot START state from runtime stats
        var prof = store.GetProfile();
        startEnemyKills = new Dictionary<string, int>(prof.enemyKills);
        startBossKills  = new Dictionary<string, int>(prof.bossKills);

        Debug.Log("[StatsRunAdapter] CaptureStart snapshot stored");
    }

    public Dictionary<string, int> GetEnemyRunDeltas()
    {
        if (!_captured)
        {
            Debug.LogWarning("[StatsRunAdapter] GetEnemyRunDeltas called before CaptureStart");
            return new Dictionary<string, int>();
        }

        var prof = RuntimeStatsStore.Instance?.GetProfile();
        if (prof == null) return new Dictionary<string, int>();

        return Diff(startEnemyKills, prof.enemyKills);
    }

    public Dictionary<string, int> GetBossRunDeltas()
    {
        if (!_captured)
        {
            Debug.LogWarning("[StatsRunAdapter] GetBossRunDeltas called before CaptureStart");
            return new Dictionary<string, int>();
        }

        var prof = RuntimeStatsStore.Instance?.GetProfile();
        if (prof == null) return new Dictionary<string, int>();

        return Diff(startBossKills, prof.bossKills);
    }

    private Dictionary<string, int> Diff(
        Dictionary<string, int> start,
        Dictionary<string, int> end)
    {
        var result = new Dictionary<string, int>();
        if (start == null || end == null) return result;

        foreach (var kv in end)
        {
            int before = start.TryGetValue(kv.Key, out var v) ? v : 0;
            int delta = kv.Value - before;

            if (delta > 0)
                result[kv.Key] = delta;
        }

        return result;
    }
}
