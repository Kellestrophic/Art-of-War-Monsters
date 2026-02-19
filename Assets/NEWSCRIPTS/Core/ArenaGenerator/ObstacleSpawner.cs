using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    public List<GameObject> obstaclePrefabs;
    public int obstacleCount = 5;
    public Vector2 arenaMin;
    public Vector2 arenaMax;

    public void SpawnObstacles()
    {
        for (int i = 0; i < obstacleCount; i++)
        {
            Vector2 pos = new Vector2(
                Random.Range(arenaMin.x, arenaMax.x),
                Random.Range(arenaMin.y, arenaMax.y)
            );

            Instantiate(
                obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)],
                pos,
                Quaternion.identity,
                transform
            );
        }
    }
}
