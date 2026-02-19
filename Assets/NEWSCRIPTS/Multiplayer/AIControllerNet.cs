
using UnityEngine;
using Unity.Netcode;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class AIControllerNet : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float stopDistance = 1.2f;
    [SerializeField] float ledgeCheckDistance = 0.8f;
    [SerializeField] float jumpCooldown = 0.40f;
    [SerializeField] float stuckJumpTime = 0.45f;
    [SerializeField] float climbIfTargetAbove = 0.75f;   // jump if target this much higher

    [Header("Ground & Obstacles")]
    [SerializeField] Transform groundCheck;              // place at feet (local Y ≈ -0.6)
    [SerializeField] float groundCheckRadius = 0.12f;    // MUST be positive
    [SerializeField] LayerMask groundMask;               // floors/platforms only
    [SerializeField] LayerMask obstacleMask;             // solid walls/geometry
    [SerializeField] LayerMask losBlockMask;             // blocks ranged LOS
    [SerializeField] float wallCheckDistance = 0.45f;    // chest ray
    [SerializeField] float wallCheckHeight = 0.30f;
    [SerializeField] float kneeProbeOffset = 0.20f;      // knee circle forward
    [SerializeField] float kneeProbeRadius = 0.15f;

    [Header("Combat")]
    [SerializeField] float meleeRange = 1.8f;
    [SerializeField] float meleeCooldown = 0.80f;
    [SerializeField] float rangedRange = 6.0f;
    [SerializeField] float rangedCooldown = 1.50f;       // you asked for 1.5s
    [SerializeField] MonoBehaviour meleeComponent;        // drag MeleeAttack (optional)
    [SerializeField] string meleeMethod = "TrySwing";
    [SerializeField] MonoBehaviour rangedComponent;       // drag ProjectileLauncher (optional)
    [SerializeField] string rangedMethod = "Fire";

    [Header("Animator (your parameter names)")]
    [SerializeField] Animator animator;                   // auto-found if empty
    [SerializeField] string isMovingParam   = "isMoving";
    [SerializeField] string isRunningParam  = "isRunning";
    [SerializeField] string isGroundedParam = "isGrounded";
    [SerializeField] string isOnWallParam   = "isOnWall";
    [SerializeField] string isFallingParam  = "isFalling";
    [SerializeField] string yVelocityParam  = "yVelocity";
    [SerializeField] string jumpTrigger     = "jump";
    [SerializeField] string attackTrigger   = "attack";
    [SerializeField] string rangedTrigger   = "rangedAttack";

    [Header("Debug")]
    [SerializeField] bool debugLogs = false;

    Rigidbody2D rb;
    Transform target;
    float nextRepathTime;
    const float RepathInterval = 0.25f;

    Vector3 baseScale = Vector3.one;
    float nextJumpAllowedAt;
    float stuckSince;
    float nextMeleeAt, nextRangedAt;

    public override void OnNetworkSpawn()
    {
        // Simulate on server; if you ever want offline testing, flip to: if (NetworkManager.Singleton && !IsServer) { enabled = false; return; }
        if (!IsServer) { enabled = false; return; }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;

        if (!groundCheck)
        {
            var gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform);
            gc.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            groundCheck = gc.transform;
        }
        groundCheckRadius = Mathf.Abs(groundCheckRadius);

        if (!animator) animator = GetComponentInChildren<Animator>();

        // Auto-find typical child components if you don't wire them
        meleeComponent  = meleeComponent  ?? FindChildBehaviourByTypeNames("MeleeAttack", "MeleeAttackNet", "Melee");
        rangedComponent = rangedComponent ?? FindChildBehaviourByTypeNames("ProjectileLauncher", "BloodSlash", "RangedAttack");

        // Reasonable defaults
        if (obstacleMask == 0) obstacleMask = groundMask;
        if (losBlockMask == 0) losBlockMask = obstacleMask;
    }

    void FixedUpdate()
    {
        if (!IsServer) return;

        // Find target periodically
        if (Time.time >= nextRepathTime || !target)
        {
            nextRepathTime = Time.time + RepathInterval;
            target = FindHumanTarget();
            if (debugLogs && !target) Debug.Log("[AI] No target found");
        }
        if (!target) { DriveAnimatorIdle(); return; }

        // Desired horizontal velocity
        float dx = target.position.x - transform.position.x;
        float absDx = Mathf.Abs(dx);
        float desiredVX = (absDx > stopDistance) ? Mathf.Sign(dx) * moveSpeed : 0f;

        // Probes
        bool grounded = IsGrounded();

        bool wallAhead = false;
        if (grounded && Mathf.Abs(desiredVX) > 0.01f)
        {
            Vector2 dir = new Vector2(Mathf.Sign(desiredVX), 0f);

            // Chest ray against walls
            Vector2 chest = new Vector2(transform.position.x, transform.position.y + wallCheckHeight);
            wallAhead = Physics2D.Raycast(chest, dir, wallCheckDistance, obstacleMask);

            // Knee circle ahead (helps detect short posts)
            Vector2 knee = new Vector2(transform.position.x + dir.x * kneeProbeOffset, transform.position.y + kneeProbeOffset);
            bool kneeBlocked = Physics2D.OverlapCircle(knee, kneeProbeRadius, obstacleMask);

            // Ledge look-ahead
            Vector2 ahead = new Vector2(transform.position.x + dir.x * ledgeCheckDistance, transform.position.y);
            var groundAhead = Physics2D.Raycast(ahead, Vector2.down, 1.6f, groundMask);

            // Target significantly above?
            bool targetAbove = (target.position.y - transform.position.y) > climbIfTargetAbove;

            if (Time.time >= nextJumpAllowedAt &&
                (wallAhead || kneeBlocked || !groundAhead.collider || targetAbove || Stuck(desiredVX)))
            {
                DoJump();
            }
        }

        // Apply horizontal velocity (Unity 6 API)
        var v = GetVel();
        v.x = desiredVX;
        SetVel(v);

        // Flip without squashing
        if (desiredVX > 0.1f)  transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        if (desiredVX < -0.1f) transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);

        // Animator
        DriveAnimatorMove(grounded, wallAhead);

        // Attacks (melee preferred; ranged only at distance + LOS + cooldown)
        TryAttack(absDx);
    }

    // ---- Movement helpers ----
    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
    }

    bool Stuck(float desiredVX)
    {
        if (Mathf.Abs(desiredVX) < 0.01f) { stuckSince = 0f; return false; }
        if (Mathf.Abs(GetVel().x) < 0.05f)
        {
            if (stuckSince <= 0f) stuckSince = Time.time;
            return (Time.time - stuckSince) >= stuckJumpTime;
        }
        stuckSince = 0f;
        return false;
    }

    void DoJump()
    {
        var v = GetVel(); v.y = 0f; SetVel(v);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        nextJumpAllowedAt = Time.time + jumpCooldown;
        SetTrigger(jumpTrigger);
        if (debugLogs) Debug.Log("[AI] Jump");
    }

    // ---- Combat ----
    void TryAttack(float absDx)
    {
        // Melee first
        if (absDx <= meleeRange && meleeComponent && Time.time >= nextMeleeAt)
        {
            if (debugLogs) Debug.Log("[AI] Melee");
            meleeComponent.SendMessage(meleeMethod, SendMessageOptions.DontRequireReceiver);
            SetTrigger(attackTrigger);
            nextMeleeAt = Time.time + meleeCooldown;
            return;
        }

        // Then ranged (distance, LOS, cooldown)
        if (rangedComponent && absDx > (meleeRange + 0.2f) && absDx <= rangedRange &&
            Time.time >= nextRangedAt && HasLineOfSight())
        {
            if (debugLogs) Debug.Log("[AI] Ranged");
            rangedComponent.SendMessage(rangedMethod, SendMessageOptions.DontRequireReceiver);
            SetTrigger(rangedTrigger);
            nextRangedAt = Time.time + rangedCooldown;
        }
    }

    bool HasLineOfSight()
    {
        if (!target) return false;
        Vector2 src = new(transform.position.x, transform.position.y + wallCheckHeight);
        Vector2 dst = new(target.position.x,    target.position.y + wallCheckHeight);
        var dir = (dst - src).normalized;
        float dist = Vector2.Distance(src, dst);
        var hit = Physics2D.Raycast(src, dir, dist, losBlockMask);
        return !hit.collider;
    }

    Transform FindHumanTarget()
    {
        var allNOs = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        foreach (var no in allNOs)
        {
            if (!no.IsSpawned) continue;
            if (no.GetComponent<AIControllerNet>()) continue;   // skip bots
            if (!no.GetComponent<PlayerIdentityNet>()) continue;   // human marker
            return no.transform;
        }
        return null;
    }

    // ---- Animator drivers ----
    void DriveAnimatorIdle()
    {
        SetBool(isMovingParam, false);
        SetBool(isRunningParam, false);
        SetBool(isGroundedParam, false);
        SetBool(isOnWallParam, false);
        SetBool(isFallingParam, false);
        SetFloat(yVelocityParam, 0f);
    }

    void DriveAnimatorMove(bool grounded, bool wallAhead)
    {
        float absVx = Mathf.Abs(GetVel().x);
        bool moving  = absVx > 0.05f;
        bool running = absVx > moveSpeed * 0.6f;
        bool falling = !grounded && GetVel().y < -0.05f;

        SetBool(isMovingParam,   moving);
        SetBool(isRunningParam,  running);
        SetBool(isGroundedParam, grounded);
        SetBool(isOnWallParam,   wallAhead);
        SetBool(isFallingParam,  falling);
        SetFloat(yVelocityParam, GetVel().y);
    }

    void SetBool(string p, bool v)  { if (animator && !string.IsNullOrEmpty(p)) animator.SetBool(p, v); }
    void SetFloat(string p, float v){ if (animator && !string.IsNullOrEmpty(p)) animator.SetFloat(p, v); }
    void SetTrigger(string p)       { if (animator && !string.IsNullOrEmpty(p)) animator.SetTrigger(p); }

    // ---- Velocity wrappers (Unity 6 compatible) ----
#if UNITY_6000_0_OR_NEWER
    Vector2 GetVel() => rb.linearVelocity;
    void SetVel(Vector2 v) => rb.linearVelocity = v;
#else
    Vector2 GetVel() => rb.velocity;
    void SetVel(Vector2 v) => rb.velocity = v;
#endif

    // ---- Utilities ----
    MonoBehaviour FindChildBehaviourByTypeNames(params string[] names)
    {
        var all = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in all)
        {
            if (!mb) continue;
            var tname = mb.GetType().Name;
            for (int i = 0; i < names.Length; i++)
                if (tname == names[i]) return mb;
        }
        return null;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, Mathf.Abs(groundCheckRadius));
        }
        // chest rays
        Gizmos.color = Color.yellow;
        Vector3 chest = transform.position + new Vector3(0f, wallCheckHeight, 0f);
        Gizmos.DrawLine(chest, chest + Vector3.right * wallCheckDistance);
        Gizmos.DrawLine(chest, chest + Vector3.left  * wallCheckDistance);
        // knee circles
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position + new Vector3( kneeProbeOffset, kneeProbeOffset, 0), kneeProbeRadius);
        Gizmos.DrawWireSphere(transform.position + new Vector3(-kneeProbeOffset, kneeProbeOffset, 0), kneeProbeRadius);
        // ledge probes
        Gizmos.color = Color.cyan;
        Vector3 r = Vector3.right * ledgeCheckDistance;
        Gizmos.DrawLine(transform.position + r, transform.position + r + Vector3.down * 1.6f);
        Gizmos.DrawLine(transform.position - r, transform.position - r + Vector3.down * 1.6f);
    }
}
