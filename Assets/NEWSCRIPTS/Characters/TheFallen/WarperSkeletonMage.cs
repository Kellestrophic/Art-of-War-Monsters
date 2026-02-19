using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class WarperSkeletonMage : BaseSkeletonMage
{
    [Header("Teleport")]
    [SerializeField] private Transform[] teleportPoints;
    [SerializeField] private float teleportCooldown = 3.5f;

    [Header("Strike Zone")]
    [SerializeField] private GameObject strikeWarningPrefab;
    [SerializeField] private GameObject strikeImpactPrefab;
    [SerializeField] private float strikeDelay = 0.8f;
    [SerializeField] private float strikeCooldown = 2.5f;
private bool phaseTriggered;
    private float nextTeleportTime;
    private float nextStrikeTime;
    private bool isActing;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    protected override void Update()
    {
        if (!isEnabled || !IsAlive || !allowMajorAttack || isActing)
            return;

        if (Time.time >= nextTeleportTime && Time.time >= nextStrikeTime)
            StartCoroutine(TeleportAndStrikeRoutine());
              if (!phaseTriggered && damagable.Health <= damagable.MaxHealth * 0.35f)
    {
        phaseTriggered = true;
        controller.NotifyMageLowHealth(this);
    }
    }

    private IEnumerator TeleportAndStrikeRoutine()
    {
        isActing = true;
        nextTeleportTime = Time.time + teleportCooldown;
        nextStrikeTime = Time.time + strikeCooldown;

        // TELEPORT
        if (teleportPoints.Length > 0)
        {
            Transform p = teleportPoints[Random.Range(0, teleportPoints.Length)];
            rb.position = p.position;
        }

        yield return new WaitForSeconds(0.15f);

        Transform player = FindPlayer();
        if (!player)
        {
            isActing = false;
            yield break;
        }

        Vector2 strikePos = new Vector2(player.position.x, player.position.y - 0.5f);

        Instantiate(strikeWarningPrefab, strikePos, Quaternion.identity);
        yield return new WaitForSeconds(strikeDelay);
        Instantiate(strikeImpactPrefab, strikePos, Quaternion.identity);

        // 🔥 FINAL PHASE CHAIN
        if (allowMajorAttack)
        {
            yield return new WaitForSeconds(0.25f);
            Instantiate(strikeImpactPrefab, strikePos + Vector2.right * 1.5f, Quaternion.identity);
        }

        isActing = false;
    }

    private Transform FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        return go ? go.transform : null;
    }
}
