using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DetectionZone : MonoBehaviour
{
    [Header("Filter (set to Player layer if you use layers)")]
    public LayerMask detectionMask;           // optional
    public bool useTagCheck = true;
    public string requiredTag = "Player";
[Header("Ground Detection (optional)")]
public bool detectGround = false;   // enable ONLY for CliffDetectionZone
public LayerMask groundMask;

public bool IsTouchingGround { get; private set; }

    [Header("Safety")]
    public bool ignoreMyOwnCollider = true;   // <-- new
    public bool ignoreMyRoot = true;          // <-- new

    [Header("Debug")]
    public bool debugLogs = false;

    [HideInInspector] public List<Collider2D> detectedColliders = new List<Collider2D>();

    private Transform _myRoot;
    private Collider2D _myCollider;

    void Awake()
    {
        _myCollider = GetComponent<Collider2D>();
        _myCollider.isTrigger = true;
        _myRoot = transform.root;
    }

    bool IsValid(Collider2D other)
{
    if (!other) return false;
// NEVER detect anything from the same enemy prefab
if (other.transform.root == transform.root)
    return false;

    // NEVER allow ground unless this is a ground detector
    if (!detectGround && (groundMask.value & (1 << other.gameObject.layer)) != 0)
        return false;

    if (ignoreMyOwnCollider && other == _myCollider) return false;
    if (ignoreMyRoot && other.transform.root == _myRoot) return false;

    if (detectionMask.value != 0)
        if ((detectionMask.value & (1 << other.gameObject.layer)) == 0) return false;

    if (useTagCheck && !string.IsNullOrEmpty(requiredTag))
        if (!other.CompareTag(requiredTag) && !other.transform.root.CompareTag(requiredTag)) return false;

    return true;
}

   void OnTriggerEnter2D(Collider2D other)
{
   

    // --- Ground detection path ---
    if (detectGround)
    {
        if ((groundMask.value & (1 << other.gameObject.layer)) != 0)
        {
            IsTouchingGround = true;
        }
        return;
    }

    // --- Normal detection path (player, enemies, etc.) ---
    if (!IsValid(other)) return;

    if (!detectedColliders.Contains(other))
    {
        detectedColliders.Add(other);
        if (debugLogs) Debug.Log($"[DetectionZone:{name}] + {other.name}");
    }
}


   void OnTriggerExit2D(Collider2D other)
{
    // --- Ground detection path ---
    if (detectGround)
    {
        if ((groundMask.value & (1 << other.gameObject.layer)) != 0)
        {
            IsTouchingGround = false;
        }
        return;
    }

    // --- Normal detection path ---
    if (detectedColliders.Remove(other))
    {
        if (debugLogs) Debug.Log($"[DetectionZone:{name}] - {other.name}");
    }
}

    void OnDisable() => detectedColliders.Clear();
}
