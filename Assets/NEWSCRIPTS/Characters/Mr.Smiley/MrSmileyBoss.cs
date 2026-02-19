using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Damagable))]
public class MrSmileyBoss : MonoBehaviour
{
    // =====================================================
    // PHASES
    // =====================================================
    private enum BossPhase { Phase1, Phase2, Phase3, Phase4 }
    private BossPhase currentPhase = BossPhase.Phase1;
[SerializeField] private SpriteRenderer spriteRenderer;

    private bool spawned75;
    private bool spawned50;
    private bool spawned25;
private float lockedCastY;
    private bool isBusy;
    private bool canMove = true;
    private float facingDir = 1f;
    private Vector3 baseScale;
[SerializeField] private float normalCastCooldown = 6f;
[SerializeField] private float spinCooldownTime = 8f;
[Header("Cast Visuals")]
[SerializeField] private Animator castEffectAnimator;
private bool physicsSuspended = false;

private float nextSpinTime = 0f;
private float nextNormalCastTime = 0f;

    // =====================================================
    // REFERENCES
    // =====================================================
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private TouchingDirections touchingDirections;
    private Damagable damagable;

    // =====================================================
    // TELEPORT POINTS
    // =====================================================
    [Header("Teleport Points")]
    [SerializeField] private Transform groundSpawnPoint;
    [SerializeField] private Transform castSpawnPoint;
[Header("Normal Cast Scaling Per Phase")]
[SerializeField] private int spikesPhase1 = 2;
[SerializeField] private int spikesPhase2 = 4;
[SerializeField] private int spikesPhase3 = 6;
[SerializeField] private int spikesPhase4 = 8;

[SerializeField] private int handsPhase1 = 2;
[SerializeField] private int handsPhase2 = 3;
[SerializeField] private int handsPhase3 = 4;
[SerializeField] private int handsPhase4 = 6;

[Header("Cast Controllers")]
[SerializeField] private SpikeCastController spikeController;
[SerializeField] private ShadowHandController shadowHandController;

    // =====================================================
    // MOVEMENT
    // =====================================================
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;

    // =====================================================
    // ATTACK RANGES
    // =====================================================
    [Header("Ranges")]
    [SerializeField] private float meleeRange = 1.4f;
    [SerializeField] private float spinRange = 5f;
[Header("Casting Timing")]
[SerializeField] private float castWindupTime = 0.75f; // time to play cast animation BEFORE spawning

    // =====================================================
    // COOLDOWNS
    // =====================================================
    [Header("Cooldowns")]
    [SerializeField] private float meleeCooldown = 3f;
    [SerializeField] private float spinCooldown = 8f;
    [SerializeField] private float decisionDelay = 1.5f;

    private float nextDecisionTime;
[Header("Casting Cooldowns")]

[SerializeField] private float summonCastCooldown = 999f; // phase-locked

private bool canNormalCast = true;
private bool canSummonCast = true;

    // =====================================================
    // SPIN
    // =====================================================
    [Header("Spin")]
    [SerializeField] private float spinSpeedMultiplier = 1.2f;
    [SerializeField] private float spinDuration = 2f;

    // =====================================================
    // PHASE SUMMONING
    // =====================================================
    [Header("Phase Summons")]
    [Header("Enemy Spawn Pool")]
[SerializeField] private EnemySpawnEntry[] enemySpawnPool;

    [SerializeField] private Transform[] summonPoints;

    [Header("Summon Scaling Per Phase")]
    [SerializeField] private int summonsPhase2 = 3; // 75%
    [SerializeField] private int summonsPhase3 = 5; // 50%
    [SerializeField] private int summonsPhase4 = 7; // 25%

    [Header("Phase Cast Timing")]
    [SerializeField] private float phaseCastDuration = 4f;
private float nextCastTime = 0f;

    // =====================================================
    // HITBOXES
    // =====================================================
    [Header("Hitboxes")]
    [SerializeField] private BossHitbox meleeHitbox;
    [SerializeField] private BossHitbox spinHitbox;

    // =====================================================
    // UNITY
    // =====================================================

    private enum BossAction
{
    None,
    Melee,
    Spin,
    NormalCast,
    PhaseCast
}

private BossAction currentAction = BossAction.None;

private void Awake()
{
    rb = GetComponent<Rigidbody2D>();

    if (!spriteRenderer)
        spriteRenderer = GetComponent<SpriteRenderer>();

    // Find Damagable
    damagable = GetComponent<Damagable>();

    if (damagable == null)
    {
        Debug.LogError("[MrSmileyBoss] Damagable MUST be on same GameObject");
        isDead = true;
        canMove = false;
        StopAllCoroutines();

        if (phaseLockedSpikes != null)
            foreach (var spike in phaseLockedSpikes)
                spike?.EnableSpikes();

        return;
    }

    // ✅ Safe to call AFTER null check
    damagable.SetStatsKeyForTracking("MrSmiley");

    // ✅ Remove ONLY our listeners (not everyone’s)
    damagable.onDeath.RemoveListener(OnBossDeath);
    damagable.onDeath.AddListener(OnBossDeath);

    damagable.healthChanged.RemoveListener(OnHealthChanged);
    damagable.healthChanged.AddListener(OnHealthChanged);

    // Reset boss runtime state
    isDead = false;
    spawned75 = spawned50 = spawned25 = false;
    currentPhase = BossPhase.Phase1;
    currentAction = BossAction.None;

    canMove = true;
    canNormalCast = true;

    // Physics must be re-enabled (you disable it on death)
    rb.simulated = true;
    rb.constraints = RigidbodyConstraints2D.FreezeRotation;

    baseScale = transform.localScale;

    nextNormalCastTime = Time.time + normalCastCooldown;
    nextSpinTime = Time.time + spinCooldownTime;
}





[SerializeField] private GameObject endLevelPortalPrefab;
[SerializeField] private float deathFadeTime = 2.5f;

[Header("Arena Locks")]
[SerializeField] private ArenaSpikeZone[] phaseLockedSpikes;

private bool isDead = false;

private void OnBossDeath()
{
    
    if (isDead) return;
    isDead = true;

    Debug.Log("[MrSmileyBoss] OnDeath received");
    RuntimeStatsStore.Instance?.RecordBossKill("MrSmiley");


    // Match Cultist behavior
    rb.linearVelocity = Vector2.zero;
    rb.simulated = false;

    // Disable all hitboxes
    foreach (var hb in GetComponentsInChildren<BossHitbox>(true))
        hb.DisableHitbox();

    // Stop AI
    StopAllCoroutines();
    enabled = false;

    // Boss-only logic
    SpawnEndPortal();

    // Let Damagable own animation & timing
    Destroy(gameObject, 2.5f);
}

private bool _wired;

private void Start()
{
    ResetBossForRun();
}

private void OnEnable()
{
    // If your restart re-enables this object instead of recreating it
    ResetBossForRun();
}

private void ResetBossForRun()
{
    // Reset phase/action state
    isDead = false;
    spawned75 = spawned50 = spawned25 = false;
    currentPhase = BossPhase.Phase1;
    currentAction = BossAction.None;

    canMove = true;
    canNormalCast = true;
    physicsSuspended = false;

    // Reset rigidbody state
    if (rb != null)
    {
        rb.simulated = true;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.angularVelocity = 0f;
    }

    // Re-lock arena spikes at fight start
    if (phaseLockedSpikes != null)
        foreach (var spike in phaseLockedSpikes)
            spike?.EnableSpikes();

    // Wire events ONCE (don’t nuke everyone else’s listeners)
    if (damagable != null && !_wired)
    {
        damagable.onDeath.AddListener(OnBossDeath);
        damagable.healthChanged.AddListener(OnHealthChanged);
        _wired = true;
    }

    // 🔥 IMPORTANT: make sure Damagable can be damaged again
    // If your Damagable has invuln / alive flags, you must reset them here.
    // (See note below)
}


private void SpawnEndPortal()
{
    if (!endLevelPortalPrefab)
    {
        Debug.LogWarning("[Boss] No end-level portal prefab assigned");
        return;
    }

    Vector3 spawnPos = transform.position;
    spawnPos.y += 0.5f; // lift slightly off ground

    Instantiate(endLevelPortalPrefab, spawnPos, Quaternion.identity);
}


 private void Update()
{
    if (!enabled || isDead) return;
    if (!player) return;

    SetCastEffectActive(currentAction == BossAction.PhaseCast);
    HandleMovement();
    HandleDecision();
}



private GameObject GetRandomEnemyPrefab()
{
    if (enemySpawnPool == null || enemySpawnPool.Length == 0)
        return null;

    float totalWeight = 0f;
    foreach (var entry in enemySpawnPool)
        totalWeight += entry.weight;

    float roll = Random.value * totalWeight;
    float running = 0f;

    foreach (var entry in enemySpawnPool)
    {
        running += entry.weight;
        if (roll <= running)
            return entry.prefab;
    }

    return enemySpawnPool[0].prefab; // fallback
}


private void FixedUpdate()
{
    if (isDead) return;

    if (physicsSuspended)
        return;

    rb.angularVelocity = 0f;
}


[SerializeField] private float castCooldown = 6f;

private void SetCastEffectActive(bool active)
{
    if (!castEffectAnimator) return;

    // Safety check
    if (castEffectAnimator == animator)
    {
        Debug.LogError("[MrSmileyBoss] CastEffect animator is the BOSS animator!");
        return;
    }

    if (active)
    {
        castEffectAnimator.gameObject.SetActive(true);

        // 🔥 FORCE animation to play
        castEffectAnimator.Play(
            castEffectAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash,
            0,
            0f
        );
    }
    else
    {
        castEffectAnimator.gameObject.SetActive(false);
    }
}


    // =====================================================
    // MOVEMENT
    // =====================================================
private void HandleMovement()
{
    if (currentAction == BossAction.PhaseCast ||
        currentAction == BossAction.NormalCast ||
        !canMove ||
        !touchingDirections.IsGrounded)
    {
        animator.SetBool("IsMoving", false);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        return;
    }

    float dx = player.position.x - transform.position.x;
    facingDir = Mathf.Sign(dx);

    rb.linearVelocity = new Vector2(facingDir * walkSpeed, rb.linearVelocity.y);
    animator.SetBool("IsMoving", true);
    Flip(facingDir);
}





    private void Flip(float dir)
    {
        if (dir == 0) return;

        transform.localScale = new Vector3(
            Mathf.Sign(dir) * Mathf.Abs(baseScale.x),
            baseScale.y,
            baseScale.z
        );
    }

    // =====================================================
    // AI DECISION
    // =====================================================

private IEnumerator NormalCastRoutine()
{
    if (currentAction != BossAction.None)
        yield break;

    currentAction = BossAction.NormalCast;
    canNormalCast = false;

    canMove = false;
    rb.linearVelocity = Vector2.zero;

    animator.SetBool("IsMoving", false);
    animator.SetBool("isNormalCasting", true);
    SetCastEffectActive(true);

    yield return new WaitForSeconds(castWindupTime);

    spikeController?.SetSpikesPerCast(GetSpikeCountForPhase());
    spikeController?.StartSpikeCast();
    shadowHandController?.FireHands(GetHandCountForPhase());

    yield return new WaitForSeconds(0.4f);

    animator.SetBool("isNormalCasting", false);
    SetCastEffectActive(false);

    // ✅ RESET COOLDOWN HERE
    nextNormalCastTime = Time.time + normalCastCooldown;

    canMove = true;
    canNormalCast = true;
    currentAction = BossAction.None;
}










private void HandleDecision()
{
    if (currentAction != BossAction.None)
        return;

    if (Time.time < nextDecisionTime)
        return;

    float distance = Vector2.Distance(transform.position, player.position);

    if (distance <= meleeRange)
    {
        StartCoroutine(MeleeRoutine());
    }
    else if (distance <= spinRange && Time.time >= nextSpinTime)
    {
        StartCoroutine(SpinRoutine());
    }
    else if (canNormalCast && Time.time >= nextNormalCastTime)
    {
        StartCoroutine(NormalCastRoutine());
    }

    nextDecisionTime = Time.time + decisionDelay;
}







    // =====================================================
    // MELEE
    // =====================================================
    private IEnumerator MeleeRoutine()
{
    isBusy = true;

    rb.linearVelocity = Vector2.zero;
    animator.SetBool("IsMoving", false);
    animator.SetTrigger("Attack");

    yield return new WaitForSeconds(0.8f); // match animation hit frame

    isBusy = false;
    rb.linearVelocity = Vector2.zero;
canMove = true;

}


    // =====================================================
    // SPIN
    // =====================================================
private IEnumerator SpinRoutine()
{
    EnableSpinHitbox();

    if (currentAction == BossAction.PhaseCast)
    yield break;

    currentAction = BossAction.Spin;

    nextSpinTime = Time.time + spinCooldownTime;

    canMove = false;
    rb.linearVelocity = Vector2.zero;

    animator.SetBool("IsMoving", false);
    animator.SetBool("IsSpinning", true);
    animator.SetTrigger("Spin");

    yield return new WaitForSeconds(2f); // wind-up

    Vector2 dir = (player.position - transform.position).normalized;
    dir.y = 0f;
    dir.Normalize();

    Flip(dir.x);

    float originalGravity = rb.gravityScale;
    rb.gravityScale = 0f;

    float timer = 0f;
    float speed = walkSpeed * spinSpeedMultiplier;

    while (timer < spinDuration)
    {
        rb.linearVelocity = dir * speed;
        timer += Time.deltaTime;
        yield return null;
    }

   rb.linearVelocity = Vector2.zero;
rb.gravityScale = originalGravity;

// 🔥 TURN OFF SPIN DAMAGE
DisableSpinHitbox();

animator.SetBool("IsSpinning", false);


    canMove = true;
    currentAction = BossAction.None;
}






    // =====================================================
    // PHASE HANDLER (SUMMON ONLY)
    // =====================================================
  private void OnHealthChanged(int current, int max)
{
    float hp = (float)current / max;

    if (!spawned75 && hp <= 0.75f)
    {
        spawned75 = true;
        currentPhase = BossPhase.Phase2;
        StartCoroutine(PhaseCastRoutine(summonsPhase2));
    }
    else if (!spawned50 && hp <= 0.50f)
    {
        spawned50 = true;
        currentPhase = BossPhase.Phase3;
        StartCoroutine(PhaseCastRoutine(summonsPhase3));
    }
    else if (!spawned25 && hp <= 0.25f)
    {
        spawned25 = true;
        currentPhase = BossPhase.Phase4;
        StartCoroutine(PhaseCastRoutine(summonsPhase4));
    }
}



private int GetSpikeCountForPhase()
{
    return currentPhase switch
    {
        BossPhase.Phase1 => spikesPhase1,
        BossPhase.Phase2 => spikesPhase2,
        BossPhase.Phase3 => spikesPhase3,
        BossPhase.Phase4 => spikesPhase4,
        _ => spikesPhase1
    };
}

private int GetHandCountForPhase()
{
    return currentPhase switch
    {
        BossPhase.Phase1 => handsPhase1,
        BossPhase.Phase2 => handsPhase2,
        BossPhase.Phase3 => handsPhase3,
        BossPhase.Phase4 => handsPhase4,
        _ => handsPhase1
    };
}

   
private IEnumerator PhaseCastRoutine(int summonCount)
{

    currentAction = BossAction.PhaseCast;
    // 🔓 UNLOCK ARENA DURING PHASE CAST
foreach (var spike in phaseLockedSpikes)
    spike.DisableSpikes();
    canMove = false;
    canNormalCast = false;

    // Stop movement
    rb.linearVelocity = Vector2.zero;
    rb.angularVelocity = 0f;

    // Teleport to cast point
yield return StartCoroutine(FadeTeleportTo(castSpawnPoint));


    // Lock physics (solid + no falling)
    float originalGravity = rb.gravityScale;
    rb.gravityScale = 0f;
    rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;

    // Start casting visuals
    animator.SetBool("isNormalCasting", false);
    animator.SetBool("IsCasting", true);
    SetCastEffectActive(true);

    // 🔥 SPAWN EVERYTHING AT ONCE (WITH SPREAD)
for (int i = 0; i < summonCount; i++)
{
    Transform p = summonPoints[Random.Range(0, summonPoints.Length)];
    GameObject prefab = GetRandomEnemyPrefab();
    if (prefab == null) continue;

    // Small guaranteed offset so units never overlap
    float angle = (360f / summonCount) * i;
    Vector2 offset = new Vector2(
        Mathf.Cos(angle * Mathf.Deg2Rad),
        Mathf.Sin(angle * Mathf.Deg2Rad)
    ) * 0.4f; // tweak radius if needed

    Instantiate(prefab, (Vector2)p.position + offset, Quaternion.identity);
}


    // ⏱ HOLD CAST FOR FULL DURATION
    yield return new WaitForSeconds(phaseCastDuration);

    // End casting visuals
    animator.SetBool("IsCasting", false);
    SetCastEffectActive(false);

    // Restore physics
    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    rb.gravityScale = originalGravity;

    // Return to ground
   yield return StartCoroutine(FadeTeleportTo(castSpawnPoint));
   // 🔒 RE-LOCK ARENA AFTER PHASE (unless final phase)
    if (currentPhase != BossPhase.Phase4)
    {
        foreach (var spike in phaseLockedSpikes)
            spike.EnableSpikes();
    }

    // Unlock AI
    canMove = true;
    canNormalCast = true;
    currentAction = BossAction.None;
}


private IEnumerator FadeTeleportTo(Transform target, float fadeTime = 0.35f)
{
    if (!target || !spriteRenderer)
        yield break;

    physicsSuspended = true;
    rb.linearVelocity = Vector2.zero;

    Color c = spriteRenderer.color;

    // Fade OUT
    float t = 0f;
    while (t < fadeTime)
    {
        t += Time.deltaTime;
        c.a = Mathf.Lerp(1f, 0f, t / fadeTime);
        spriteRenderer.color = c;
        yield return null;
    }

    // TELEPORT (instant)
    transform.position = new Vector3(
        target.position.x,
        target.position.y,
        transform.position.z
    );

    // Fade IN
    t = 0f;
    while (t < fadeTime)
    {
        t += Time.deltaTime;
        c.a = Mathf.Lerp(0f, 1f, t / fadeTime);
        spriteRenderer.color = c;
        yield return null;
    }

    physicsSuspended = false;
}




    public void EnableMeleeHitbox() => meleeHitbox.EnableHitbox();
    public void DisableMeleeHitbox() => meleeHitbox.DisableHitbox();
    public void EnableSpinHitbox() => spinHitbox.EnableHitbox();
    public void DisableSpinHitbox() => spinHitbox.DisableHitbox();
}
