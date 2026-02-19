using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class HitboxAttackNet : NetworkBehaviour
{
    [SerializeField] private int damage = 5;
    [SerializeField] private float hitCooldown = 0.25f;
    private float nextHit;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only the owner of the attacker reports hits; server will apply and replicate HP.
        if (!IsOwner) return;
        if (Time.time < nextHit) return;

        if (other.TryGetComponent<DamageableNet>(out var d))
        {
            nextHit = Time.time + hitCooldown;
            d.ApplyDamageServerRpc(damage);
        }
    }
}
