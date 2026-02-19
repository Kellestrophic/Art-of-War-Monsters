using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class GhostProjectile : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeSeconds = 6f;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockback = 2f;

    [Header("Grounding Effect")]
    [SerializeField] private float groundedDuration = 1.25f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Rigidbody2D rb;
    private Collider2D col;

    private Vector2 velocity;
    private Transform ownerRoot;
    private bool ownerIsEnemy;

    // Prevent multi-hit spam
    private readonly HashSet<Damagable> hitTargets = new();

    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        col.isTrigger = true;
    }

    private void OnEnable()
    {
        CancelInvoke();
        Invoke(nameof(DestroySelf), lifeSeconds);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = velocity;
    }

    // =====================================================
    // LAUNCH
    // =====================================================

    public void Launch(Vector2 direction, GameObject owner)
    {
        velocity = direction.normalized * speed;

        transform.SetParent(null);
        ownerRoot = owner ? owner.transform.root : null;
        ownerIsEnemy = DetermineIsEnemyTeam(ownerRoot);

        rb.linearVelocity = velocity;

        if (debugLogs)
            Debug.Log($"[GhostProjectile] Launched dir={velocity} ownerEnemy={ownerIsEnemy}");
    }

    // =====================================================
    // HIT LOGIC (PHASE-THROUGH)
    // =====================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        var dmg = other.GetComponentInParent<Damagable>();
        if (dmg == null || !dmg.IsAlive)
            return;

        // Ignore owner
        if (ownerRoot != null && dmg.transform.root == ownerRoot)
            return;

        // Team check
        bool targetIsEnemy = DetermineIsEnemyTeam(dmg.transform.root);
        if (targetIsEnemy == ownerIsEnemy)
            return;

        // Prevent multi-hit spam
        if (hitTargets.Contains(dmg))
            return;

        hitTargets.Add(dmg);

        // Apply damage
        dmg.Hit(damage, velocity.normalized * knockback);

        // Apply grounding
        ApplyGrounded(dmg);

        if (debugLogs)
            Debug.Log($"[GhostProjectile] Hit {dmg.name} (grounded)");
    }

    // =====================================================
    // GROUNDING EFFECT
    // =====================================================

    private void ApplyGrounded(Damagable dmg)
{
    var behaviours = dmg.GetComponentsInParent<MonoBehaviour>();

    foreach (var b in behaviours)
    {
        var method = b.GetType().GetMethod(
            "ForceSlam",
            new[] { typeof(float), typeof(float) }
        );

        if (method != null)
        {
            method.Invoke(b, new object[] { 18f, 1.25f });
            return;
        }
    }
}


    // =====================================================
    // TEAM RESOLUTION
    // =====================================================

    private static bool DetermineIsEnemyTeam(Transform root)
    {
        if (!root) return false;

        var d = root.GetComponentInChildren<Damagable>();
        if (d != null) return d.isEnemy;

        if (root.CompareTag("Enemy")) return true;
        if (root.CompareTag("Player")) return false;

        return false;
    }

    private void DestroySelf()
    {
        CancelInvoke();
        Destroy(gameObject);
    }
}
