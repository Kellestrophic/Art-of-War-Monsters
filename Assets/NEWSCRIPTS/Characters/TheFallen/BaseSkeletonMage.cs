using UnityEngine;

[RequireComponent(typeof(Damagable))]
public class BaseSkeletonMage : MonoBehaviour
{
    [Header("Controller")]
    public FallenController controller;

    [Header("State")]
    public bool IsAlive { get; private set; } = true;

    protected bool isEnabled;
    protected bool allowMajorAttack;

    protected Damagable damagable;
    protected Animator animator;
    protected Rigidbody2D rb;

    protected virtual void Awake()
{
    damagable = GetComponent<Damagable>();
   animator = GetComponentInChildren<Animator>();
    rb = GetComponent<Rigidbody2D>();

    damagable.onDeath.AddListener(OnDeath);

    // 🔥 SAFETY: allow controller to enable us later
    isEnabled = false;
}


public virtual void SetEnabled(bool value)
{
    isEnabled = value;

    // DO NOT disable physics completely — just stop motion
    if (rb)
    {
        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    // Visuals
    var renderers = GetComponentsInChildren<SpriteRenderer>();
    foreach (var r in renderers)
        r.enabled = value;
}







    public virtual void AllowMajorAttack(bool value)
    {
        allowMajorAttack = value;
    }

    // 🔥 FINAL PHASE SIGNALS
    public virtual void OnFinalPhaseStart() { }
    public virtual void OnAllyDeath() { }

    protected virtual void Update()
    {
        if (!isEnabled || !IsAlive)
            return;
    }

    protected virtual void OnDeath()
    {
        if (!IsAlive) return;

        IsAlive = false;

        if (controller != null)
            controller.NotifyMageDeath(this);
    }
}
