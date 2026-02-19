using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeSeconds = 5f;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockback = 3f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D col;
    [SerializeField] private SpriteRenderer sprite; // optional, for flipX

[Header("Initial Tracking")]
[SerializeField] private float trackingDuration = 0.15f; // seconds
[SerializeField] private float trackingStrength = 12f;   // turn speed
private Transform trackingTarget;
private float trackingTimer;
private Vector2 lockedDirection;


    // cached travel
   private Vector2 dir = Vector2.right;
    private Vector2 expectedVel = Vector2.right;
    private bool launched = false;

    // owner & team
    private GameObject owner; // ✅ so "this.owner = owner;" compiles
    private Transform ownerRoot;
    private bool ownerIsEnemy = false;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!col) col = GetComponent<Collider2D>();
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        col.isTrigger = true; // damage via triggers
    }

    private void OnEnable()
    {
        CancelInvoke();
        Invoke(nameof(DestroySelf), lifeSeconds);
    }

    /// <summary>Launch straight; 'owner' (optional) will be ignored on contact.</summary>
   public void Launch(Vector2 direction, float overrideSpeed = -1f, int overrideDamage = -1, GameObject owner = null)
{
    // Base direction
    dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

    if (overrideSpeed > 0f) speed = overrideSpeed;
    if (overrideDamage >= 0) damage = overrideDamage;

    transform.SetParent(null);

    this.owner = owner; // ✅ now valid
    ownerRoot = owner ? owner.transform.root : null;
    ownerIsEnemy = DetermineIsEnemyTeam(ownerRoot);

    // Tracking init
    lockedDirection = dir;
    trackingTimer = trackingDuration;

    // Find player once at fire time
    var player = FindFirstObjectByType<NeoDraculaController>();
    trackingTarget = (player != null) ? player.transform : null;

    // Visual orientation
    if (sprite) sprite.flipX = (dir.x < 0f);
    else transform.right = new Vector3(dir.x, dir.y, 0f);

    launched = true;

    if (debugLogs)
        Debug.Log($"[Projectile] Launch dir={dir} speed={speed} owner={(owner ? owner.name : "null")} enemyTeam={ownerIsEnemy}");
}


    private void FixedUpdate()
{
    if (!launched) return;

    // Initial tracking phase
    if (trackingTarget != null && trackingTimer > 0f)
    {
        trackingTimer -= Time.fixedDeltaTime;

        Vector2 toTarget = ((Vector2)trackingTarget.position - rb.position).normalized;

        lockedDirection = Vector2.Lerp(
            lockedDirection,
            toTarget,
            trackingStrength * Time.fixedDeltaTime
        ).normalized;
    }

    // Keep legacy dir synced so your existing knockback/logs work
    dir = lockedDirection;

    expectedVel = lockedDirection * speed;
    rb.linearVelocity = expectedVel;
}


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (debugLogs)
        Debug.Log($"[Proj] hit {other.name}, tag={other.tag}, layer={LayerMask.LayerToName(other.gameObject.layer)}, targetIsEnemy={DetermineIsEnemyTeam(other.transform.root)}, ownerIsEnemy={ownerIsEnemy}");

        if (!launched) return;

        // ignore shooter (any child collider)
        if (ownerRoot != null && other.transform.root == ownerRoot) return;
        

        HandleHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!launched) return;

        if (ownerRoot != null && collision.transform.root == ownerRoot) return;

        HandleHit(collision.collider);
    }

    private void HandleHit(Collider2D other)
{
    // Projectile vs projectile — mutually destroy
    var otherProj = other.GetComponentInParent<UniversalProjectile>();
    if (otherProj != null && otherProj != this)
    {
        otherProj.Pop();
        DestroySelf();
        return;
    }
    var otherEnemyProj = other.GetComponentInParent<EnemyProjectile>();
if (otherEnemyProj != null && otherEnemyProj != this)
    return; // ignore other enemy arrows

  // ALWAYS resolve to the victim's Damagable anywhere in the rig
    var dmg = other.GetComponentInParent<Damagable>();
    if (dmg != null && dmg.IsAlive)
    {
        // ignore shooter
        if (ownerRoot != null && dmg.transform.root == ownerRoot) return;

        // ONLY damage opposite team
        bool targetIsEnemy = DetermineIsEnemyTeam(dmg.transform.root);
        if (targetIsEnemy == ownerIsEnemy) return;

       dmg.Hit(damage, lockedDirection * knockback);

        DestroySelf();
        return;
    }

    // Solid world (non-trigger) — pop on impact
    // Hit solid world only (ignore all triggers)
if (!other.isTrigger)
{
    DestroySelf();
}

}

   private static bool DetermineIsEnemyTeam(Transform root)
{
    if (root == null) return false; // default to player side to avoid self-harm

    // Prefer any Damagable in the rig (child OR root)
    var d = root.GetComponentInChildren<Damagable>();
    if (d == null) d = root.GetComponentInParent<Damagable>();
    if (d != null) return d.isEnemy;

    // Fallbacks if Damagable missing
    if (root.CompareTag("Enemy"))  return true;
    if (root.CompareTag("Player")) return false;

    return false;
}

    private void DestroySelf()
    {
        CancelInvoke();
        Destroy(gameObject);
    }

    // Allow other projectiles to despawn us
    public void Pop() => DestroySelf();
}
