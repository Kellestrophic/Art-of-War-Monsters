using System.Collections;
using UnityEngine;

public class EnemyRangedShooter : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Transform shootOrigin;          // muzzle / spawn transform (child)
    [SerializeField] private GameObject projectilePrefab;    // prefab must have UniversalProjectile

    [Header("Projectile")]
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private float fireCooldown = 1.0f;

    [Header("Facing Source")]
    [Tooltip("Drag the enemy's main SpriteRenderer here. If left null, we fall back to transform scale sign.")]
    [SerializeField] private SpriteRenderer bodySR;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private float _nextFire;

    /// <summary>
    /// Computes +1 (right) / -1 (left) from current facing.
    /// If a SpriteRenderer is assigned, use flipX; otherwise use transform.lossyScale.x.
    /// (Robust: if either system says 'left', we go left.)
    /// </summary>
    private int FacingSign()
    {
        int byFlip  = (bodySR != null && bodySR.flipX) ? -1 : 1;
        int byScale = (transform.lossyScale.x >= 0f) ? 1 : -1;
        return (byFlip < 0 || byScale < 0) ? -1 : 1;
    }

    /// <summary>Animation Event hook for the 'Shoot' clip, or call from AI.</summary>
    public void FireProjectile()
    {
        StartCoroutine(CoShoot());
    }

    public void FireRanged()
    {
        StartCoroutine(CoShoot());
    }

    private IEnumerator CoShoot()
    {
        if (Time.time < _nextFire) yield break;
        _nextFire = Time.time + fireCooldown;

        yield return new WaitForSeconds(0.05f); // tiny windup, optional

        if (projectilePrefab == null || shootOrigin == null)
        {
            Debug.LogError("[EnemyRangedShooter] Missing projectilePrefab or shootOrigin.");
            yield break;
        }

        Vector2 origin = shootOrigin.position;

        // ✅ STRICTLY from facing (no player math, no muzzle rotation)
        int sign = FacingSign();
        Vector2 dir = new Vector2(sign, 0f);

        if (debugLogs)
        {
            string flip = (bodySR != null) ? bodySR.flipX.ToString() : "(no SR)";
            Debug.Log($"[EnemyRangedShooter] Fire dir.x={dir.x}  flipX={flip}  lossyScale.x={transform.lossyScale.x}");
            Debug.DrawRay(origin, dir * 1.0f, Color.red, 0.75f);
        }

        GameObject go = Instantiate(projectilePrefab, origin, Quaternion.identity);

        // Flip projectile art to match direction (cosmetic)
        var psr = go.GetComponentInChildren<SpriteRenderer>();
        if (psr) psr.flipX = (dir.x < 0f);

        var uni = go.GetComponent<UniversalProjectile>();
        if (uni != null)
        {
            uni.Launch(direction: dir,
                       overrideSpeed: projectileSpeed,
                       overrideDamage: projectileDamage,
                       owner: gameObject);
        }
        else
        {
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb) rb.linearVelocity = dir * projectileSpeed;
            else Debug.LogWarning("[EnemyRangedShooter] Projectile missing UniversalProjectile and Rigidbody2D.");
        }
    }
}
