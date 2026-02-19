using UnityEngine;

public class CultistFacing : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    [Header("Masks")]
    [SerializeField] private LayerMask groundMask;   // Only solid ground/walls/tilemap layers
    [SerializeField] private LayerMask edgeMask;     // Usually same as groundMask

    [Header("Facing")]
    [SerializeField] private float flipCooldown = 0.25f;
    [SerializeField] private float flipThreshold = 0.5f;   // min X-separation to consider flipping
    private float nextFlipAllowed;
    private int facingDir = +1; // +1 right, -1 left

    [Header("Obstacles & Edges")]
    [SerializeField] private float wallCheckDistance = 0.25f;
    [SerializeField] private Vector2 wallBoxSize = new(0.18f, 0.38f);
    [SerializeField] private float edgeForward = 0.2f;
    [SerializeField] private float edgeDown = 0.35f;
    [SerializeField] private float wallGrace = 0.15f;
    [Header("Edges (robust)")]
[SerializeField] private float edgeDeepBonus = 0.45f;  // extra depth to confirm there's ground farther down
[SerializeField] private bool requireMoveTowardEdge = true;
[SerializeField] private float minSpeedForEdge = 0.05f;
[SerializeField] private float edgeCooldown = 0.40f;   // separate cooldown for edges

    private float wallBlockUntil;

    [Header("Startup")]
    [SerializeField] private float startupGrace = 0.2f;
    private float enableTime;

    [Header("Debug")]
    [SerializeField] private bool debugFlip;
    private string lastReason = "";

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        enableTime = Time.time;
        if (rb) rb.linearVelocity = Vector2.zero;
        if (animator) { animator.Rebind(); animator.Update(0f); }

        // Initial face toward player (no micro jitter)
        if (player)
        {
            float dx = player.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f) SetFacing(dx > 0 ? +1 : -1, "Init");
        }
        ApplyScale();
    }

    void Update()
    {
        if (Time.time < enableTime + startupGrace) return;

        // 1) Player-based facing with hysteresis
        if (player)
        {
            float dx = player.position.x - transform.position.x;
            if (Mathf.Abs(dx) > flipThreshold && Time.time >= nextFlipAllowed && Time.time >= wallBlockUntil)
            {
                int desired = dx > 0 ? +1 : -1;
                if (desired != facingDir) TryFlip(desired, "PlayerSide");
            }
        }

        // 2) Wall/edge handling (mask-safe, parent-scale-safe)
        if (IsBlockedAhead())
        {
            if (Time.time >= nextFlipAllowed)
            {
                TryFlip(-facingDir, "WallAhead");
                wallBlockUntil = Time.time + wallGrace;
            }
        }
        else if (IsEdgeAhead())
        {
            if (Time.time >= nextFlipAllowed)
            {
                TryFlip(-facingDir, "EdgeAhead");
                wallBlockUntil = Time.time + wallGrace;
            }
        }
    }

    private void TryFlip(int desired, string reason)
    {
        facingDir = desired;
        ApplyScale();
        nextFlipAllowed = Time.time + flipCooldown;
        lastReason = reason;
        if (debugFlip) Debug.Log($"[CultistFacing] Flip -> {facingDir} ({reason}) @ {name}");
    }

    private void SetFacing(int dir, string reason)
    {
        facingDir = dir;
        ApplyScale();
        lastReason = reason;
    }

    private void ApplyScale()
    {
        // Apply facing that is robust to parent negative scale
        Vector3 lossy = transform.lossyScale;
        float signParent = Mathf.Sign(lossy.x == 0 ? 1 : lossy.x);
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * facingDir * signParent;
        transform.localScale = s;
    }

    private bool IsBlockedAhead()
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(facingDir * (wallBoxSize.x * 0.5f + 0.05f), 0.0f);
        RaycastHit2D hit = Physics2D.BoxCast(origin, wallBoxSize, 0f, Vector2.right * facingDir, wallCheckDistance, groundMask);
        if (debugFlip)
        {
            Color c = hit ? Color.red : Color.green;
            DebugDrawBox(origin + Vector2.right * wallCheckDistance * 0.5f, wallBoxSize, c, 0f);
        }
        return hit.collider != null;
    }

    private bool IsEdgeAhead()
    {
        Vector2 probe = (Vector2)transform.position + new Vector2(facingDir * edgeForward, 0f);
        RaycastHit2D hit = Physics2D.Raycast(probe, Vector2.down, edgeDown, edgeMask);
        if (debugFlip)
        {
            Debug.DrawLine(probe, probe + Vector2.down * edgeDown, hit ? Color.green : Color.red);
        }
        return !hit;
    }

    private void DebugDrawBox(Vector2 center, Vector2 size, Color color, float ang)
    {
        Vector2 h = size * 0.5f;
        Vector2[] pts = new[]
        {
            center + new Vector2(-h.x, -h.y),
            center + new Vector2(-h.x,  h.y),
            center + new Vector2( h.x,  h.y),
            center + new Vector2( h.x, -h.y)
        };
        Debug.DrawLine(pts[0], pts[1], color);
        Debug.DrawLine(pts[1], pts[2], color);
        Debug.DrawLine(pts[2], pts[3], color);
        Debug.DrawLine(pts[3], pts[0], color);
    }
}
