using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damagable))]
public class SkeletonArcher : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2.5f;
    public float walkStopRate = 0.6f;

    [Header("Detection")]
    public DetectionZone attackZone;
    public DetectionZone cliffDetectionZone;

    [Header("Ranged Attack")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float shootCooldown = 1.6f;

    [Header("Stats")]
    public string enemyName = "SkeletonArcher";

    // Components
    private Rigidbody2D rb;
    private TouchingDirections touchingDirections;
    private Animator animator;
    private Damagable damagable;

    // State
    private bool isDead = false;
    private float lastShotTime = -999f;

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
                transform.localScale = new Vector2(
                    transform.localScale.x * -1,
                    transform.localScale.y
                );

                walkDirectionVector =
                    value == WalkableDirection.Right ? Vector2.right : Vector2.left;
            }
            _walkDirection = value;
        }
    }

    public bool HasTarget { get; private set; }
    public bool CanMove => animator.GetBool(AnimationStrings.canMove);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        touchingDirections = GetComponent<TouchingDirections>();
        animator = GetComponent<Animator>();

        damagable = GetComponent<Damagable>()
                 ?? GetComponentInChildren<Damagable>();

        if (damagable == null)
        {
            Debug.LogError("[SkeletonArcher] No Damagable on " + name);
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        // Force stats tracking key
        damagable.SetStatsKeyForTracking(enemyName);

        damagable.damagableHit.RemoveListener(OnHit);
        damagable.damagableHit.AddListener(OnHit);

        damagable.onDeath.RemoveListener(OnDeath);
        damagable.onDeath.AddListener(OnDeath);
    }
private void FaceTarget()
{
    if (attackZone == null || attackZone.detectedColliders.Count == 0)
        return;

    Transform target = attackZone.detectedColliders[0].transform;

    float dir = target.position.x - transform.position.x;

    if (dir > 0 && WalkDirection != WalkableDirection.Right)
        WalkDirection = WalkableDirection.Right;
    else if (dir < 0 && WalkDirection != WalkableDirection.Left)
        WalkDirection = WalkableDirection.Left;
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
animator.SetBool(AnimationStrings.hasTarget, HasTarget);

if (HasTarget)
{
    FaceTarget(); // 👈 NEW
}

        if (HasTarget && Time.time - lastShotTime >= shootCooldown)
        {
            animator.SetTrigger("Shoot");
            lastShotTime = Time.time;
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        if ((touchingDirections.IsGrounded && touchingDirections.IsOnWall) ||
            (cliffDetectionZone != null &&
             cliffDetectionZone.detectedColliders.Count == 0))
        {
            FlipDirection();
        }

        if (!damagable.LockVelocity)
        {
            if (CanMove)
            {
                rb.linearVelocity = new Vector2(
                    walkSpeed * walkDirectionVector.x,
                    rb.linearVelocityY
                );
            }
            else
            {
                rb.linearVelocity = new Vector2(
                    Mathf.Lerp(rb.linearVelocityX, 0, walkStopRate),
                    rb.linearVelocityY
                );
            }
        }
    }

    private void FlipDirection()
    {
        WalkDirection =
            WalkDirection == WalkableDirection.Right
            ? WalkableDirection.Left
            : WalkableDirection.Right;
    }

    public void OnHit(int damage, Vector2 knockback)
    {
        rb.linearVelocity = new Vector2(
            knockback.x,
            rb.linearVelocityY + knockback.y
        );
    }

    // =========================
    // RANGED ATTACK (ANIM EVENT)
    // =========================
 public void FireArrow()
{
    if (isDead || arrowPrefab == null || firePoint == null)
        return;

    if (attackZone == null || attackZone.detectedColliders.Count == 0)
        return;

    Transform target = attackZone.detectedColliders[0].transform;

    Vector2 dir = (target.position - firePoint.position).normalized;

    GameObject arrow =
        Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);

    var proj = arrow.GetComponent<EnemyProjectile>();
    if (proj != null)
    {
        proj.Launch(dir, owner: gameObject);
    }
}


    // =========================
    // DEATH
    // =========================
   // =========================
// DEATH
// =========================
public void OnDeath()
{
    Die();
}

private void Die()
{
    if (isDead) return;
    isDead = true;

    // Stop movement, but DO NOT disable physics yet
    rb.linearVelocity = Vector2.zero;

    // Disable AI / sensors
    if (attackZone != null) attackZone.enabled = false;
    if (cliffDetectionZone != null) cliffDetectionZone.enabled = false;

    // Let the animator fully play the death animation
    float deathDuration = GetDeathAnimationLength();
    Invoke(nameof(FinalizeDeath), deathDuration);
}

private void FinalizeDeath()
{
    // Safe cleanup AFTER animation finishes
    rb.simulated = false;
    Destroy(gameObject);
}

private float GetDeathAnimationLength()
{
    if (animator == null || animator.runtimeAnimatorController == null)
        return 1.5f; // safe fallback

    foreach (var clip in animator.runtimeAnimatorController.animationClips)
    {
        if (clip.name.ToLower().Contains("death"))
            return clip.length;
    }

    return 1.5f;
}


}
