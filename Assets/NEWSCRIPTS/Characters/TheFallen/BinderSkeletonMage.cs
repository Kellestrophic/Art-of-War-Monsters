using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BinderSkeletonMage : BaseSkeletonMage
{
    // =====================================================
    // ORBS
    // =====================================================
    [Header("Dark Orbs")]
    [SerializeField] private GameObject darkOrbPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float orbCooldown = 2.8f;

    // =====================================================
    // DASH MOVEMENT
    // =====================================================
    [Header("Flying Dash")]
    [SerializeField] private float preferredDistance = 5f;
    [SerializeField] private float dashSpeed = 6f;
    [SerializeField] private float dashDuration = 0.35f;
    [SerializeField] private float dashCooldown = 1.8f;
    [SerializeField] private float hoverHeight = 2.2f;
private float facing = 1f;
private Vector3 baseScale;
private bool phaseTriggered;

    // =====================================================
    // ARENA BOUNDS
    // =====================================================
    [Header("Arena Bounds")]
  [Header("Arena Bounds")]
[SerializeField] private Transform leftBound;
[SerializeField] private Transform rightBound;

    // =====================================================
    // STATE
    // =====================================================
    private float nextOrbTime;
    private float nextDashTime;
    private bool isCasting;
    private bool isDashing;
    private float targetY;
    private float dashDir;

    // =====================================================
    // UNITY
    // =====================================================
   protected override void Awake()
{
    base.Awake();

    rb.bodyType = RigidbodyType2D.Kinematic;
    rb.gravityScale = 0f;
    rb.freezeRotation = true;

    baseScale = transform.localScale; // ⭐ ADD THIS
    targetY = transform.position.y + hoverHeight;
}


    protected override void Update()
    {
        if (!isEnabled || !IsAlive)
            return;

        MaintainHover();

        if (!isDashing)
            HandleDashLogic();

        ClampToArena();
   if (!phaseTriggered && damagable.Health <= damagable.MaxHealth * 0.35f)
    {
        phaseTriggered = true;
        controller.NotifyMageLowHealth(this);
    }
        if (!allowMajorAttack || isCasting)
            return;

        if (Time.time >= nextOrbTime)
            StartCoroutine(CastRoutine());
    }

    // =====================================================
    // HOVER
    // =====================================================
    private void MaintainHover()
    {
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * 3f);
        transform.position = pos;
    }

    // =====================================================
    // DASH LOGIC
    // =====================================================
    private void HandleDashLogic()
    {
        if (Time.time < nextDashTime)
            return;

        Transform player = FindPlayer();
        if (!player)
            return;

        float dx = transform.position.x - player.position.x;
        float distance = Mathf.Abs(dx);

        if (distance < preferredDistance)
        {
           dashDir = Mathf.Sign(dx);

// WALL-AWARE REVERSAL
if (WillHitWall(dashDir))
    dashDir *= -1f;

// 🔄 FACE DASH DIRECTION
FaceDirection(dashDir);

StartCoroutine(DashRoutine());

        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;

        animator.SetBool("IsMoving", true);

        float t = 0f;
        while (t < dashDuration)
        {
            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
            t += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        animator.SetBool("IsMoving", false);

        isDashing = false;
    }

    // =====================================================
    // WALL AWARENESS
    // =====================================================
    private bool WillHitWall(float dir)
{
    float nextX = transform.position.x + dir * dashSpeed * dashDuration;
    return nextX <= leftBound.position.x || nextX >= rightBound.position.x;
}


  private void ClampToArena()
{
    Vector3 pos = transform.position;
    float clampedX = Mathf.Clamp(
        pos.x,
        leftBound.position.x,
        rightBound.position.x
    );

    if (Mathf.Abs(clampedX - pos.x) > 0.001f)
    {
        // Hit wall → stop dash movement
        rb.linearVelocity = Vector2.zero;
    }

    pos.x = clampedX;
    transform.position = pos;
}


    // =====================================================
    // CAST
    // =====================================================
    private IEnumerator CastRoutine()
    {
        isCasting = true;
        nextOrbTime = Time.time + orbCooldown;

        rb.linearVelocity = Vector2.zero;
        animator.SetBool("IsMoving", false);
        animator.SetTrigger("Blast");

        yield return new WaitForSeconds(0.15f);
        isCasting = false;
    }

    // 🔥 ANIMATION EVENT
    public void FireOrb()
    {
        if (!darkOrbPrefab || !firePoint)
            return;

        Transform player = FindPlayer();
        if (!player)
            return;

        Vector2 dir = (player.position - firePoint.position).normalized;
        GameObject orb = Instantiate(darkOrbPrefab, firePoint.position, Quaternion.identity);

        if (orb.TryGetComponent(out GhostProjectile ghost))
            ghost.Launch(dir, gameObject);
    }
private void FaceDirection(float dir)
{
    if (dir == 0f || dir == facing)
        return;

    facing = dir;
    transform.localScale = new Vector3(
        facing * Mathf.Abs(baseScale.x),
        baseScale.y,
        baseScale.z
    );
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
