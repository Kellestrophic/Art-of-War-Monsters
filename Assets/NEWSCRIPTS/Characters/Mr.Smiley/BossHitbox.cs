using UnityEngine;

public class BossHitbox : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 10;
    public Vector2 knockback = new Vector2(4f, 2f);

    private Collider2D col;
    private Transform owner;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        owner = transform.root;

        col.enabled = false; // OFF by default
    }

    public void EnableHitbox()
    {
        col.enabled = true;
    }

    public void DisableHitbox()
    {
        col.enabled = false;
    }

   private void OnTriggerEnter2D(Collider2D other)
{
    if (!col.enabled) return;

    Damagable dmg = other.GetComponentInParent<Damagable>();
    if (dmg == null) return;

    Vector2 dir = (other.transform.position - owner.position).normalized;
    dmg.Hit(damage, new Vector2(dir.x * knockback.x, knockback.y));
}

}
