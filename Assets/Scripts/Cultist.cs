using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damagable))]
public class Cultist : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float walkStopRate = 0.6f;

    [Header("Detection / Combat")]
    public DetectionZone attackZone;
    public DetectionZone cliffDetectionZone;
    public Collider2D attackCollider;
[Header("Attack Cooldown")]
public float attackCooldownTime = 1.25f;
[Header("Combat Stats")]
[SerializeField] private int attackDamage = 1;

    [Header("Stats")]
    public string enemyName = "Cultist";
[Header("Hit Reaction")]
[SerializeField] private float hitFacingLockTime = 0.25f;

private float facingLockedUntil = -999f;
private bool FacingLocked => Time.time < facingLockedUntil;

    // Components
    private Rigidbody2D rb;
    private TouchingDirections touchingDirections;
    private Animator animator;
    private Damagable damagable;

    // State
    private bool isDead = false;

    public enum WalkableDirection { Right, Left }
    private WalkableDirection _walkDirection;
    private Vector2 walkDirectionVector = Vector2.right;

    public WalkableDirection WalkDirection
    {
        get => _walkDirection;
        set
        {
            if (_walkDirection != value)
            {
                transform.localScale = new Vector2(transform.localScale.x * -1, transform.localScale.y);
                walkDirectionVector = value == WalkableDirection.Right ? Vector2.right : Vector2.left;
            }
            _walkDirection = value;
        }
    }
    // 🔥 Animation Event: call this at the START of the attack animation (first frame)
public void AnimEvent_AttackStarted()
{
    attackRequested = false;
}

    // Prevents spamming triggers every frame
private bool attackRequested = false;

// Optional: helps debugging and prevents re-trigger while in attack
private static readonly int AttackTriggerHash = Animator.StringToHash(AnimationStrings.attackTrigger);

private bool CanAttack => GetAttackCooldown() <= 0f && HasTarget && CanMove;
private void TryAttack()
{
    // Must have target, not be on cooldown, must be able to move (your original logic)
    if (!CanAttack) return;

    // ✅ NEW: don't keep triggering every frame
    if (attackRequested) return;

    attackRequested = true;

    // Trigger attack animation ONCE
    animator.ResetTrigger(AnimationStrings.attackTrigger);
    animator.SetTrigger(AnimationStrings.attackTrigger);

    // Start cooldown immediately (same as before)
    SetAttackCooldown(attackCooldownTime);
}


    public bool _hasTarget = false;
    public bool HasTarget
    {
        get => _hasTarget;
        private set
        {
            _hasTarget = value;
            animator.SetBool(AnimationStrings.hasTarget, value);
        }
    }

    public bool CanMove => animator.GetBool(AnimationStrings.canMove);
    private float GetAttackCooldown() => animator.GetFloat(AnimationStrings.attackCooldown);
    private void SetAttackCooldown(float value) => animator.SetFloat(AnimationStrings.attackCooldown, Mathf.Max(value, 0));

    private Attack attack;

private void Awake()
{
   

    rb = GetComponent<Rigidbody2D>();
    touchingDirections = GetComponent<TouchingDirections>();
    animator = GetComponent<Animator>();

    damagable = GetComponent<Damagable>() ?? GetComponentInChildren<Damagable>();
    if (damagable == null)
    {
        Debug.LogError("[Cultist] No Damagable on " + name);
        return;
    }

    // 🔑 Hook attack damage
    attack = GetComponentInChildren<Attack>();
    if (attack != null)
    {
        attack.SetDamage(attackDamage);
    }
}



    private void OnEnable()
    {
        // Re-hook listeners in case of pooling or re-enabling
        if (damagable == null)
            // ⭐ FORCE statsKeyOverride so all kill tracking = "Cultist"
    damagable.SetStatsKeyForTracking(enemyName);

            damagable = GetComponent<Damagable>()
                         ?? GetComponentInChildren<Damagable>(true)
                         ?? GetComponentInParent<Damagable>(true);

        if (damagable != null)
        {
            damagable.damagableHit.RemoveListener(OnHit);
            damagable.damagableHit.AddListener(OnHit);

            damagable.onDeath.RemoveListener(OnDeath);
            damagable.onDeath.AddListener(OnDeath);

            Debug.Log($"[Cultist] ✅ Hooked onDeath for {name} (goId={gameObject.GetInstanceID()}) " +
                      $"→ Damagable on {damagable.gameObject.name} (damId={damagable.gameObject.GetInstanceID()})");
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

    HasTarget = attackZone != null && attackZone.detectedColliders.Count > 0;

    // Tick cooldown
    if (GetAttackCooldown() > 0)
        SetAttackCooldown(GetAttackCooldown() - Time.deltaTime);

    // Attempt attack
    TryAttack();
}

    private void FixedUpdate()
    {
        if (isDead) return;

       if (!FacingLocked &&
    ((touchingDirections.IsGrounded && touchingDirections.IsOnWall) ||
     (cliffDetectionZone != null && cliffDetectionZone.detectedColliders.Count == 0)))
{
    FlipDirection();
}


        if (!damagable.LockVelocity)
        {
            if (CanMove)
                rb.linearVelocity = new Vector2(walkSpeed * walkDirectionVector.x, rb.linearVelocityY);
            else
                rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocityX, 0, walkStopRate), rb.linearVelocityY);
        }
    }

    private void FlipDirection()
    {
        WalkDirection = WalkDirection == WalkableDirection.Right ? WalkableDirection.Left : WalkableDirection.Right;
    }

   public void OnHit(int damage, Vector2 knockback)
{
    // Lock facing briefly
    facingLockedUntil = Time.time + hitFacingLockTime;

    rb.linearVelocity = new Vector2(
        knockback.x,
        rb.linearVelocityY + knockback.y
    );
}


    public void EnableAttackCollider()
    {
        if (attackCollider != null)
            attackCollider.enabled = true;
    }

    public void DisableAttackCollider()
    {
        if (attackCollider != null)
            attackCollider.enabled = false;
    }

    // === Death chain ===
    public void OnDeath()
    {
        Debug.Log("[Cultist] OnDeath event received for " + name + " (id=" + gameObject.GetInstanceID() + ")");
        Die();
    }

    public void Die()
    {
        if (isDead) return;        // one-shot guard
        isDead = true;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        DisableAttackCollider();
        if (attackZone != null) attackZone.enabled = false;
        if (cliffDetectionZone != null) cliffDetectionZone.enabled = false;

        animator.SetBool(AnimationStrings.isAlive, false);

        Destroy(gameObject, 2f);
    }
}
