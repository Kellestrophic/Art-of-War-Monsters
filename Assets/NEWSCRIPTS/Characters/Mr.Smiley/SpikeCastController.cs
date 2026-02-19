using System.Collections;
using UnityEngine;

public class SpikeCastController : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private BoxCollider2D spikeArea;
    [SerializeField] private GameObject warningPrefab;
    [SerializeField] private GameObject spikePrefab;

    [Header("Timing")]
    [SerializeField] private float warningTime = 0.6f;
    [SerializeField] private float spikeSpacing = 0.15f;

    [Header("Count (Default)")]
    [SerializeField] private int spikesPerCast = 8;

    // -----------------------------
    // PUBLIC API
    // -----------------------------

    // Old call (keeps working)
    public void StartSpikeCast()
    {
        StartSpikeCast(spikesPerCast);
    }

    // ✅ New scaled call
    public void StartSpikeCast(int overrideCount)
{
    StartCoroutine(SpikeRoutine(overrideCount));
}

private IEnumerator SpikeRoutine(int count)
{
    Bounds b = spikeArea.bounds;
    float floorY = b.min.y;

    Vector2[] positions = new Vector2[count];

    for (int i = 0; i < count; i++)
    {
        float x = Random.Range(b.min.x, b.max.x);
        positions[i] = new Vector2(x, floorY);
        Instantiate(warningPrefab, positions[i], Quaternion.identity);
    }

    yield return new WaitForSeconds(warningTime);

    for (int i = 0; i < positions.Length; i++)
    {
        Instantiate(spikePrefab, positions[i], Quaternion.identity);
        yield return new WaitForSeconds(spikeSpacing);
    }
}
public void SetSpikesPerCast(int count)
{
    spikesPerCast = Mathf.Max(1, count);
}


}
