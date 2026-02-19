using UnityEngine;

public class WolfmanAbilityEffect : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.2f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
