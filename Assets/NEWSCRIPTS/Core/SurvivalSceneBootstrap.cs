using UnityEngine;

public class SurvivalSceneBootstrap : MonoBehaviour
{
    public ArenaGenerator arena;
    public SpawnPointManager spawnPoints;

    public Transform playerSpawnPoint;

    private void Start()
    {
        // 1) Generate arena
        arena.GenerateArena();

        // 2) Generate spawn points
        spawnPoints.GenerateSpawnPoints();

        // 3) Spawn player
        SpawnPlayer();

        }

    void SpawnPlayer()
    {
        if (SurvivalRunConfig.SelectedPlayerPrefab == null)
        {
            Debug.LogError("[Survival] No selected player prefab!");
            return;
        }

        Instantiate(
            SurvivalRunConfig.SelectedPlayerPrefab,
            playerSpawnPoint.position,
            Quaternion.identity
        );
    }
}
