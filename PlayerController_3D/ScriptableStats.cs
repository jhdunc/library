using UnityEngine;
[CreateAssetMenu(fileName ="Player Stats",menuName ="Player")]
public class ScriptableStats : ScriptableObject
{
    [Header("LAYERS")] [Tooltip("Set this to the layer of the player")]
    public LayerMask groundLayer;

    [Header("GROUNDER SETTINGS")]
    [Tooltip("Amount to offset ground/ceiling detection and range of detection sphere")]
    public float grounderOffset = -0.87f;
    public float grounderRadius = 0.2f;

    [Header("WALL CHECK OFFSET")]
    [Tooltip("Amount to offset wall detection and range of detection sphere")]
    public float wallCheckOffset = 0.2f;
    public float wallCheckRadius = 0.2f;

    [Header("COMBAT")]
    public float invincibleTime = 0.5f;
    public int damage = 1;

    [Header("WALKING SPEED")]
    [Tooltip("How fast the player moves gets to max speed. Velocity adjustments for acceleration.")]
    public float walkSpeed = 10.2f;
    public float acceleration = 1.43f;
    public float maxWalkingPenalty = 0.5f;
    public float currentMovementLerpSpeed = 100;

    [Header("JUMPING")]
    public bool enableDoubleJump = true;
    public float jumpForce = 13;
    public float fallMultiplier = 2;
    public float jumpVelocityFalloff = 2;
    public float wallJumpLock = 0.125f;
    public float wallJumpMovementLerp = 20;
    public float coyoteTime = 0.3f;

    [Header("WALL SLIDE")]
    [Tooltip("How quickly the player can slide down a wall")]
    public float slideSpeed = 4;

    [Header("DASH")]
    public float dashSpeed = 30;
    public float dashLength = 0.2f;

    [Header("DASH TARGETS")]
    public bool useDashTargets = true;
    public float dashTargetCastRadius = 4;
    public float dashTargetCastExtent = 6;
    public float dashTargetCastDistance = 15;
    public LayerMask dashTargetMask;
}