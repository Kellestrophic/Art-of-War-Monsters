using UnityEngine;

[System.Serializable]
public class EnemyDefinition
{
    public GameObject prefab;
    public int minWave = 1;
    public int maxWave = 999;
    public float weight = 1f;
}
