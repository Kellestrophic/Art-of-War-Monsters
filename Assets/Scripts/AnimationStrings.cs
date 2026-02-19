using UnityEngine;

public class AnimationStrings : MonoBehaviour
{
    // ─────────────────────────────
    // MOVEMENT
    // ─────────────────────────────
    public const string isWalking      = "isWalking";
    public const string isRunning      = "isRunning";
    public const string isJumping      = "isJumping";
    public const string isFalling      = "isFalling";
    public const string isLanding      = "isLanding";
    public const string yVelocity      = "yVelocity";

    // ─────────────────────────────
    // STATE GATES
    // ─────────────────────────────
    // ─────────────────────────────
// STATE GATES
// ─────────────────────────────
public const string canMove        = "canMove";
public const string isAlive        = "isAlive";
public const string lockVelocity   = "lockVelocity";


    // ─────────────────────────────
    // COMBAT
    // ─────────────────────────────
    public const string attackTrigger        = "attack";
    public const string rangedAttackTrigger  = "rangedAttack";
    public const string hitTrigger           = "hit";

    // If your animator has a light attack:
    public const string attackLightTrigger   = "AttackLight";

    // ─────────────────────────────
    // SPELL / CAST / SPECIAL
    // ─────────────────────────────
    public const string castTrigger          = "CastTrigger";
    public const string spawnTrigger         = "SpawnTrigger"; 
    public const string jumpTrigger         = "jumpTrigger";



    // Death
    public const string deathTrigger         = "DeathTrigger";

    // ─────────────────────────────
    // ENVIRONMENTAL
    // ─────────────────────────────
    public const string isGrounded    = "isGrounded";
    public const string isOnWall      = "isOnWall";
    public const string isOnCeiling   = "isOnCeiling";

    // cooldown param (if used)
    public const string attackCooldown = "attackCooldown";

    // ─────────────────────────────
// LEGACY SUPPORT (for older systems)
// ─────────────────────────────

// Used by old player & enemy AI
public const string isMoving   = "isMoving";    // old movement bool
public const string hasTarget  = "hasTarget";   // enemies use this to detect player
public const string isHit      = "isHit";       // Damageable.cs references this

}



