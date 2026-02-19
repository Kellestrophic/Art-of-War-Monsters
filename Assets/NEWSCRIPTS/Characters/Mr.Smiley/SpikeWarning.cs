using UnityEngine;

public class SpikeWarning : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.6f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}
