using UnityEngine;

public class HazardDamage : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private float tickRate = 0.5f;

    private Damagable _target;
    private float _timer;

    private void Update()
    {
        if (_target == null) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _timer = tickRate;
            _target.Hit(damage, Vector2.zero);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent(out Damagable dmg))
        {
            _target = dmg;
            _timer = 0f; // immediate tick
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_target == null) return;
        if (!other.CompareTag("Player")) return;

        _target = null;
    }
}

