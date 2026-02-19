using UnityEngine;

public class KillZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Damagable dmg = other.GetComponent<Damagable>();
        if (dmg == null)
            return;

        // Prevent double-kill
        if (!dmg.IsAlive)
            return;

        // Force death through the normal damage pipeline
        // Use a very large damage value so Health <= 0
        dmg.Hit(int.MaxValue, Vector2.zero);

        Debug.Log($"[KillZone] '{other.name}' killed by fall");
    }
}
