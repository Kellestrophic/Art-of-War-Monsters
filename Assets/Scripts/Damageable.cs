using System;
using UnityEngine;
using UnityEngine.Events;

public class Damagable : MonoBehaviour
{
    // --- Events (keep names/signatures exactly) ---
    public UnityEvent<int, Vector2> damagableHit;
    public UnityEvent<int, int> healthChanged = new UnityEvent<int, int>();
    public UnityEvent onDeath = new UnityEvent();

    // --- Components / meta ---
    private Animator animator;
    private EnemyMetadata metadata; // provides statsKey + isBoss

    [Header("Debug / Overrides")]
    [Tooltip("Optional manual stats key if EnemyMetadata is missing.")]
    [SerializeField] private string statsKeyOverride = "";
    // Canonical key for StatsTracker + CosmeticUnlock
public string metaKey =>
    !string.IsNullOrEmpty(statsKeyOverride)
        ? statsKeyOverride
        : (!string.IsNullOrEmpty(metadata?.statsKey) ? metadata.statsKey : "");
public void SetStatsKeyForTracking(string key)
{
    if (!string.IsNullOrEmpty(key))
        statsKeyOverride = key;  // forces the reporter to use THIS key
}


    // --- Health ---
    [SerializeField] private int _maxHealth = 100;
    public int MaxHealth
    {
        get => _maxHealth;
        set => _maxHealth = value;
    }

    [SerializeField] private int _health = 100;
    public int Health
    {
        get => _health;
        set
        {
            _health = value;
            healthChanged?.Invoke(_health, MaxHealth);
            if (_health <= 0) IsAlive = false;
            
        }
    }

    // --- Alive / hit / invincibility ---
    [SerializeField] private bool _isAlive = true;
    [SerializeField] private bool isInvincible = false;

    // Guard: some animator controllers may not have these params
    private bool hasIsHitParam, hasLockVelocityParam, hasIsAliveParam, hasHitTriggerParam;

    public bool IsHit
    {
        get => (animator != null && hasIsHitParam) ? animator.GetBool(AnimationStrings.isHit) : false;
        private set { if (animator != null && hasIsHitParam) animator.SetBool(AnimationStrings.isHit, value); }
    }

    private float timeSinceHit = 0f;
    public float invicnibletyTime = 0.25f;

    // --- Enemy rewards ---
    [Header("Enemy XP Settings")]
    public bool isEnemy = false;
    public int xpReward = 25;


    // Exposed for other scripts exactly as before
    public bool LockVelocity
    {
        get => (animator != null && hasLockVelocityParam) && animator.GetBool(AnimationStrings.lockVelocity);
        set { if (animator != null && hasLockVelocityParam) animator.SetBool(AnimationStrings.lockVelocity, value); }
    }

    public bool IsAlive
    {
        get => _isAlive;
        set
        {
            if (_isAlive == value) return; // no-op
            _isAlive = value;
            

            if (animator != null)
    animator.SetBool("isAlive", value);


            Debug.Log("IsAlive set " + value);

            if (!_isAlive)
            {
                animator.SetTrigger(AnimationStrings.deathTrigger);

                try
                {
                    Debug.Log($"[Damagable] DEATH START for '{name}' (isEnemy={isEnemy})");

                    // 1) XP reward using new XP system
if (isEnemy && xpReward > 0)
{
    var store = RuntimeStatsStore.Instance;
    if (store != null && store.IsBootstrapped)
    {
        store.AddXP(xpReward, $"Enemy:{metaKey}");
        Debug.Log($"[Damagable] Granted {xpReward} XP for killing {metaKey}");
    }
}




                    // 2) Strike (only for enemies)
                    var strike = FindFirstObjectByType<StrikeBar>();
                    if (strike != null && isEnemy) strike.AddPoint();

                    // 3) Resolve stats key / boss flag
                    if (metadata == null)
                        metadata = GetComponent<EnemyMetadata>() ?? GetComponentInParent<EnemyMetadata>();

                    string key =
                        !string.IsNullOrEmpty(statsKeyOverride) ? statsKeyOverride :
                        (!string.IsNullOrEmpty(metadata?.statsKey) ? metadata.statsKey : gameObject.name);

                    bool boss = (metadata != null && metadata.isBoss);

        
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[Damagable] DEATH BLOCK EXCEPTION: " + ex);
                }
                finally
                {
                    // Always notify listeners (AI, UI hooks, etc.)
                    try { onDeath.Invoke(); }
                    catch (System.Exception ex2) { Debug.LogError("[Damagable] onDeath.Invoke() EXCEPTION: " + ex2); }

                    // Show the victory banner even without netcode:
                    var ui = FindFirstObjectByType<VictoryBannerUI>(FindObjectsInactive.Include);
                    if (ui != null)
                    {
                        // If an ENEMY died, the player won; if the PLAYER died, the player lost.
                        if (isEnemy) ui.ShowLocalWin("Player");
                        else ui.ShowLocalLoss("Player");
                    }
                }

                Debug.Log("[Damagable] DEATH DONE");
            }
        }
    }

   private void Awake()
{
    
    animator = GetComponent<Animator>();
    metadata = GetComponent<EnemyMetadata>() ?? GetComponentInParent<EnemyMetadata>();

    // NEW: auto-detect
    AutoDetectIsEnemy();

    // Cache animator params...
    hasIsHitParam        = AnimatorHasParam(animator, AnimationStrings.isHit);
    hasLockVelocityParam = AnimatorHasParam(animator, AnimationStrings.lockVelocity);
    hasIsAliveParam      = AnimatorHasParam(animator, AnimationStrings.isAlive);
    hasHitTriggerParam   = AnimatorHasParam(animator, AnimationStrings.hitTrigger, AnimatorControllerParameterType.Trigger);

    string keyProbe = (!string.IsNullOrEmpty(metadata?.statsKey)) ? metadata.statsKey : "(none)";
    bool bossProbe = metadata != null && metadata.isBoss;
    Debug.Log($"[Damagable] VERSION vA7  isEnemy={isEnemy}  metaKey={keyProbe}  boss={bossProbe}");
}


private void AutoDetectIsEnemy()
{
    // If you have EnemyMetadata on the object or parent, it’s an enemy.
    if (metadata == null)
        metadata = GetComponent<EnemyMetadata>() ?? GetComponentInParent<EnemyMetadata>();

    if (!isEnemy)
    {
        if (metadata != null)
        {
            isEnemy = true;
            Debug.Log($"[Damagable] Auto-set isEnemy=true on '{name}' (EnemyMetadata found)");
        }
        else if (CompareTag("Enemy"))
        {
            isEnemy = true;
            Debug.Log($"[Damagable] Auto-set isEnemy=true on '{name}' (tag=Enemy)");
        }
    }
}

    private void Update()
    {
        if (isInvincible)
        {
            if (timeSinceHit > invicnibletyTime)
            {
                isInvincible = false;
                timeSinceHit = 0;
            }
            timeSinceHit += Time.deltaTime;
        }

        if (!isInvincible && IsHit)
            IsHit = false;
    }

  public bool Hit(int damage, Vector2 knockback)
{
    if (!IsAlive || isInvincible) return false;

    Health -= damage;
    isInvincible = true;

    if (animator != null && hasHitTriggerParam)
        animator.SetTrigger(AnimationStrings.hitTrigger);

    LockVelocity = true;

    damagableHit?.Invoke(damage, knockback);
    CharacterEvents.characterDamaged.Invoke(gameObject, damage);
    return true;

}


// Let SendMessage("Hit", damage) or simple calls award strike too
public bool Hit(int damage)
{
    return Hit(damage, Vector2.zero);
}

public bool Heal(int healthRestore)
{
    if (IsAlive && Health < MaxHealth)
    {
        int actualHeal = Mathf.Min(healthRestore, MaxHealth - Health);
        Health += actualHeal;
        CharacterEvents.characterHealed.Invoke(gameObject, actualHeal);
        Debug.Log($"{gameObject.name} healed for {actualHeal} (max {MaxHealth})");
        return true;
    }

    Debug.Log($"{gameObject.name} is already at full health.");
    return false;
}


    // --- Helpers ---
    private static bool AnimatorHasParam(Animator anim, string paramName, AnimatorControllerParameterType type = AnimatorControllerParameterType.Bool)
    {
        if (anim == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (var p in anim.parameters)
        {
            if (p.type == type && p.name == paramName) return true;
            // allow mismatched type fallback (some controllers rename types)
            if (p.name == paramName) return true;
        }
        return false;
    }
}
