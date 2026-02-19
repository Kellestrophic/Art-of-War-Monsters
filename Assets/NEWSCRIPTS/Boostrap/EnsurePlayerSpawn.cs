using UnityEngine;
#if UNITY_NETCODE
using Unity.Netcode;
#endif

/// Guarantees a Player exists when the gameplay scene loads,
/// and places them safely ON the ground at a spawn point.
public class EnsurePlayerSpawn : MonoBehaviour
{
    [Header("Player Prefab & Spawn")]
    [SerializeField] private GameObject playerPrefab;     // Player prefab (with Damagable, tag "Player")
    [SerializeField] private Transform spawnPoint;        // Optional; auto-found if left empty

    [Header("Ground Snap")]
    [Tooltip("Layers considered 'ground' for safe spawn snap.")]
    [SerializeField] private LayerMask groundMask;        // <-- set to your Ground/Tilemap layer
    [Tooltip("How far up to start the ground ray (relative to the target X).")]
    [SerializeField] private float groundRayStartAbove = 10f;
    [Tooltip("How far down to scan for ground.")]
    [SerializeField] private float groundRayDistance = 40f;
    [Tooltip("Small pad above ground so feet don't clip.")]
    [SerializeField] private float groundPad = 0.05f;

    [Header("Options")]
    [Tooltip("Force unpause on scene load in case previous match paused time.")]
    [SerializeField] private bool unpauseOnLoad = true;
    [Tooltip("Tag to assign to the spawned player if none set.")]
    [SerializeField] private string playerTag = "Player";

#if UNITY_NETCODE
    [Header("Netcode")]
    [Tooltip("If Netcode is running and there is no local Player in the scene, also spawn one for the Host (PvE).")]
    [SerializeField] private bool spawnForHostWhenNetcode = false;
#endif

    private void Awake()
    {
        if (unpauseOnLoad) Time.timeScale = 1f;
    }

    private void Start()
    {
        // If a player already exists (e.g., placed in scene), do nothing.
        var existing = GameObject.FindGameObjectWithTag(playerTag);
        if (existing != null) return;

#if UNITY_NETCODE
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening)
        {
            // In online play the server/host usually spawns players.
            if (!spawnForHostWhenNetcode) return;
            if (!nm.IsServer) return; // only host/server may do this
        }
#endif

        if (!playerPrefab)
        {
            Debug.LogError("[EnsurePlayerSpawn] No playerPrefab assigned.");
            return;
        }

        // Find a spawn transform if not assigned
        Transform spawnT = FindSpawnTransform();
        Vector3 basePos = spawnT ? spawnT.position : Vector3.zero;
        Quaternion rot  = spawnT ? spawnT.rotation : Quaternion.identity;

        // Instantiate slightly above in case ground snap fails
        var player = Instantiate(playerPrefab, basePos + Vector3.up * 2f, rot);
        if (!string.IsNullOrEmpty(playerTag)) player.tag = playerTag;
        var pd = player.GetComponentInChildren<Damagable>();
        if (pd) pd.isEnemy = false;
        // Snap to ground safely (if we know what 'ground' is)
        SafeSnapToGround(player, basePos);

        // Reset motion
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;

        // Keep on Z = 0 (typical 2D setup)
        var p = player.transform.position;
        player.transform.position = new Vector3(p.x, p.y, 0f);
    }

    private Transform FindSpawnTransform()
    {
        if (spawnPoint) return spawnPoint;

        // Try common tags/names if you didn’t wire the field
        var t = GameObject.FindGameObjectWithTag("Respawn")?.transform;
        if (t) return t;

        var go = GameObject.Find("PlayerSpawn") ?? GameObject.Find("SpawnPoint") ?? GameObject.Find("Spawn");
        return go ? go.transform : null;
    }

    private void SafeSnapToGround(GameObject player, Vector3 around)
    {
        if (groundMask.value == 0)
        {
            // Ground mask not set — keep the initial position
            return;
        }

        // Start a ray above the target X, scan down for ground
        Vector2 start = new Vector2(around.x, around.y + groundRayStartAbove);
        RaycastHit2D hit = Physics2D.Raycast(start, Vector2.down, groundRayDistance, groundMask);
        if (!hit) return;

        // Use player collider height to rest on top of ground
        float halfHeight = 0.5f;
        var col = player.GetComponent<Collider2D>();
        if (col != null)
        {
            // Bounds are valid after instantiate
            halfHeight = col.bounds.extents.y;
        }

        float y = hit.point.y + halfHeight + groundPad;
        var pos = player.transform.position;
        player.transform.position = new Vector3(around.x, y, pos.z);
    }
}
