using UnityEngine;

[RequireComponent(typeof(Damagable))]
public class EnemyDropOnDeath : MonoBehaviour
{
    [Header("Health Drop")]
    [SerializeField] private GameObject healthPackPrefab;
    [SerializeField, Range(0f, 1f)] private float dropChance = 0.35f;

    private Damagable damagable;

    private void Awake()
    {
        damagable = GetComponent<Damagable>();
        damagable.onDeath.AddListener(HandleDeath);
    }

    private void OnDestroy()
    {
        if (damagable != null)
            damagable.onDeath.RemoveListener(HandleDeath);
    }

    private void HandleDeath()
    {
        if (!healthPackPrefab) return;

        if (Random.value <= dropChance)
        {
            Instantiate(
                healthPackPrefab,
                transform.position,
                Quaternion.identity
            );
        }
    }
}
