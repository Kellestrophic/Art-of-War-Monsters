using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Attack : MonoBehaviour
{
    [Header("Knockback")]
    public Vector2 knockback = new Vector2(2f, 1f);

    [Header("Behavior")]
    [SerializeField] private bool flipWithFacing = true;

    private Collider2D hitbox;
    private Damagable ownerDamagable;
  private NeoDraculaController draculaOwner;
    private WolfmanController wolfmanOwner;
    private void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        hitbox.isTrigger = true;
        hitbox.enabled = false;

 ownerDamagable = GetComponentInParent<Damagable>();
        draculaOwner = GetComponentInParent<NeoDraculaController>();
        wolfmanOwner = GetComponentInParent<WolfmanController>();
        if (ownerDamagable == null)
            Debug.LogError("[Attack] No Damagable found on owner!");
    }
[SerializeField] private int damage = 1;

public void SetDamage(int value)
{
    damage = Mathf.Max(0, value);
}

    // Called by animation events
    public void EnableAttackHitbox()  => hitbox.enabled = true;
    public void DisableAttackHitbox() => hitbox.enabled = false;

private void OnTriggerEnter2D(Collider2D other)
{
    if (!hitbox.enabled) return;

    var victim = other.GetComponentInParent<Damagable>();
    if (victim == null) return;
    if (!victim.IsAlive) return; // 🔥 BLOCK DEAD TARGETS

    if (victim == ownerDamagable) return;
    if (victim.isEnemy == ownerDamagable.isEnemy) return;
    if (!ownerDamagable.IsAlive) return;


    Vector2 finalKnockback = knockback;
    if (flipWithFacing)
    {
        float facing = Mathf.Sign(transform.root.localScale.x);
        finalKnockback.x = Mathf.Abs(knockback.x) * facing;
    }

    // Apply damage
    if (draculaOwner != null)
        draculaOwner.ResolveMeleeHit(victim, finalKnockback);
    else if (wolfmanOwner != null)
        wolfmanOwner.ResolveMeleeHit(victim, finalKnockback);
    else
        victim.Hit(damage, finalKnockback);

    // 🔥 ABSOLUTE RULE
    hitbox.enabled = false;
}
public void SetOwner(Damagable owner)
{
    ownerDamagable = owner;
}

}
