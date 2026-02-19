using UnityEngine;
using System.Collections.Generic;

public class SurvivalDirector : MonoBehaviour
{
    [Header("References")]
    public SpawnPointManager spawnPoints;

    [Header("Enemies")]
    public List<EnemyDefinition> enemies;

    [Header("Time")]
    public float survivalTime = 0f;

    [Header("Spawn Timing")]
    public float baseSpawnInterval = 3f;      // seconds
    public float minSpawnInterval = 0.6f;     // cap
    public float spawnAcceleration = 0.02f;   // how fast it ramps

    [Header("Difficulty Scaling")]
    public int healthBonusPerMinute = 10;
    public int damageBonusPerMinute = 5;

    private float spawnTimer = 0f;

    private void Update()
    {
        survivalTime += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        float currentInterval = Mathf.Max(
            minSpawnInterval,
            baseSpawnInterval - survivalTime * spawnAcceleration
        );

        if (spawnTimer >= currentInterval)
        {
            spawnTimer = 0f;
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        EnemyDefinition enemyDef = GetEnemyForTime();
        Vector2 spawnPos = spawnPoints.SpawnPoints[
            Random.Range(0, spawnPoints.SpawnPoints.Count)
        ];

        GameObject e = Instantiate(enemyDef.prefab, spawnPos, Quaternion.identity);

        // Difficulty scaling
        var dmg = e.GetComponent<Damagable>();
        if (dmg != null)
        {
            int minutes = Mathf.FloorToInt(survivalTime / 60f);
            dmg.MaxHealth += minutes * healthBonusPerMinute;
            dmg.Health = dmg.MaxHealth;
        }
    }

    EnemyDefinition GetEnemyForTime()
    {
        float minutes = survivalTime / 60f;

        List<EnemyDefinition> valid = enemies.FindAll(
            e => minutes >= e.minWave   // reuse minWave as "minutes survived"
        );

        if (valid.Count == 0)
            valid = enemies;

        return valid[Random.Range(0, valid.Count)];
    }
}
