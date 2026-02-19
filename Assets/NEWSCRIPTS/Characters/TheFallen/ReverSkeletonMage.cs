using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ReaverSkeletonMage : BaseSkeletonMage
{
    // =====================================================
    // PHASE 1 – SUMMONING
    // =====================================================
    [Header("Summoning")]
    [SerializeField] private GameObject summonPrefab;
    [SerializeField] private Transform[] summonPoints;

    [SerializeField] private int minSummons = 2;
    [SerializeField] private int maxSummons = 3;

    [SerializeField] private float summonCooldown = 6f;
    [SerializeField] private float summonWindup = 0.7f;

    // =====================================================
    // MELEE
    // =====================================================
    [Header("Melee")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float meleeRange = 1.4f;
    [SerializeField] private float meleeCooldown = 2.2f;
    [SerializeField] private BossHitbox meleeHitbox;
private bool phaseTriggered;
    // =====================================================
    // INTERNAL STATE
    // =====================================================
    private Rigidbody2D rb;
    private float nextMeleeTime;
    private float nextSummonTime;
    private bool isSummoning;
    private bool frenzy;

    // Facing
    private float facing = 1f;
    private Vector3 baseScale;

    // =====================================================
    // UNITY
    // =====================================================
    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;

        if (meleeHitbox)
            meleeHitbox.DisableHitbox();
    }

    protected override void Update()
    {
        if (!isEnabled || !IsAlive)
            return;

        HandleMovementAndMelee();

        // Summoning pressure
        if (!isSummoning && Time.time >= nextSummonTime)
        {
            StartCoroutine(SummonRoutine());
        }
           if (!phaseTriggered && damagable.Health <= damagable.MaxHealth * 0.35f)
    {
        phaseTriggered = true;
        controller.NotifyMageLowHealth(this);
    }
    if (controller.CurrentPhase != FallenController.FallenPhase.Final_Trinity &&
    damagable.Health <= 1)
{
    damagable.Health = 1; // 🚫 cannot die yet
}

    }

    // =====================================================
    // MOVEMENT + MELEE
    // =====================================================
    private void HandleMovementAndMelee()
    {
        Transform player = FindPlayer();
        if (!player) return;

        float dx = player.position.x - transform.position.x;
        float distance = Mathf.Abs(dx);
        float dir = Mathf.Sign(dx);

        // Flip to face player
        if (dir != 0 && dir != facing)
        {
            facing = dir;
            transform.localScale = new Vector3(
                facing * Mathf.Abs(baseScale.x),
                baseScale.y,
                baseScale.z
            );
        }

        // Move or attack
        if (distance > meleeRange)
        {
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
            animator.SetBool("IsMoving", true);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsMoving", false);

            if (allowMajorAttack && Time.time >= nextMeleeTime)
{
    nextMeleeTime = Time.time + meleeCooldown;
    animator.SetTrigger("Attack");
}

        }
    }

    // =====================================================
    // ANIMATION EVENTS (MELEE HITBOX)
    // =====================================================
    public void EnableMeleeHitbox()
    {
        if (meleeHitbox)
            meleeHitbox.EnableHitbox();
    }

    public void DisableMeleeHitbox()
    {
        if (meleeHitbox)
            meleeHitbox.DisableHitbox();
    }

    // =====================================================
    // ALLY DEATH → FRENZY
    // =====================================================
    public override void OnAllyDeath()
    {
        if (!IsAlive || frenzy) return;
        StartCoroutine(FrenzyRoutine());
    }

    private IEnumerator FrenzyRoutine()
    {
        frenzy = true;

        float originalCooldown = summonCooldown;
        float originalAnimSpeed = animator.speed;

        summonCooldown *= 0.6f;
        animator.speed *= 1.25f;

        yield return new WaitForSeconds(3f);

        summonCooldown = originalCooldown;
        animator.speed = originalAnimSpeed;
        frenzy = false;
    }

    // =====================================================
    // SUMMONING
    // =====================================================
    private IEnumerator SummonRoutine()
    {
        isSummoning = true;
        nextSummonTime = Time.time + summonCooldown;

        rb.linearVelocity = Vector2.zero;
        animator.SetBool("IsMoving", false);
        animator.SetTrigger("Cast");

        yield return new WaitForSeconds(summonWindup);

        int count = Random.Range(minSummons, maxSummons + 1);

        for (int i = 0; i < count; i++)
        {
            Transform p = summonPoints[Random.Range(0, summonPoints.Length)];
            Vector2 offset = new Vector2(Random.Range(-0.6f, 0.6f), 0f);

            GameObject summoned = Instantiate(
                summonPrefab,
                (Vector2)p.position + offset,
                Quaternion.identity
            );

            // Force foreground rendering
            var sr = summoned.GetComponentInChildren<SpriteRenderer>();
            if (sr)
            {
                sr.sortingLayerName = "Enemies";
                sr.sortingOrder = 5;
            }
        }

        isSummoning = false;
    }
private void OnDisable()
{
    Debug.LogError($"🔥 ReaverSkeletonMage DISABLED by something on frame {Time.frameCount}", this);
}

    // =====================================================
    // UTIL
    // =====================================================
    private Transform FindPlayer()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        return go ? go.transform : null;
    }
}
