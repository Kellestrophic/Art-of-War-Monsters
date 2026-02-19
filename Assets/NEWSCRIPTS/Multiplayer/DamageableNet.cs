using System;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class DamageableNet : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHP = 100;
    public int MaxHP => maxHP;

    // Everyone reads; only server writes
    public NetworkVariable<int> Hp = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Strike")]
    [Tooltip("Strike meter max (HUD expects 25 for your game).")]
    public int MaxStrike = 25;
    public NetworkVariable<int> Strike = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Hit Response")]
    [SerializeField] private float iFrameSeconds = 0.2f;
    [SerializeField] private bool setAnimatorOnDeath = true;

    private float lastHitServerTime = -999f;

    private Animator animator;
    private AnimatorSyncLite animSync;

    // NEW: local (client-side) signals so PlayerController can subscribe
    public event Action<int, Vector2> OnDamagedLocal; // (damage, knockback)
    public event Action OnDiedLocal;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>(true);
        animSync = GetComponentInChildren<AnimatorSyncLite>(true);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Hp.Value     = Mathf.Clamp(Hp.Value, 0, maxHP);
            Strike.Value = Mathf.Clamp(Strike.Value, 0, MaxStrike);
        }
    }

    /// <summary>
    /// Server-authoritative damage. Attacker reference lets us award Strike.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ApplyDamageServerRpc(
        int amount,
        float knockX = 0, float knockY = 0,
        bool playHurt = true,
        NetworkObjectReference attackerRef = default)
    {
        if (Hp.Value <= 0) return;

        // server-side i-frames
        if (Time.time - lastHitServerTime < iFrameSeconds) return;
        lastHitServerTime = Time.time;

        int dmg = Mathf.Max(0, Mathf.Abs(amount));
        Hp.Value = Mathf.Max(0, Hp.Value - dmg);

        // Award Strike to the attacker (+1 per successful hit; tweak as desired)
        if (attackerRef.TryGet(out var attackerNO))
        {
            var atkHP = attackerNO.GetComponent<DamageableNet>();
            if (atkHP != null)
            {
                atkHP.Strike.Value = Mathf.Min(atkHP.MaxStrike, atkHP.Strike.Value + 1);
            }
        }

        if (playHurt) HurtClientRpc(dmg, knockX, knockY);
        if (Hp.Value == 0) DeathClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RestoreFullServerRpc()
    {
        Hp.Value = maxHP;
        Strike.Value = 0; // reset if desired
        lastHitServerTime = -999f;
        if (setAnimatorOnDeath && animator) animator.SetBool(AnimationStrings.isAlive, true);
    }

    // ── client FX ───────────────────────────────────────────────
    [ClientRpc]
    private void HurtClientRpc(int amount, float kx, float ky)
    {
        var kb = new Vector2(kx, ky);

        // Fire local events on every client (owner + observers)
        OnDamagedLocal?.Invoke(amount, kb);

        // Apply owner knockback + optional cues only on controlling client
        var pcNet = GetComponent<PlayerController_Net>();
        var pc    = (object)pcNet ?? GetComponent<PlayerController_Net>(); // fallback if you ever use offline
        if (pcNet && IsOwner)
        {
            pcNet.OnHit(amount, kb);
        }
        else if (pc is PlayerController_Net offline && NetworkManager.Singleton == null)
        {
            offline.OnHit(amount, kb);
        }

        // Mirror a Hurt trigger for everyone’s Animator
        animSync?.RaiseHurt();
    }

    [ClientRpc]
    private void DeathClientRpc()
    {
        // Local event for UI/SFX on all clients
        OnDiedLocal?.Invoke();

        if (setAnimatorOnDeath && animator)
            animator.SetBool(AnimationStrings.isAlive, false);
    }
}
