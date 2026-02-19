using UnityEngine;
using Unity.Netcode;

/// Detects local input / motion and tells the server "I'm active".
public class ActivityMonitorNet : NetworkBehaviour
{
    [Header("Detection")]
    [SerializeField] float moveEps = 0.02f;     // m/frame movement considered "activity"
    [SerializeField] float minPingInterval = 0.5f; // seconds between pings

    Rigidbody2D rb;
    Vector3 lastPos;
    float nextPingAt;

    void Awake() { rb = GetComponent<Rigidbody2D>(); lastPos = transform.position; }

    void Update()
    {
        if (!IsOwner || !IsClient) return;
        if (Time.unscaledTime < nextPingAt) { lastPos = transform.position; return; }

        bool inputActive =
            Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
            Input.GetButtonDown("Jump") ||
            Input.anyKeyDown;

        bool moved = (transform.position - lastPos).sqrMagnitude > (moveEps * moveEps);
        bool velocity = rb ? rb.linearVelocity.sqrMagnitude > 0.01f : false;

        if (inputActive || moved || velocity)
        {
            // tell the server we are active
            ReportActiveServerRpc();
            nextPingAt = Time.unscaledTime + minPingInterval;
        }
        lastPos = transform.position;
    }

    [ServerRpc(RequireOwnership = false)]
    void ReportActiveServerRpc(ServerRpcParams rpc = default)
    {
        var sender = rpc.Receive.SenderClientId;
        InactivityManagerNet.Instance?.NoteActive(sender);
    }
}
