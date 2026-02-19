using System.Collections.Generic;
using UnityEngine;

public class ShadowHandController : MonoBehaviour
{
    [Header("Spawns")]
    [SerializeField] private Transform[] handSpawns;

    [Header("Projectile")]
    [SerializeField] private GameObject shadowHandPrefab;
    [SerializeField] private float handSpeed = 8f;

   public void FireHands(int count)
{
    if (handSpawns == null || handSpawns.Length == 0) return;
    if (!shadowHandPrefab) return;

    count = Mathf.Clamp(count, 1, handSpawns.Length);

    // 🔥 SHUFFLE ONCE PER CAST
    Shuffle(handSpawns);

    for (int i = 0; i < count; i++)
    {
        FireFromSpawn(handSpawns[i]);
    }
}

private void Shuffle<T>(T[] array)
{
    for (int i = array.Length - 1; i > 0; i--)
    {
        int j = Random.Range(0, i + 1);
        (array[i], array[j]) = (array[j], array[i]);
    }
}

    private void FireFromSpawn(Transform spawn)
    {
        GameObject hand = Instantiate(
            shadowHandPrefab,
            spawn.position,
            spawn.rotation
        );

        Rigidbody2D rb = hand.GetComponent<Rigidbody2D>();
        if (!rb) return;

        rb.linearVelocity = spawn.right.normalized * handSpeed;
    }
}
