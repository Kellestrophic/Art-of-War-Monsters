using UnityEngine;

public class StatsTrackerSpawner : MonoBehaviour
{
    [Header("Assign the StatsTracker prefab")]
    public StatsTracker statsTrackerPrefab;

    void Awake()
    {
        // Already exists?
        if (StatsTracker.Instance != null)
        {
            Debug.Log("[StatsTrackerSpawner] StatsTracker already exists.");
            return;
        }

        // Must have prefab
        if (statsTrackerPrefab == null)
        {
            Debug.LogError("[StatsTrackerSpawner] Missing prefab reference!");
            return;
        }

        // Spawn
        var tracker = Instantiate(statsTrackerPrefab);
        DontDestroyOnLoad(tracker.gameObject);

        Debug.Log("[StatsTrackerSpawner] Spawned global StatsTracker singleton.");
    }
}
