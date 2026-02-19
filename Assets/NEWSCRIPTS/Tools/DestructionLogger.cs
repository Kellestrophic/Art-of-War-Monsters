using UnityEngine;

public class DestructionLogger : MonoBehaviour
{
    void OnEnable() { Debug.Log($"[Pickup] ENABLED: {name} at {transform.position}"); }
    void OnDisable() { Debug.Log($"[Pickup] DISABLED: {name} (frame {Time.frameCount})"); }
    void OnDestroy() { Debug.Log($"[Pickup] DESTROYED: {name} (frame {Time.frameCount})"); }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Pickup] Triggered by: {other.name} (tag: {other.tag}, layer: {other.gameObject.layer})");
    }
    void OnCollisionEnter2D(Collision2D col)
    {
        Debug.Log($"[Pickup] Collided with: {col.collider.name} (tag: {col.collider.tag}, layer: {col.collider.gameObject.layer})");
    }
}
