using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("Hitbox")]
    [SerializeField] private Transform meleePoint;        // where the hitbox is (empty under the weapon/hand)
    [SerializeField] private float radius = 0.9f;         // hit radius
    [SerializeField] private LayerMask targetMask;        // set to Enemy (and anything else you want to hit)

    [Header("Damage")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float knockbackX = 6f;
    [SerializeField] private float knockbackY = 4f;

    // Call from your melee animation event
    public void AnimEvent_MeleeHit()
    {
        if (!meleePoint) { Debug.LogError("[PlayerMeleeAttack] meleePoint not set"); return; }

        Collider2D[] hits = Physics2D.OverlapCircleAll(meleePoint.position, radius, targetMask);
        foreach (var h in hits)
        {
            // Prefer Damagable.Hit so STRIKE POINTS are awarded by the victim
            var dmg = h.GetComponentInParent<Damagable>();
            if (dmg != null && dmg.IsAlive)
            {
                Vector2 dir = ((Vector2)h.transform.position - (Vector2)meleePoint.position).normalized;
                Vector2 knock = new Vector2(dir.x * knockbackX, knockbackY);
                dmg.Hit(damage, knock);  // <-- this is what triggers strike points
            }
            else
            {
                // Optional compatibility with any legacy receivers
                h.SendMessage("Hit", damage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!meleePoint) return;
        Gizmos.color = new Color(1, 0, 0, 0.35f);
        Gizmos.DrawWireSphere(meleePoint.position, radius);
    }
}
