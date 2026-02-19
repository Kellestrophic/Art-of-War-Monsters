using UnityEngine;

public class ArenaSpikeZone : MonoBehaviour
{
    [SerializeField] private Collider2D damageCollider;
    [SerializeField] private GameObject visual; // spikes sprite/anim

    public void EnableSpikes()
    {
        if (damageCollider) damageCollider.enabled = true;
        if (visual) visual.SetActive(true);
    }

    public void DisableSpikes()
    {
        if (damageCollider) damageCollider.enabled = false;
        if (visual) visual.SetActive(false);
    }
}
