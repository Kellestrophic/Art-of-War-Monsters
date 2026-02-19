using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] public Transform launchPoint;          // muzzle / spawn transform
    [SerializeField] public GameObject projectilePrefab;    // must have UniversalProjectile

    [Header("Aim")]
    [Tooltip("If true, shoot left/right based on facing. If false, use 'aimDirection'.")]
    [SerializeField] public bool useFacing = true;
    [Tooltip("Used only when 'useFacing' = false. Will be normalized at fire time.")]
    [SerializeField] public Vector2 aimDirection = Vector2.right;

    [Header("Projectile")]
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private int projectileDamage = 1;

    [Header("Facing Source (optional)")]
    [Tooltip("If set, facing uses SpriteRenderer.flipX (right = !flipX, left = flipX). If null, falls back to transform scale sign.")]
    [SerializeField] private SpriteRenderer bodySR;

    private int FacingSign()
    {
        if (bodySR != null) return bodySR.flipX ? -1 : 1;
        return transform.lossyScale.x >= 0f ? 1 : -1;
    }

    public void FireProjectile()
    {
        if (!projectilePrefab || !launchPoint) return;

        // 1) Spawn at the muzzle
        GameObject go = Instantiate(projectilePrefab, launchPoint.position, Quaternion.identity);

        // 2) Choose a straight direction
        Vector2 dir = useFacing
            ? new Vector2(FacingSign(), 0f)
            : (aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.right);

        // 3) Flip the projectile art to match (optional)
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.flipX = (dir.x < 0f);

        // 4) Launch straight
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
            else Debug.LogError("[ProjectileLauncher] Projectile prefab is missing UniversalProjectile and Rigidbody2D.");
        }
    }
}
