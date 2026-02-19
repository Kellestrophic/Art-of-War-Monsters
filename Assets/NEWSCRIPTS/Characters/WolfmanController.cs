using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class WolfmanController : MonoBehaviour
{
    [Header("Movement")]
    public float runSpeed = 6f;
    public float jumpForce = 12f;
[Header("Slam State")]
private bool forceGrounded;
private float forceGroundedTimer;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.15f;
    public LayerMask groundLayer;
[Header("Projectile Ability")]
[SerializeField] private GameObject projectilePrefab;
[SerializeField] private Transform projectileSpawnPoint;
[SerializeField] private int projectileDamage = 8;
[SerializeField] private int strikeCostOnProjectile = 2;
private bool abilityFiredThisCast;

    [Header("References")]
    public Animator anim;

    [Header("Attacks")]
    public Attack primaryAttack;   // normal claw
    public Attack abilityAttack;   // special attack
[Header("Slope Handling")]
public float slopeCheckDistance = 0.6f;
public float maxSlopeAngle = 45f;

private bool onSlope;
private Vector2 slopeNormal;
private float slopeAngle;
private enum MeleeType { Primary, Ability }
private MeleeType currentMelee = MeleeType.Primary;

    [Header("Primary Attack Settings")]
    public int primaryDamage = 10;
    public Vector2 primaryKnockback = new Vector2(3f, 1f);
    public float primaryCooldown = 0.35f;

    [Header("Ability Attack Settings")]
[Header("Strike Gain")]
[SerializeField] private int strikeGainPrimary = 1;
[SerializeField] private int strikeGainAbility = 2;
    public int abilityDamage = 20;
    public Vector2 abilityKnockback = new Vector2(6f, 2f);
    public float abilityCooldown = 1.2f;

    Rigidbody2D rb;
    Damagable damagable;

    Vector2 moveInput;
    bool isGrounded;
    bool isDead;
bool isJumping;

    float lastPrimaryTime = -999f;
    float lastAbilityTime = -999f;

    Vector3 baseScale;
    
    public void ForceSlam(float slamForce, float groundedDuration)
{
    // Cancel upward motion immediately
    rb.linearVelocity = new Vector2(rb.linearVelocity.x, -Mathf.Abs(slamForce));

    // Lock grounded state
    forceGrounded = true;
    forceGroundedTimer = groundedDuration;

    anim.SetBool("isJumping", false);
}
void CheckSlope()
{
    RaycastHit2D hit = Physics2D.Raycast(
        groundCheck.position,
        Vector2.down,
        slopeCheckDistance,
        groundLayer
    );

    if (hit)
    {
        slopeNormal = hit.normal;
        slopeAngle = Vector2.Angle(slopeNormal, Vector2.up);
        onSlope = slopeAngle > 0f && slopeAngle <= maxSlopeAngle;
    }
    else
    {
        onSlope = false;
    }
}

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        damagable = GetComponent<Damagable>();
        baseScale = transform.localScale;

        damagable.onDeath.AddListener(OnDeath);
        damagable.damagableHit.AddListener(OnHit);

        anim.SetBool("isAlive", true);
        anim.SetBool("canMove", true);
var bar = StrikeBarHUD.GetPlayerBar();
if (bar != null)
{
    bar.SetPoints(bar.maxPoints);
}

    }
    // Called by animation event
// Called by animation event
public void FireProjectile()
{
    if (abilityFiredThisCast) return;
    abilityFiredThisCast = true;

    if (isDead) return;

    var bar = StrikeBarHUD.GetPlayerBar();
    if (bar == null || bar.currentPoints < strikeCostOnProjectile)
        return;

    bar.AddPoints(-strikeCostOnProjectile);

    GameObject fx = Instantiate(
        projectilePrefab,
        projectileSpawnPoint.position,
        Quaternion.identity
    );

    // 🔁 Visual flip only
    Vector3 s = fx.transform.localScale;
    s.x = Mathf.Abs(s.x) * Mathf.Sign(transform.localScale.x);
    fx.transform.localScale = s;

    // 🔥 ASSIGN OWNER + ENABLE HITBOX
    var atk = fx.GetComponent<Attack>();
    if (atk != null)
    {
        atk.SetOwner(damagable);
        atk.EnableAttackHitbox(); // ⭐ THIS WAS MISSING
    }
}


public void ResolveMeleeHit(Damagable victim, Vector2 knockback)
{
    if (victim == null || !victim.IsAlive) return;

    int damage;
    int strikeGain;

    if (currentMelee == MeleeType.Ability)
    {
        damage = abilityDamage;
        strikeGain = strikeGainAbility;
    }
    else
    {
        damage = primaryDamage;
        strikeGain = strikeGainPrimary;
    }

    bool hitApplied = victim.Hit(damage, knockback);
    if (hitApplied)
    {
        StrikeBarHUD.GetPlayerBar()?.AddPoints(strikeGain);
        Debug.Log($"🐺 Wolfman hit ({currentMelee}) → {damage} dmg | Strike +{strikeGain}");
    }
}



    void Update()
    {
        if (forcedGroundTimer > 0f)
{
    forcedGroundTimer -= Time.deltaTime;
}

        if (isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        CheckGround();
        CheckSlope(); // 👈 ADD THIS
        UpdateAnimator();
        Flip();
    }

  void FixedUpdate()
{
    if (isDead) return;

    Vector2 vel = rb.linearVelocity;

    // HORIZONTAL
    vel.x = moveInput.x * runSpeed;

    // SLAM / GROUND LOCK
    if (forceGrounded)
    {
        vel.y = Mathf.Min(vel.y, 0f); // NEVER go up
    }
    else if (isGrounded && !isJumping)
    {
        vel.y = 0f;
    }

    rb.linearVelocity = vel;

    // Timer
    if (forceGrounded)
    {
        forceGroundedTimer -= Time.fixedDeltaTime;
        if (forceGroundedTimer <= 0f)
            forceGrounded = false;
    }
}


    // =========================
    // INPUT
    // =========================
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (isDead) return;
        moveInput = ctx.ReadValue<Vector2>();
    }

public void OnJump(InputAction.CallbackContext ctx)
{
    if (isDead || !ctx.started) return;

    Debug.Log($"🦘 Jump pressed | isGrounded = {isGrounded}");

    if (isGrounded)
    {
        isJumping = true;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        anim.SetBool("isJumping", true);
    }
}



    // NORMAL ATTACK
    public void OnAttack(InputAction.CallbackContext ctx)
{
    if (isDead || !ctx.performed) return;
    if (Time.time < lastPrimaryTime + primaryCooldown) return;

    lastPrimaryTime = Time.time;
    currentMelee = MeleeType.Primary;
    anim.SetTrigger("AttackLight");
}


    // ABILITY ATTACK
  public void OnAbility(InputAction.CallbackContext ctx)
{
    if (isDead || !ctx.performed) return;
    if (Time.time < lastAbilityTime + abilityCooldown) return;

    lastAbilityTime = Time.time;
    abilityFiredThisCast = false;   // 🔥 RESET
    anim.SetTrigger("AbilityAttack");
}

// =========================
// FORCED GROUND (BINDER ORB)
// =========================
private float forcedGroundTimer = 0f;

public void ForceGrounded(float duration)
{
    forcedGroundTimer = Mathf.Max(forcedGroundTimer, duration);
}

    // =========================
    // ATTACK DATA
    // =========================
   
    // =========================
    // GROUND / ANIM
    // =========================
   void CheckGround()
{
    if (forceGrounded)
{
    isGrounded = true;
    return;
}

    bool wasGrounded = isGrounded;

    // Cast straight DOWN only
    RaycastHit2D hit = Physics2D.Raycast(
        groundCheck.position,
        Vector2.down,
        groundRadius,
        groundLayer
    );

if (forcedGroundTimer > 0f)
{
    isGrounded = true;
}
else
{
    isGrounded = hit.collider != null;
}


    if (isGrounded && !wasGrounded)
    {
        isJumping = false;
        anim.SetBool("isJumping", false);
    }
}


    void UpdateAnimator()
    {
        anim.SetBool("isRunning", Mathf.Abs(moveInput.x) > 0.1f);
        anim.SetBool("isIdle", Mathf.Abs(moveInput.x) < 0.05f);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    void Flip()
    {
        if (moveInput.x > 0)
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        else if (moveInput.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
    }

    // =========================
    // DAMAGE / DEATH
    // =========================
    void OnHit(int damage, Vector2 knockback)
    {
        if (isDead) return;

        anim.SetTrigger("hit");
        rb.linearVelocity = new Vector2(knockback.x, rb.linearVelocity.y + knockback.y);
    }

    void OnDeath()
    {
        if (isDead) return;
        isDead = true;

        anim.SetBool("isAlive", false);
        anim.SetTrigger("DeathTrigger");

        rb.linearVelocity = Vector2.zero;

        var deathMenu = FindFirstObjectByType<DeathMenuManager>();
        if (deathMenu != null)
            deathMenu.ShowDeathMenu();
    }


}
