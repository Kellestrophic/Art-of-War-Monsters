using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damagable))]
public class SkeletonWarriorEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2.5f;
    public float walkStopRate = 0.6f;
[Header("Attack Timing")]
public float attackHitDelay = 0.25f; // tweak this

    [Header("Detection")]
    public DetectionZone attackZone;
    public DetectionZone cliffDetectionZone;

    [Header("Melee Damage")]
    public int attackDamage = 10;
    public Vector2 knockback = new Vector2(2f, 1f);

    [Tooltip("Seconds between starting attacks.")]
    public float attackCooldown = 1.2f;

    [Tooltip("Delay after attack starts before damage is applied (sync to swing).")]
    public float hitDelay = 0.15f;

    [Tooltip("How close must the player be (X distance) to take damage.")]
    public float hitRange = 1.2f;

    [Header("Animator")]
    [Tooltip("Trigger name for Attack 1 animation.")]
    public string attack1Trigger = "Attack1";
    [Tooltip("Trigger name for Attack 2 animation.")]
    public string attack2Trigger = "Attack2";

    [Header("Stats")]
    public string enemyName = "SkeletonWarrior";

    // Components
    private Rigidbody2D rb;
    private TouchingDirections touching;
    private Animator animator;
    private Damagable damagable;

    // State
    private bool isDead = false;
    private Vector2 walkDir = Vector2.right;

    private float nextAttackTime = 0f;
    private int comboIndex = 0; // 0 -> Attack1, 1 -> Attack2

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        touching = GetComponent<TouchingDirections>();
        animator = GetComponent<Animator>();

        damagable = GetComponent<Damagable>() ?? GetComponentInChildren<Damagable>(true);
        if (damagable == null)
        {
            Debug.LogError("[SkeletonWarriorEnemy] No Damagable on " + name);
            enabled = false;
            return;
        }

        damagable.SetStatsKeyForTracking(enemyName);
    }

    private void OnEnable()
    {
        if (damagable != null)
        {
            damagable.damagableHit.RemoveListener(OnHit);
            damagable.damagableHit.AddListener(OnHit);

            damagable.onDeath.RemoveListener(OnDeath);
            damagable.onDeath.AddListener(OnDeath);
        }
    }

    private void OnDestroy()
    {
        if (damagable != null)
        {
            damagable.damagableHit.RemoveListener(OnHit);
            damagable.onDeath.RemoveListener(OnDeath);
        }
    }

    private void Update()
    {
        if (isDead) return;

        bool hasTarget = attackZone != null && attackZone.detectedColliders.Count > 0;

        // These match your other enemies
        if (animator) animator.SetBool(AnimationStrings.hasTarget, hasTarget);

        if (hasTarget)
        {
            FaceTarget();

            // Stop walking while attacking
            if (animator) animator.SetBool(AnimationStrings.canMove, false);

            // Start an attack on cooldown
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;

                // Trigger combo animation
                if (animator)
                {
                    if (comboIndex == 0) animator.SetTrigger(attack1Trigger);
                    else animator.SetTrigger(attack2Trigger);
                }

                comboIndex = 1 - comboIndex; // flip 0<->1

                // Apply damage shortly after swing begins (NO animation event needed)
                Invoke(nameof(ApplyMeleeDamage), hitDelay);
            }
        }
        else
        {
            // Patrol when no target
            if (animator) animator.SetBool(AnimationStrings.canMove, true);
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        // Flip if wall or cliff
        if ((touching.IsGrounded && touching.IsOnWall) ||
            (cliffDetectionZone != null && cliffDetectionZone.detectedColliders.Count == 0))
        {
            Flip();
        }

        // Move only if canMove
        bool canMove = animator ? animator.GetBool(AnimationStrings.canMove) : true;

        if (!damagable.LockVelocity)
        {
            if (canMove)
            {
                rb.linearVelocity = new Vector2(walkDir.x * walkSpeed, rb.linearVelocityY);
            }
            else
            {
                rb.linearVelocity = new Vector2(
                    Mathf.Lerp(rb.linearVelocityX, 0f, walkStopRate),
                    rb.linearVelocityY
                );
            }
        }
    }

    private void ApplyMeleeDamage()
    {
        if (isDead) return;

        if (attackZone == null || attackZone.detectedColliders.Count == 0)
            return;

        // Find a valid target Damagable in the zone (player)
        Damagable victim = null;
        Transform victimT = null;

        for (int i = 0; i < attackZone.detectedColliders.Count; i++)
        {
            var c = attackZone.detectedColliders[i];
            if (c == null) continue;

            var d = c.GetComponentInParent<Damagable>();
            if (d == null || !d.IsAlive) continue;

            // Must be opposite team
            if (d.isEnemy == damagable.isEnemy) continue;

            victim = d;
            victimT = d.transform;
            break;
        }

        if (victim == null || victimT == null) return;

        // Range check (prevents “swinging at nothing” hitting across the room)
        float dx = Mathf.Abs(victimT.position.x - transform.position.x);
        if (dx > hitRange) return;

        // Knockback in facing direction
        float facing = Mathf.Sign(transform.localScale.x);
        Vector2 kb = new Vector2(Mathf.Abs(knockback.x) * facing, knockback.y);

        victim.Hit(attackDamage, kb);
    }

    private void FaceTarget()
    {
        if (attackZone == null || attackZone.detectedColliders.Count == 0) return;

        Transform target = attackZone.detectedColliders[0].transform;
        float dir = target.position.x - transform.position.x;

        if (dir > 0f && walkDir.x < 0f) Flip();
        else if (dir < 0f && walkDir.x > 0f) Flip();
    }

    private void Flip()
    {
        walkDir *= -1f;
        transform.localScale = new Vector2(-transform.localScale.x, transform.localScale.y);
    }

    private void OnHit(int dmg, Vector2 kb)
    {
        rb.linearVelocity = new Vector2(kb.x, rb.linearVelocityY + kb.y);
    }

    private void OnDeath()
    {
        if (isDead) return;
        isDead = true;

        CancelInvoke();

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        if (attackZone != null) attackZone.enabled = false;
        if (cliffDetectionZone != null) cliffDetectionZone.enabled = false;

        if (animator) animator.SetBool(AnimationStrings.isAlive, false);

        Destroy(gameObject, 2f);
    }
}
