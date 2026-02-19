using UnityEngine;

public class ExplodeHitbox : MonoBehaviour
{
    private ExplodingCultist owner;

    void Awake()
    {
        owner = GetComponentInParent<ExplodingCultist>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        owner.TriggerExplosion();
    }
}
