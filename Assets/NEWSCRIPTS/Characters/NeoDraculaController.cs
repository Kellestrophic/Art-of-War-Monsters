using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class NeoDraculaController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpForce = 12f;
[SerializeField] private Animator castEffectAnimator;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.15f;
    public LayerMask groundLayer;
// ----------------------------
// DAMAGE & STRIKE SETTINGS
// ----------------------------
private bool isAttacking;
private bool canAttack = true;


[Header("Damage Values")]
[SerializeField] private int meleeDamage = 10;
[SerializeField] private int rangedDamage = 6;

[Header("Strike Bar Interaction")]
[SerializeField] private int strikeGainOnMelee = 1;
[SerializeField] private int strikeCostOnRanged = 1;


[Header("Slope Handling")]
public float slopeCheckDistance = 0.6f;
public float maxSlopeAngle = 45f;

private bool onSlope;
private Vector2 slopeNormal;
private float slopeAngle;

    [Header("References")]
    public Animator anim;
    private Rigidbody2D rb;
    private Damagable damagable;

    // Input
    private Vector2 moveInput;
    private bool isRunning;

    // States
    private bool isGrounded;
    private bool isJumping;
    private bool isFalling;
    private bool isLanding;

    // Death flag
    private bool isDead = false;

    // Store original scale so animations never override it
    private Vector3 baseScale;

    // ----------------------------
    // ATTACK SETTINGS
    // ----------------------------
    [Header("Attack Settings")]
    public float meleeCooldown = 0.35f;
    private float lastMeleeTime = -999f;

    [Header("Ranged Attack Settings")]
    public float rangedCooldown = 0.8f;
    private float lastRangedTime = -10f;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
public void BeginAttack()
{
    isAttacking = true;
}

public void EndAttack()
{
    isAttacking = false;
}

    void Awake()
    {
        // Start with full strike bar
var bar = StrikeBarHUD.GetPlayerBar();
if (bar != null)
{
    bar.SetPoints(bar.maxPoints);
}


        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;

        damagable = GetComponent<Damagable>();
        damagable.onDeath.AddListener(OnDeath);
        damagable.damagableHit.AddListener(OnHit);

        // Ensure alive at start
        anim.SetBool("isAlive", true);
        anim.SetBool("canMove", true);
        
    }
   public void ForceDisableAttack()
{
    var atk = GetComponentInChildren<Attack>();
    if (atk != null)
        atk.DisableAttackHitbox();
}

public void DealMeleeDamage(Damagable target)
{
    target.Hit(meleeDamage, Vector2.zero);

    // Gain strike on successful melee hit
    StrikeBarHUD.GetPlayerBar()?.AddPoints(strikeGainOnMelee);
}

    void Update()
    {
        // If dead, freeze logic.
        if (isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        isRunning = Keyboard.current.leftShiftKey.isPressed;

        CheckGroundState();
        CheckSlope();
        HandleJumpState();
        UpdateAnimator();
        FlipCharacter();
    }
public void ResolveMeleeHit(Damagable victim, Vector2 knockback)
{
    if (victim == null || !victim.IsAlive) return;

    bool hitApplied = victim.Hit(meleeDamage, knockback);

    if (hitApplied)
    {
        StrikeBarHUD.GetPlayerBar()?.AddPoints(strikeGainOnMelee);
        Debug.Log($"🗡️ Melee hit → Strike +{strikeGainOnMelee}");
    }
}
// === MELEE HITBOX PROXY ===
public void EnableAttackHitbox()
{
    var attack = GetComponentInChildren<Attack>();
    if (attack == null)
    {
        Debug.LogError("❌ EnableAttackHitbox: No Attack component found!");
        return;
    }

    attack.EnableAttackHitbox();
}

public void DisableAttackHitbox()
{
    var attack = GetComponentInChildren<Attack>();
    if (attack == null)
    {
        Debug.LogError("❌ DisableAttackHitbox: No Attack component found!");
        return;
    }

    attack.DisableAttackHitbox();
}

    void FixedUpdate()
    {
        if (isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        MoveCharacter();
    }

    // ----------------------------
    // MOVEMENT
    // ----------------------------
   void MoveCharacter()
{
    float speed = isRunning ? runSpeed : walkSpeed;

    // If grounded on a slope, move ALONG the slope
    if (isGrounded && onSlope && !isJumping)
    {
        Vector2 slopeDir = Vector2.Perpendicular(slopeNormal).normalized;

        // Ensure slope direction matches input direction
        if (slopeDir.x * moveInput.x < 0)
            slopeDir = -slopeDir;

        rb.linearVelocity = new Vector2(
            slopeDir.x * speed,
            slopeDir.y * speed
        );
    }
    else
    {
        // Normal flat movement or airborne
        rb.linearVelocity = new Vector2(
            moveInput.x * speed,
            rb.linearVelocity.y
        );
        if (isGrounded && onSlope && Mathf.Abs(moveInput.x) < 0.01f)
{
    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
}

    }
}


    // ----------------------------
    // JUMP INPUT
    // ----------------------------
    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (isDead) return;
        if (!ctx.started) return;

        // Force-update ground state because input arrives BEFORE Update()
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        if (isGrounded)
        {
            isJumping = true;
            isFalling = false;
            isLanding = false;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            anim.SetBool("isJumping", true);
        }
    }

    // ----------------------------
    // JUMP STATE MACHINE
    // ----------------------------
    void HandleJumpState()
    {
        if (!isGrounded && rb.linearVelocity.y < -0.1f)
        {
            isFalling = true;
            isJumping = false;
        }

        if (isGrounded && isFalling)
        {
            isLanding = true;
            isFalling = false;
        }
    }

    // ----------------------------
    // GROUND CHECK
    // ----------------------------
    void CheckGroundState()
    {
        bool wasGrounded = isGrounded;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            isLanding = true;
        }
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

    // ----------------------------
    // ANIMATOR
    // ----------------------------
    void UpdateAnimator()
    {
        float horizontal = Mathf.Abs(moveInput.x);

        bool walking = horizontal > 0.1f && !isRunning;
        bool running = horizontal > 0.1f && isRunning;

        anim.SetBool("isWalking", walking);
        anim.SetBool("isRunning", running);

        anim.SetBool("isJumping", isJumping);
        anim.SetBool("isFalling", isFalling);
        anim.SetBool("isLanding", isLanding);

        anim.SetFloat("yVelocity", rb.linearVelocity.y);

        if (isLanding)
            Invoke(nameof(ResetLanding), 0.1f);
    }

    void ResetLanding()
    {
        isLanding = false;
        anim.SetBool("isLanding", false);
    }

    // ----------------------------
    // SAFE FLIP (never changes size)
    // ----------------------------
    void FlipCharacter()
    {
        if (moveInput.x > 0)
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        else if (moveInput.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
    }

    // ----------------------------
    // PLAYER INPUT
    // ----------------------------
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (isDead) return;
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (isDead) return;
        if (!ctx.performed) return;

        if (Time.time < lastMeleeTime + meleeCooldown)
            return;

        lastMeleeTime = Time.time;
        anim.SetTrigger("AttackLight");
    }

public void OnRangedAttack(InputAction.CallbackContext ctx)
{
    if (isDead) return;
    if (!ctx.performed) return;

    if (Time.time < lastRangedTime + rangedCooldown)
        return;

    lastRangedTime = Time.time;
    anim.SetTrigger("rangedAttack");
}



    // Called by animation event
   public void FireProjectile()
{
    Debug.Log("🔥 FireProjectile() CALLED");

    if (isDead) return;

    var bar = StrikeBarHUD.GetPlayerBar();
    Debug.Log($"🔥 StrikeBar found: {bar != null}");

    if (bar == null) return;

    // ❌ Not enough Strike → cancel projectile
    if (bar.currentPoints < strikeCostOnRanged)
    {
        Debug.Log("[Strike] Not enough Strike to fire ranged");
        return;
    }

    // ✅ Spend Strike NOW (actual attack moment)
    Debug.Log($"🔥 strikeCostOnRanged = {strikeCostOnRanged}");

    bar.AddPoints(-strikeCostOnRanged);

    float facing = Mathf.Sign(transform.localScale.x);
    Vector2 dir = new Vector2(facing, 0);

    GameObject proj = Instantiate(
        projectilePrefab,
        projectileSpawnPoint.position,
        Quaternion.identity
    );

    var up = proj.GetComponent<UniversalProjectile>();
    if (up == null)
    {
        Debug.LogError("❌ Projectile missing UniversalProjectile");
        Destroy(proj);
        return;
    }

    up.Launch(
        dir,
        overrideSpeed: -1f,
        overrideDamage: rangedDamage,
        owner: gameObject
    );
}


    // Called from Damagable.damagableHit
   public void OnHit(int damage, Vector2 knockback)
{
    if (isDead) return;

    // 🔥 Cultists do NOT interrupt attacks
    // Only play hit animation if NOT attacking
    if (!anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
    {
        anim.ResetTrigger("hit");
        anim.SetTrigger("hit");
    }

    rb.linearVelocity = new Vector2(
        rb.linearVelocity.x,                 // ❗ DO NOT launch upward
        Mathf.Max(rb.linearVelocity.y, 0f)   // prevent extra airtime
    );
}

    // Called from Damagable.onDeath

  public void OnDeath()
{
    anim.SetBool("isAlive", false);
    anim.SetBool("canMove", false);

    anim.SetTrigger("DeathTrigger");

    rb.linearVelocity = Vector2.zero;

    // ⭐ NEW — show death screen
    var deathMenu = FindFirstObjectByType<DeathMenuManager>();
    if (deathMenu != null)
        deathMenu.ShowDeathMenu();
}


}
