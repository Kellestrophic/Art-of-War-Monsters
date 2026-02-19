// Assets/NEWSCRIPTS/Multiplayer/AnimatorSyncLite.cs
using Unity.Netcode;
using UnityEngine;

public class AnimatorSyncLite : NetworkBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] TouchingDirections touching; // optional

    [Header("Param names (match your Animator)")]
    [SerializeField] string speedParam="Speed", groundedParam="IsGrounded",
                          runningParam="isRunning", movingParam="isMoving",
                          yVelParam="yVelocity", fallingParam="isFalling";

    [Header("Triggers")]
    [SerializeField] string attackTrigger="attackTrigger", jumpTrigger="jumpTrigger",
                          rangedTrigger="rangedAttackTrigger", hurtTrigger="Hurt";

    public NetworkVariable<float> nvSpeed = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> nvGrounded = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> nvRunning = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> nvMoving = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> nvYVel = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> nvFalling = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!touching) touching = GetComponent<TouchingDirections>();
    }

    public override void OnNetworkSpawn()
    {
        nvSpeed   .OnValueChanged += (_,v)=>{ if(!IsOwner&&animator) animator.SetFloat(speedParam, v); };
        nvGrounded.OnValueChanged += (_,v)=>{ if(!IsOwner&&animator) animator.SetBool (groundedParam, v); };
        nvRunning .OnValueChanged += (_,v)=>{ if(!IsOwner&&animator) animator.SetBool (runningParam, v); };
        nvMoving  .OnValueChanged += (_,v)=>{ if(!IsOwner&&animator) animator.SetBool (movingParam, v); };
        nvYVel    .OnValueChanged += (_,v)=>{ if(!IsOwner&&animator) animator.SetFloat(yVelParam,  v); };
        nvFalling .OnValueChanged += (_,v)=>{ if(!IsOwner&&animator) animator.SetBool (fallingParam, v); };
    }

    void Update()
    {
        if (!IsOwner || !animator || !rb) return;
        nvSpeed.Value   = Mathf.Abs(rb.linearVelocity.x);
        nvYVel.Value    = rb.linearVelocity.y;
        nvFalling.Value = rb.linearVelocity.y < -0.15f;
        nvRunning.Value = animator.GetBool(runningParam);
        nvMoving.Value  = animator.GetBool(movingParam);
        nvGrounded.Value= touching ? touching.IsGrounded : animator.GetBool(groundedParam);

        // reflect for owner (in case controller didn’t already set them)
        animator.SetFloat(speedParam, nvSpeed.Value);
        animator.SetFloat(yVelParam,  nvYVel.Value);
        animator.SetBool (fallingParam, nvFalling.Value);
    }

    // Trigger replication (owner calls these from PlayerController)
    public void RaiseAttack(){ if(IsOwner){ animator.SetTrigger(attackTrigger); TriggerServerRpc(0);} }
    public void RaiseJump()  { if(IsOwner){ animator.SetTrigger(jumpTrigger);   TriggerServerRpc(1);} }
    public void RaiseRanged(){ if(IsOwner){ animator.SetTrigger(rangedTrigger); TriggerServerRpc(2);} }
    public void RaiseHurt()  { if(IsOwner){ animator.SetTrigger(hurtTrigger);   TriggerServerRpc(3);} }

    [ServerRpc] void TriggerServerRpc(byte id) => TriggerClientRpc(id);
    [ClientRpc] void TriggerClientRpc(byte id)
    {
        if (IsOwner || !animator) return;
        switch(id){ case 0: animator.SetTrigger(attackTrigger); break;
                    case 1: animator.SetTrigger(jumpTrigger);   break;
                    case 2: animator.SetTrigger(rangedTrigger); break;
                    case 3: animator.SetTrigger(hurtTrigger);   break; }
    }
}
