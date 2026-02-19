using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damagable))]
public class ExplodingCultist : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 3.6f;
    public float walkStopRate = 0.6f;
private Transform player;

    [Header("Detection")]
    public DetectionZone chaseZone;          // player proximity (run)
    public Collider2D explodeHitbox;         // trigger proximity (explode)

    [Header("Explosion")]
    public float explosionRadius = 2.2f;
    public int explosionDamage = 40;
    public GameObject explosionVfxPrefab;
    public LayerMask playerMask;

    [Header("Animator")]
    public string paramCanMove = AnimationStrings.canMove;
    public string paramIsRunning = "IsRunning";
    public string paramIsAlive = AnimationStrings.isAlive;
    public string trigExplode = "Explode";
private bool wasChasingLastFrame;
private bool wasChasing;

    private Rigidbody2D rb;
    private TouchingDirections touching;
    private Animator animator;
    private Damagable damagable;

    private bool isDead;
    private bool hasTarget;
    private bool exploding;

    public enum WalkDir { Right, Left }
    private WalkDir walkDir = WalkDir.Right;
    private Vector2 walkVec = Vector2.right;

  private bool CanMove => true;


    void Awake()
    {
        explodeHitbox.enabled = false;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        rb = GetComponent<Rigidbody2D>();
        touching = GetComponent<TouchingDirections>();
        animator = GetComponent<Animator>();
        damagable = GetComponent<Damagable>();

        animator.applyRootMotion = false;
        animator.SetBool(paramIsAlive, true);
    }

 void Update()
{
    animator.speed = hasTarget ? 1.35f : 1f;

    if (isDead) return;

    bool nowChasing = false;
if (hasTarget && !explodeHitbox.enabled)
    explodeHitbox.enabled = true;

if (!hasTarget && explodeHitbox.enabled)
    explodeHitbox.enabled = false;

if (chaseZone != null)
{
    for (int i = chaseZone.detectedColliders.Count - 1; i >= 0; i--)
    {
        var c = chaseZone.detectedColliders[i];

        // clean up dead entries
        if (!c)
        {
            chaseZone.detectedColliders.RemoveAt(i);
            continue;
        }

        // ONLY chase the player
        if (c.CompareTag("Player") || c.transform.root.CompareTag("Player"))
        {
            nowChasing = true;
            break;
        }
    }
}

    // Detect chase exit
    if (wasChasing && !nowChasing)
    {
        // Player just LEFT chase zone → turn around
        SetDirection(walkDir == WalkDir.Right ? WalkDir.Left : WalkDir.Right);
    }

    wasChasing = nowChasing;
    hasTarget = nowChasing;

    animator.SetBool("hasTarget", hasTarget);
    animator.SetBool("IsRunning", hasTarget);
    animator.SetBool("IsWalking", !hasTarget);
    animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
}


private void TurnAround()
{
    SetDirection(walkDir == WalkDir.Right ? WalkDir.Left : WalkDir.Right);
}


private void FacePlayer()
{
    if (!player) return;

    float dir = player.position.x - transform.position.x;

    if (dir > 0 && walkDir != WalkDir.Right)
        SetDirection(WalkDir.Right);
    else if (dir < 0 && walkDir != WalkDir.Left)
        SetDirection(WalkDir.Left);
}


private void SetDirection(WalkDir dir)
{
    walkDir = dir;
    walkVec = dir == WalkDir.Right ? Vector2.right : Vector2.left;

    transform.localScale = new Vector2(
        Mathf.Abs(transform.localScale.x) * (dir == WalkDir.Right ? 1 : -1),
        transform.localScale.y
    );
}




void FixedUpdate()
{
    if (isDead || exploding) return;

    if (hasTarget)
    {
        FacePlayer();
    }
    else if (touching.IsGrounded && touching.IsOnWall)
    {
        SetDirection(walkDir == WalkDir.Right ? WalkDir.Left : WalkDir.Right);
    }

    float speed = hasTarget ? runSpeed : walkSpeed;
    rb.linearVelocity = new Vector2(speed * walkVec.x, rb.linearVelocityY);
}




    private void Flip()
    {
        walkDir = walkDir == WalkDir.Right ? WalkDir.Left : WalkDir.Right;
        transform.localScale = new Vector2(transform.localScale.x * -1f, transform.localScale.y);
        walkVec = walkDir == WalkDir.Right ? Vector2.right : Vector2.left;
    }

    // ============================================================
    // EXPLOSION TRIGGER (HITBOX)
    // ============================================================
  
    private void Explode()
    {
        exploding = true;
        isDead = true;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        animator.SetBool(paramIsAlive, false);
        animator.SetTrigger(trigExplode);
    }
public void TriggerExplosion()
{
    if (isDead || exploding) return;

    exploding = true;   // 🔒 LOCK IMMEDIATELY
    Explode();
}


    // ============================================================
    // ANIMATION EVENT (CALL THIS AT IMPACT FRAME)
    // ============================================================
    public void AnimEvent_ExplodeDamage()
    {
        if (explosionVfxPrefab)
            Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);

        var hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, playerMask);

        foreach (var h in hits)
        {
            if (!h.CompareTag("Player")) continue;

            var dmg = h.GetComponentInParent<Damagable>();
            if (dmg != null)
                dmg.Hit(explosionDamage);

            break;
        }

        Destroy(gameObject, 1.2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
