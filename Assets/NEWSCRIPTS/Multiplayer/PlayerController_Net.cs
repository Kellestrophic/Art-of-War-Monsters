using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(TouchingDirections))]
public class PlayerController_Net : NetworkBehaviour
{
    [Header("Move")]
    public float walkSpeed = 5f;
    public float runSpeed  = 8f;
    public float jumpImpulse = 10f;

    [Header("Ranged")]
    public float rangedCooldown = 1.5f;

    private Vector2 moveInput;
    private float timeSinceLastRanged;
    private bool isRangedReady = true;

    private Rigidbody2D rb;
    private Animator animator;
    private TouchingDirections touching;
    private DamageableNet hpNet;
    

    [SerializeField] private AnimatorSyncLite animSync;

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
        private set { _isMoving = value; if (IsOwner) animator.SetBool(AnimationStrings.isMoving, value); }
    }

    [SerializeField] private bool _isRunning;
    private bool IsRunning
    {
        get => _isRunning;
        set { _isRunning = value; if (IsOwner) animator.SetBool(AnimationStrings.isRunning, value); }
    }

    public bool _IsFacingRight = true;
    public bool IsFacingRight
    {
        get => _IsFacingRight;
        private set
        {
            if (_IsFacingRight != value) transform.localScale *= new Vector2(-1, 1);
            _IsFacingRight = value;
        }
    }

    public bool canMove => animator.GetBool(AnimationStrings.canMove);
    public bool IsAlive => animator.GetBool(AnimationStrings.isAlive);

    public bool LockVelocity
    {
        get => animator.GetBool(AnimationStrings.lockVelocity);
        set { if (IsOwner) animator.SetBool(AnimationStrings.lockVelocity, value); }
    }

    private void Awake()
    {
        rb       = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        touching = GetComponent<TouchingDirections>();
        if (!animSync) animSync = GetComponentInChildren<AnimatorSyncLite>(true);

        hpNet = GetComponent<DamageableNet>(); // optional, but recommended
        if (hpNet != null)
        {
            hpNet.OnDamagedLocal += (_, __) => { /* local SFX/VFX if wanted */ };
            hpNet.OnDiedLocal    += () =>
            {
                if (IsOwner && TryGetComponent<PlayerInput>(out var pi)) pi.enabled = false;
            };
        }
    }

    public override void OnNetworkSpawn()
    {
        if (TryGetComponent<PlayerInput>(out var pi)) pi.enabled = IsOwner;
    }

    private void Update()
    {
        if (!IsOwner) return;
        timeSinceLastRanged += Time.deltaTime;
        if (!isRangedReady && timeSinceLastRanged >= rangedCooldown) isRangedReady = true;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (!LockVelocity)
        {
            Vector2 v = rb.linearVelocity;
            v.x = moveInput.x * CurrentMoveSpeed;
            rb.linearVelocity = v;
        }

        animator.SetFloat(AnimationStrings.yVelocity, rb.linearVelocity.y);
        animator.SetBool(AnimationStrings.isFalling, rb.linearVelocity.y < -0.15f);
    }

    // INPUTS (owner-only)
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!IsOwner) return;

        if (IsAlive)
        {
            moveInput = ctx.ReadValue<Vector2>();
            IsMoving = moveInput != Vector2.zero;
            if (moveInput.x > 0 && !IsFacingRight) IsFacingRight = true;
            else if (moveInput.x < 0 && IsFacingRight) IsFacingRight = false;
        }
        else IsMoving = false;
    }

    public void OnRun(InputAction.CallbackContext ctx)
    {
        if (!IsOwner) return;
        if (ctx.started) IsRunning = true;
        else if (ctx.canceled) IsRunning = false;
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!IsOwner) return;
        if (ctx.started && touching.IsGrounded)
        {
            rb.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
            animSync?.RaiseJump();
        }
    }

    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (!IsOwner) return;
        if (ctx.started) animSync?.RaiseAttack();
    }

    public void OnRangedAttack(InputAction.CallbackContext ctx)
    {
        if (!IsOwner) return;
        if (ctx.started && isRangedReady)
        {
            animSync?.RaiseRanged();
            timeSinceLastRanged = 0f;
            isRangedReady = false;
        }
    }

    // Owner knockback (called by DamageableNet via HurtClientRpc → OnHit)
    public void OnHit(int damage, Vector2 knockback)
    {
        if (!IsOwner) return;
        rb.linearVelocity = new Vector2(knockback.x, rb.linearVelocityY + knockback.y);
        animSync?.RaiseHurt();
    }
}
