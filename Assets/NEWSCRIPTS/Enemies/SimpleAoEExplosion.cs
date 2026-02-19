using UnityEngine;

public static class SimpleAoEExplosion
{
    /// <param name="damageMask">Only colliders on these layers will be damaged/knocked. Pass LayerMask.GetMask("Player") to only hit the player.</param>
    public static void DoExplosion(
        Vector2 center,
        float radius,
        int damage,
        float knockback,
        GameObject sourceToIgnore = null,
        LayerMask damageMask = default)
    {
        var hits = Physics2D.OverlapCircleAll(center, radius, ~0);
        foreach (var hit in hits)
        {
            if (!hit) continue;
            var go = hit.gameObject;

            // Ignore self
            if (sourceToIgnore && go == sourceToIgnore) continue;

            // If a mask was provided, require the target to be on one of those layers
            if (damageMask.value != 0 && ((damageMask.value & (1 << go.layer)) == 0)) continue;

            // Your Damagable API: Hit(int damage, Vector2 knockbackVector)
            var dmg = go.GetComponent<Damagable>();
            if (dmg != null)
            {
                Vector2 kbVec = ((Vector2)hit.transform.position - center).normalized * knockback;
                dmg.Hit(damage, kbVec);
            }

            // Optional physical shove
            var body = hit.attachedRigidbody;
            if (body && (!sourceToIgnore || body.gameObject != sourceToIgnore))
            {
                Vector2 dir = ((Vector2)hit.transform.position - center).normalized;
                body.AddForce(dir * knockback, ForceMode2D.Impulse);
            }
        }
    }
}
