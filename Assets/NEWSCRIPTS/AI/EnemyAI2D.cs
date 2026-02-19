using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyAI2D : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack, Recover, Dead }

    #region Inspector

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform frontCheck;
    [SerializeField] private Transform meleePoint;
    [SerializeField] private Transform shootOrigin;
    [SerializeField] private Transform eye; // optional, can reuse frontCheck

    [Header("Layers & Masks")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundMask;   // floors/one-way/walkable
    [SerializeField] private LayerMask obstacleMask; // walls/solids
    [SerializeField] private LayerMask losBlockMask; // line-of-sight blockers

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float patrolSpeed = 2.0f;
    [SerializeField] private float jumpVelocity = 12f;
    [SerializeField] private bool patrolOnStart = true;
    [SerializeField] private float groundCheckRadius = 0.18f;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private float edgeCheckDistance = 0.6f;
    [SerializeField] private float faceFlipThreshold = 0.01f;

    [Header("Detection")]
    [SerializeField] private float chaseRange = 12f;
    [SerializeField] private float shootRange = 8f;
    [SerializeField] private float meleeRange = 1.2f;
    [SerializeField] private float verticalChaseBias = 2.0f; // jump if player higher
    [SerializeField] private float fovDegrees = 140f;        // 180 = full half-circle

    [Header("Combat - Melee")]
    [SerializeField] private int meleeDamage = 10;
    [SerializeField] private float meleeRadius = 0.9f;
    [SerializeField] private float meleeWindup = 0.18f;
    [SerializeField] private float meleeCooldown = 0.8f;

    [Header("Combat - Melee Knockback")]
    [SerializeField] private float meleeKnockbackX = 6f;
    [SerializeField] private float meleeKnockbackY = 4f;

    [Header("Combat - Ranged")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private float fireCooldown = 1.5f; // fallback if range not used

    [Header("Patrol/Chase Polish")]
    [SerializeField] private float stuckFlipDelay = 0.45f;
    [SerializeField] private float minMoveForStuck = 0.05f;
    [SerializeField] private float wallFlipCooldown = 0.25f;

    [Header("Chase Tuning (player-like)")]
    [SerializeField] private float preferredRange = 4.5f;
    [SerializeField] private float backoffRange = 1.2f;
    [SerializeField] private float strafeJitter = 0.6f;
    [SerializeField] private float strafeSpeed = 1.1f;

    [Header("State (debug)")]
    [SerializeField] private State state = State.Idle;

    // --- Smart probes that always align with facing ---
    [Header("Probes (auto-facing)")]
    [SerializeField] private float probeX = 0.45f;  // how far ahead to probe (horizontal)
    [SerializeField] private float probeY = 0.10f;  // slight vertical lift for the probe

    // --- NEW: Behavior Randomization ---
    [Header("Randomization")]
    [Tooltip("Random cooldown range between shots (overrides the fixed 'fireCooldown').")]
    [SerializeField] private Vector2 fireCooldownRange = new Vector2(1.0f, 2.2f);

    [Tooltip("Min/Max seconds between jumps (cooldown gate so it can’t hop every second).")]
    [SerializeField] private Vector2 jumpCooldownRange = new Vector2(0.9f, 2.4f);

    [Tooltip("Base chance to jump when conditions suggest it (wall/ledge/player higher).")]
    [Range(0f, 1f)] [SerializeField] private float jumpBaseChance = 0.45f;

    [Tooltip("How fast the perlin noise wiggles jump chance.")]
    [SerializeField] private float perlinJumpSpeed = 0.7f;

    [Tooltip("How strongly perlin noise affects the base chance (0 = none, 1 = big swings).")]
    [Range(0f, 1f)] [SerializeField] private float perlinJumpWeight = 0.5f;

    [Tooltip("Extra chance if the player is notably higher than the AI.")]
    [Range(0f, 1f)] [SerializeField] private float higherJumpBonus = 0.25f;

    [Tooltip("Occasional brief idle pauses during patrol to feel less robotic.")]
    [SerializeField] private Vector2 microPauseRange = new Vector2(0.25f, 0.8f);
    [Range(0f, 1f)] [SerializeField] private float microPauseChance = 0.12f;

    #endregion

    // runtime
    private Transform player;
    private bool grounded;
    private bool atWall;
    private bool edgeAhead;
    private bool isFacingRight = true;
    private bool attacking;
    private float nextShootTime;
    private float nextMeleeTime;

    // polish runtime
    private float stuckClock;
    private float lastFlipTime;
    private float jitterSeed;
    private float microPauseUntil;      // for patrol micro-pauses
    private float nextAllowedJumpTime;  // jump cooldown gate

    // Animator safety wiring
    private const string SHOOT_TRIGGER = "ShootTrigger";
    private const string MELEE_TRIGGER = "MeleeTrigger";
    private static readonly int FIRE_STATE = Animator.StringToHash("FireProjectile");
    private bool hasShootTrigger;
    private bool hasMeleeTrigger;
    private bool hasFireState;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!anim) anim = GetComponent<Animator>();
        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (!eye) eye = frontCheck;

        InitAnimatorSafety();

        var dmg = GetComponent<Damagable>();
        if (dmg) dmg.isEnemy = true; // hard-assert: AI are enemies

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        jitterSeed = Random.value * 1000f;
        state = patrolOnStart ? State.Patrol : State.Idle;

        StartCoroutine(CoInitialUnstick());
    }

    private IEnumerator CoInitialUnstick()
    {
        yield return new WaitForFixedUpdate();
        EvaluateProbes();
        if (atWall || !edgeAhead)
        {
            Flip(!isFacingRight);
            lastFlipTime = Time.time;
        }
    }

    private void InitAnimatorSafety()
    {
        if (!anim) return;
        hasShootTrigger = HasParam(anim, SHOOT_TRIGGER, AnimatorControllerParameterType.Trigger);
        hasMeleeTrigger = HasParam(anim, MELEE_TRIGGER, AnimatorControllerParameterType.Trigger);
        hasFireState = anim.HasState(0, FIRE_STATE);
    }

    private static bool HasParam(Animator a, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in a.parameters)
            if (p.type == type && p.name == name) return true;
        return false;
    }

    private void PlayShootAnimation()
    {
        if (!anim) return;

        if (hasShootTrigger) { anim.ResetTrigger(SHOOT_TRIGGER); anim.SetTrigger(SHOOT_TRIGGER); }
        else if (hasFireState) { anim.CrossFade(FIRE_STATE, 0.05f, 0, 0f); }
    }

    // ——— PROBES ———
    private void EvaluateProbes()
    {
        if (groundCheck)
            grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
        else
            grounded = Physics2D.OverlapCircle(transform.position, groundCheckRadius, groundMask);

        float sign = isFacingRight ? 1f : -1f;
        Vector2 fwd = new Vector2(sign, 0f);

        Vector2 root = transform.position;
        Vector2 ahead = root + new Vector2(sign * probeX, probeY);

        atWall = Physics2D.Raycast(ahead, fwd, wallCheckDistance, obstacleMask);
        RaycastHit2D groundHit = Physics2D.Raycast(ahead, Vector2.down, edgeCheckDistance, groundMask);
        edgeAhead = groundHit.collider != null; // false => drop ahead
    }

    private void Update()
    {
        if (state == State.Dead) return;

        if (!player)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        EvaluateProbes();

        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool("IsGrounded", grounded);
        anim.SetBool("IsAttacking", attacking);

        switch (state)
        {
            case State.Idle:   IdleUpdate();   break;
            case State.Patrol: PatrolUpdate(); break;
            case State.Chase:  ChaseUpdate();  break;
            case State.Attack: break;
            case State.Recover: break;
            case State.Dead:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                break;
        }

        if (Mathf.Abs(rb.linearVelocity.x) > faceFlipThreshold)
        {
            if (rb.linearVelocity.x > 0 && !isFacingRight) Flip(true);
            if (rb.linearVelocity.x < 0 && isFacingRight)  Flip(false);
        }

        if (grounded && Mathf.Abs(rb.linearVelocity.x) < minMoveForStuck)
        {
            stuckClock += Time.deltaTime;
            if (stuckClock > stuckFlipDelay && Time.time - lastFlipTime > wallFlipCooldown)
            {
                Flip(!isFacingRight);
                stuckClock = 0f;
            }
        }
        else stuckClock = 0f;
    }

    private void IdleUpdate()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (CanSeeOrSensePlayer(out _))      state = State.Chase;
        else if (patrolOnStart)              state = State.Patrol;
    }

    private void PatrolUpdate()
    {
        // Micro-pause to feel less robotic
        if (Time.time < microPauseUntil)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        else if (grounded && Random.value < microPauseChance * Time.deltaTime * 5f) // low probability per frame
        {
            microPauseUntil = Time.time + Random.Range(microPauseRange.x, microPauseRange.y);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (!grounded) { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); return; }

        if ((atWall || !edgeAhead) && Time.time - lastFlipTime > wallFlipCooldown)
        {
            Flip(!isFacingRight);
        }

        float targetSpeed = patrolSpeed * (isFacingRight ? 1f : -1f);
        MoveHoriz(targetSpeed);

        if (CanSeeOrSensePlayer(out _)) state = State.Chase;
    }

    private void ChaseUpdate()
    {
        if (!player) { state = patrolOnStart ? State.Patrol : State.Idle; return; }

        float dist = Vector2.Distance(player.position, transform.position);
        float dx = player.position.x - transform.position.x;
        float dirX = Mathf.Sign(dx);

        // Face the target
        if (dirX > 0 && !isFacingRight) Flip(true);
        if (dirX < 0 && isFacingRight)  Flip(false);

        // Choose attack if possible
        if (!attacking)
        {
            if (dist <= meleeRange && Time.time >= nextMeleeTime)
            { StartCoroutine(CoMelee()); return; }

            if (dist <= shootRange && HasLineOfSight() && Time.time >= nextShootTime)
            { StartCoroutine(CoShoot()); return; }
        }

        // Movement: approach/backoff + jitter
        float move = 0f;

        if (dist > preferredRange * 1.05f)
        {
            move = moveSpeed * dirX;
        }
        else if (dist < backoffRange)
        {
            move = moveSpeed * -dirX * 0.8f;
        }
        else
        {
            float wiggle = (Mathf.PerlinNoise(Time.time * strafeSpeed, jitterSeed) - 0.5f) * 2f;
            move = patrolSpeed * strafeJitter * wiggle;
        }

        // Jump decision (randomized & context-biased)
        bool playerHigher = player.position.y - transform.position.y > verticalChaseBias;
        bool obstacleAhead = atWall || (!edgeAhead && (dirX > 0) == isFacingRight);

        if (ShouldJump(dist, obstacleAhead, playerHigher))
        {
            Jump();
        }

        MoveHoriz(move);

        // Lose target
        if (dist > chaseRange * 1.6f && !HasLineOfSight())
            state = patrolOnStart ? State.Patrol : State.Idle;
    }

    private void MoveHoriz(float speedX)
    {
        rb.linearVelocity = new Vector2(speedX, rb.linearVelocity.y);
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
    }

    // ——— DECISION HELPERS ———

    private float NextFireCooldown()
    {
        if (fireCooldownRange.x > 0f || fireCooldownRange.y > 0f)
            return Mathf.Max(0.05f, Random.Range(fireCooldownRange.x, fireCooldownRange.y));
        return fireCooldown;
    }

    private bool ShouldJump(float distanceToPlayer, bool obstacleAhead, bool playerHigher)
    {
        if (!grounded) return false;
        if (Time.time < nextAllowedJumpTime) return false;

        float noise = Mathf.PerlinNoise(Time.time * perlinJumpSpeed, jitterSeed);
        float perlinMod = Mathf.Lerp(1f - perlinJumpWeight, 1f + perlinJumpWeight, noise);

        float chance = jumpBaseChance * perlinMod;
        if (playerHigher) chance += higherJumpBonus;
        chance = Mathf.Clamp01(chance);

        bool roll = Random.value < chance;

        // Prefer jumping when clearly blocked, but still honor cooldown
        if (obstacleAhead || roll)
        {
            nextAllowedJumpTime = Time.time + Random.Range(jumpCooldownRange.x, jumpCooldownRange.y);
            return true;
        }

        return false;
    }
// Team check that never depends on Tags
private static bool IsEnemyTeam(Transform t)
{
    if (!t) return false;
    var d = t.GetComponentInParent<Damagable>();
    return d != null && d.isEnemy;
}

    private bool CanSeeOrSensePlayer(out float distance)
    {
        distance = Mathf.Infinity;
        if (!player) return false;

        Vector2 toPlayer = player.position - transform.position;
        distance = toPlayer.magnitude;
        if (distance > chaseRange) return false;

        Vector2 fwd = isFacingRight ? Vector2.right : Vector2.left;
        float angle = Vector2.Angle(fwd, toPlayer.normalized);
        if (angle > (fovDegrees * 0.5f)) return false;

        return HasLineOfSight();
    }

    private bool HasLineOfSight()
    {
        if (!player) return false;

        Vector2 origin = eye ? (Vector2)eye.position : (Vector2)transform.position;
        Vector2 dir = ((Vector2)player.position - origin).normalized;
        float dist = Vector2.Distance(origin, player.position);

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, losBlockMask);
        return hit.collider == null;
    }

    // ... everything above stays the same ...

    private IEnumerator CoMelee()
    {
        attacking = true;
        state = State.Attack;
        nextMeleeTime = Time.time + meleeCooldown;

        if (hasShootTrigger) anim.ResetTrigger(SHOOT_TRIGGER);
        if (hasMeleeTrigger) anim.SetTrigger(MELEE_TRIGGER);

        yield return new WaitForSeconds(meleeWindup);
Collider2D[] hits = Physics2D.OverlapCircleAll(meleePoint.position, meleeRadius);
foreach (var h in hits)
{
    var target = h.GetComponentInParent<Damagable>();
    if (target == null || !target.IsAlive) continue;

    // Enemy AI only damages non-enemy (the player)
    if (IsEnemyTeam(target.transform)) continue;

    Vector2 dir = ((Vector2)h.transform.position - (Vector2)meleePoint.position).normalized;
    Vector2 knock = new Vector2(dir.x * meleeKnockbackX, meleeKnockbackY);
    target.Hit(meleeDamage, knock);
}



        yield return new WaitForSeconds(0.1f);

        attacking = false;
        state = State.Chase;
    }

// ... everything below stays the same ...

    private IEnumerator CoShoot()
    {
        attacking = true;
        state = State.Attack;
        nextShootTime = Time.time + NextFireCooldown(); // randomized cadence

        if (anim) PlayShootAnimation();

        yield return new WaitForSeconds(0.1f);

        Vector2 origin = shootOrigin ? (Vector2)shootOrigin.position : (Vector2)transform.position;
        Vector2 dir = (sr != null && sr.flipX) ? Vector2.left : Vector2.right;

        if (projectilePrefab == null)
        {
            Debug.LogError("[CoShoot] ❌ projectilePrefab is NULL");
            goto EndShoot;
        }

        GameObject go = Instantiate(projectilePrefab, origin, Quaternion.identity);
        if (!go)
        {
            Debug.LogError("[CoShoot] ❌ Instantiate returned NULL");
            goto EndShoot;
        }

        // Make sure it's visible above the enemy
        float z = (sr != null) ? sr.transform.position.z : 0f;
        go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, z);

        var psr = go.GetComponentInChildren<SpriteRenderer>();
        if (psr)
        {
            if (sr)
            {
                psr.sortingLayerID = sr.sortingLayerID;
                psr.sortingOrder   = sr.sortingOrder + 1;
            }
            psr.flipX = (dir.x < 0f);
            psr.enabled = true;
            var c = psr.color; c.a = 1f; psr.color = c;
        }

        var uni = go.GetComponent<UniversalProjectile>();
        if (uni != null)
        {
            uni.Launch(direction: dir, overrideSpeed: projectileSpeed, overrideDamage: projectileDamage, owner: gameObject);
        }
        else
        {
            var prb = go.GetComponent<Rigidbody2D>();
            if (prb) prb.linearVelocity = dir * projectileSpeed;
            Debug.LogWarning("[CoShoot] ⚠️ No UniversalProjectile on projectile prefab. Using RB velocity fallback.");
        }

    EndShoot:
        yield return new WaitForSeconds(0.05f);
        attacking = false;
        state = State.Chase;
    }

    public void Kill()
    {
        if (state == State.Dead) return;
        OnDied();
    }

    private void OnDied()
    {
        if (state == State.Dead) return;

        StopAllCoroutines();
        attacking = false;
        state = State.Dead;

        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        }

        if (anim) anim.SetTrigger("DieTrigger");

        var cols = GetComponentsInChildren<Collider2D>();
        foreach (var c in cols)
        {
            if (c == GetComponent<Collider2D>()) { c.isTrigger = true; continue; }
            c.enabled = false;
        }

        StartCoroutine(CoDisableAfter(1.0f));
    }

    private IEnumerator CoDisableAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        enabled = false;
    }

    private void Flip(bool faceRight)
    {
        isFacingRight = faceRight;
        lastFlipTime = Time.time;
        if (sr) sr.flipX = !faceRight;
        else transform.localScale = new Vector3(faceRight ? 1 : -1, 1, 1);
    }

    private void OnDrawGizmosSelected()
    {
        if (meleePoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(meleePoint.position, meleeRadius);
        }
        if (shootOrigin)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(shootOrigin.position, 0.08f);
        }
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Visualize smart probes
        Gizmos.color = Color.cyan;
        float sign = Application.isPlaying ? (isFacingRight ? 1f : -1f) : 1f;
        Vector3 ahead = transform.position + new Vector3(sign * probeX, probeY, 0f);
        Vector3 fwd = new Vector3(sign, 0f, 0f);
        Gizmos.DrawLine(ahead, ahead + fwd * wallCheckDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(ahead, ahead + Vector3.down * edgeCheckDistance);

        Gizmos.color = new Color(1, 1, 1, 0.25f);
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = new Color(1, 1, 0, 0.25f);
        Gizmos.DrawWireSphere(transform.position, shootRange);
        Gizmos.color = new Color(1, 0, 0, 0.25f);
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
