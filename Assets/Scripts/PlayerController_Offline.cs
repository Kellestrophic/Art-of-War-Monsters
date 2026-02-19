using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(TouchingDirections))]
[RequireComponent(typeof(Damagable))]
public class PlayerController_Offline : MonoBehaviour
{
    [Header("Move")]
    public float walkSpeed = 5f;
    public float runSpeed  = 8f;
    public float jumpImpulse = 10f;

    [Header("Ranged")]
    public float rangedCooldown = 1.5f;

    // runtime
    private Vector2 moveInput;
    private float timeSinceLastRanged;
    private bool isRangedReady = true;

    // refs
    private Rigidbody2D rb;
    private Animator animator;
    private TouchingDirections touching;
    private Damagable damagable;

    // computed
    public float CurrentMoveSpeed
    {
        get
        {
            if (canMove)
            {
                if (IsMoving && !touching.IsOnWall)
                    return IsRunning ? runSpeed : walkSpeed;
                return 0f;
            }
            return 0f;
        }
    }

    [SerializeField] private bool _isMoving;
    public bool IsMoving
    {
        get => _isMoving;
        private set
        {
            _isMoving = value;
            animator.SetBool(AnimationStrings.isMoving, value);
        }
    }

    [SerializeField] private bool _isRunning;
    private bool IsRunning
    {
        get => _isRunning;
        set
        {
            _isRunning = value;
            animator.SetBool(AnimationStrings.isRunning, value);
        }
    }

    [SerializeField] private bool _isFacingRight = true;
    public bool IsFacingRight
    {
        get => _isFacingRight;
        private set
        {
            if (_isFacingRight != value) transform.localScale *= new Vector2(-1, 1);
            _isFacingRight = value;
        }
    }

    public bool canMove => animator.GetBool(AnimationStrings.canMove);
    public bool IsAlive => animator.GetBool(AnimationStrings.isAlive);

    public bool LockVelocity
    {
        get => animator.GetBool(AnimationStrings.lockVelocity);
        set => animator.SetBool(AnimationStrings.lockVelocity, value);
    }

    private void Awake()
    {
        rb        = GetComponent<Rigidbody2D>();
        animator  = GetComponent<Animator>();
        touching  = GetComponent<TouchingDirections>();
        damagable = GetComponent<Damagable>();
        damagable.onDeath.AddListener(OnDeath);


        // Ensure animator "gates" are open for offline by default
        animator.SetBool(AnimationStrings.canMove, true);
        animator.SetBool(AnimationStrings.isAlive, true);

        if (damagable != null)
            damagable.damagableHit.AddListener(OnHit);
    }

    private void OnDestroy()
    {
        if (damagable != null)
            damagable.damagableHit.RemoveListener(OnHit);
    }
public void OnDeath()
{
    // Trigger death animation
    animator.SetBool(AnimationStrings.isAlive, false);

    // Disable movement
    animator.SetBool(AnimationStrings.canMove, false);

    // Show death menu
    FindObjectOfType<DeathMenuManager>().ShowDeathMenu();
}

   private void Update()
{
    timeSinceLastRanged += Time.deltaTime;
    if (!isRangedReady && timeSinceLastRanged >= rangedCooldown)
        isRangedReady = true;

    // ⭐ SAFETY FIX: Unlock movement if stuck but grounded.
    if (!canMove && touching.IsGrounded)
    {
        var state = animator.GetCurrentAnimatorStateInfo(0);

        if (!state.IsTag("Attack") && !state.IsTag("Hurt"))
        {
            animator.SetBool(AnimationStrings.canMove, true);
        }
    }
}


    private void FixedUpdate()
    {
        if (LockVelocity) return;

        // Horizontal motion
        Vector2 v = rb.linearVelocity;   // use your project's linearVelocity convenience
        v.x = moveInput.x * CurrentMoveSpeed;
        rb.linearVelocity = v;

        // Vertical anim params
        animator.SetFloat(AnimationStrings.yVelocity, rb.linearVelocity.y);
        animator.SetBool(AnimationStrings.isFalling, rb.linearVelocity.y < -0.15f);
    }

    // ── Input (Unity Events → drag these in PlayerInput) ────────────────────────

    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!IsAlive) { IsMoving = false; return; }

        moveInput = ctx.ReadValue<Vector2>();
        IsMoving = moveInput != Vector2.zero;

        if (moveInput.x > 0 && !IsFacingRight)      IsFacingRight = true;
        else if (moveInput.x < 0 && IsFacingRight)  IsFacingRight = false;
    }

    public void OnRun(InputAction.CallbackContext ctx)
    {
        if (ctx.started)       IsRunning = true;
        else if (ctx.canceled) IsRunning = false;
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        if (!touching.IsGrounded) return;

        rb.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
        animator.SetTrigger(AnimationStrings.jumpTrigger);
    }

    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        animator.SetTrigger(AnimationStrings.attackTrigger);
    }

    public void OnRangedAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        if (!isRangedReady) return;

        animator.SetTrigger(AnimationStrings.rangedAttackTrigger);
        timeSinceLastRanged = 0f;
        isRangedReady = false;
    }

    // ── Damage callback from Damagable ─────────────────────────────────────────
    public void OnHit(int damage, Vector2 knockback)
    {
        // Apply knockback
        rb.linearVelocity = new Vector2(knockback.x, rb.linearVelocityY + knockback.y);

        // Optional: if you have a hurt trigger name, fire it here
        animator.SetTrigger(AnimationStrings.hitTrigger);
    }
}
