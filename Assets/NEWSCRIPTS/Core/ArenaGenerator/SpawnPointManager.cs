using UnityEngine;
using System.Collections.Generic;

public class SpawnPointManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public int spawnPointCount = 6;
    public Vector2 arenaMin;
    public Vector2 arenaMax;
    public float minDistanceFromCenter = 3f;

    public List<Vector2> SpawnPoints { get; private set; } = new();

    public void GenerateSpawnPoints()
    {
        SpawnPoints.Clear();

        int attempts = 0;
        while (SpawnPoints.Count < spawnPointCount && attempts < 100)
        {
            attempts++;

            Vector2 pos = new Vector2(
                Random.Range(arenaMin.x, arenaMax.x),
                Random.Range(arenaMin.y, arenaMax.y)
            );

            if (pos.magnitude < minDistanceFromCenter)
                continue;

            SpawnPoints.Add(pos);
        }
    }

    private void Start()
    {
        GenerateSpawnPoints();
    }
}
