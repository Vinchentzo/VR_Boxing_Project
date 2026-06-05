using UnityEngine;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{
    private enum State
    {
        Approach,
        Hold,
        Attack
    }

    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int AttackParameter = Animator.StringToHash("Attack");

    private const string AttackStateTag = "Attack";

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 1.2f;
    [SerializeField, Min(0f)] private float desiredDistance = 1.8f;
    [SerializeField, Min(0f)] private float distanceBuffer = 0.15f;
    [SerializeField, Min(0f)] private float turnSpeed = 360f;

    [Header("Arena Bounds (XZ)")]
    [SerializeField] private Vector2 arenaMin = new Vector2(-5f, -5f);
    [SerializeField] private Vector2 arenaMax = new Vector2(5f, 5f);

    [Header("Strafing")]
    [SerializeField, Min(0f)] private float strafeSpeed = 0.8f;
    [SerializeField, Min(0f)] private float strafeChangeIntervalMin = 0.8f;
    [SerializeField, Min(0f)] private float strafeChangeIntervalMax = 2f;

    [Header("Attack")]
    [SerializeField, Min(0f)] private float attackDamage = 10f;
    [SerializeField, Min(0f)] private float attackCooldown = 2f;
    [SerializeField, Range(0f, 1f)] private float attackChancePerCheck = 0.35f;
    [SerializeField, Min(0.01f)] private float attackCheckInterval = 0.25f;

    private Rigidbody rigidBody;
    private State state = State.Approach;

    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private float fixedY;

    private float strafeDirection = 1f;
    private float nextStrafeChangeTime;
    private float nextAttackTime;
    private float nextAttackCheckTime;

    private bool attackAnimationStarted;
    private bool attackHitConsumed;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();

        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        //add as a safe check
        ConfigureRigidbody();

        startingPosition = rigidBody.position;
        startingRotation = rigidBody.rotation;
        fixedY = startingPosition.y;
    }

    private void ConfigureRigidbody()
    {
        if (rigidBody == null)
            return;

        rigidBody.isKinematic = true;
        rigidBody.useGravity = false;
        rigidBody.interpolation = RigidbodyInterpolation.Interpolate;

        // The enemy can rotate left/right, but should not fall or tilt.
        rigidBody.constraints = RigidbodyConstraints.FreezeRotationX |
                                RigidbodyConstraints.FreezeRotationZ;
    }

    private void OnEnable()
    {
        if (rigidBody == null || animator == null)
            return;

        //add as a safe check
        ConfigureRigidbody();

        ResetRuntimeState();
    }

    private void OnDisable()
    {
        if (rigidBody != null)
        {
            if (!rigidBody.isKinematic)
            {
                rigidBody.velocity = Vector3.zero;
                rigidBody.angularVelocity = Vector3.zero;
            }

            rigidBody.isKinematic = true;
        }

        if (animator != null)
        {
            animator.SetFloat(SpeedParameter, 0f);
            animator.ResetTrigger(AttackParameter);
        }
    }

    private void FixedUpdate()
    {
        Vector3 toPlayer = player.position - rigidBody.position;
        toPlayer.y = 0f;

        float distanceToPlayer = toPlayer.magnitude;

        Vector3 forward = toPlayer.sqrMagnitude > 0.0001f
            ? toPlayer.normalized
            : transform.forward;

        if (state != State.Attack)
            RotateTowardsPlayer(forward, toPlayer.sqrMagnitude);

        float currentTime = Time.time;

        if (state == State.Attack)
        {
            UpdateAttackState();
        }
        else
        {
            UpdateMovementState(distanceToPlayer);
            TryStartAttack(currentTime);
        }

        if (state == State.Hold)
            UpdateStrafeDirection(currentTime);

        Vector3 movement = CalculateMovement(forward, distanceToPlayer);

        animator.SetFloat(SpeedParameter, movement.magnitude);

        ApplyMovement(movement);
    }

    /// <summary>
    /// Allows one valid collision to deal damage during the current attack.
    /// Prevents one jab from damaging both the player's head and body.
    /// </summary>
    public bool TryConsumeAttackHit(out float damage)
    {
        damage = 0f;

        if (state != State.Attack || attackHitConsumed || !IsAttackDamageWindowActive())
            return false;

        attackHitConsumed = true;
        damage = attackDamage;

        return true;
    }

    /// <summary>
    /// Restores the enemy to the starting position and clears runtime combat state.
    /// Called when entering the menu or beginning a new fight.
    /// </summary>
    public void ResetForFight()
    {
        if (rigidBody == null || animator == null)
        {
            Debug.LogError("Enemy cannot reset because required references are missing.", this);
            return;
        }

        rigidBody.position = startingPosition;
        rigidBody.rotation = startingRotation;

        ResetRuntimeState();
    }

    private bool ValidateReferences()
    {
        if (rigidBody == null)
        {
            Debug.LogError("Enemy requires a Rigidbody component on the same GameObject.", this);
            return false;
        }

        if (player == null)
        {
            Debug.LogError("Enemy requires a Player Transform reference.", this);
            return false;
        }

        if (animator == null)
        {
            Debug.LogError("Enemy requires the Boxer Visual Animator reference.", this);
            return false;
        }

        if (arenaMin.x > arenaMax.x || arenaMin.y > arenaMax.y)
        {
            Debug.LogError("Enemy arena minimum bounds must be smaller than maximum bounds.", this);
            return false;
        }

        if (strafeChangeIntervalMin > strafeChangeIntervalMax)
        {
            Debug.LogError("Enemy minimum strafe interval cannot exceed maximum strafe interval.", this);
            return false;
        }

        return true;
    }

    private void ResetRuntimeState()
    {
        state = State.Approach;

        fixedY = startingPosition.y;

        strafeDirection = 1f;
        nextStrafeChangeTime = 0f;
        nextAttackTime = 0f;
        nextAttackCheckTime = 0f;

        attackAnimationStarted = false;
        attackHitConsumed = false;

        animator.SetFloat(SpeedParameter, 0f);
        animator.ResetTrigger(AttackParameter);
    }

    private void RotateTowardsPlayer(Vector3 forward, float squaredDistanceToPlayer)
    {
        if (squaredDistanceToPlayer < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(forward);
        Quaternion newRotation = Quaternion.RotateTowards(
            rigidBody.rotation,
            targetRotation,
            turnSpeed * Time.fixedDeltaTime
        );

        rigidBody.MoveRotation(newRotation);
    }

    private void UpdateMovementState(float distanceToPlayer)
    {
        float farThreshold = desiredDistance + distanceBuffer;
        float nearThreshold = desiredDistance - distanceBuffer;

        if (distanceToPlayer > farThreshold || distanceToPlayer < nearThreshold)
        {
            state = State.Approach;
            return;
        }

        state = State.Hold;
    }

    private void TryStartAttack(float currentTime)
    {
        if (state != State.Hold)
            return;

        if (currentTime < nextAttackTime || currentTime < nextAttackCheckTime)
            return;

        nextAttackCheckTime = currentTime + attackCheckInterval;

        if (Random.value >= attackChancePerCheck)
            return;

        state = State.Attack;

        nextAttackTime = currentTime + attackCooldown;

        attackAnimationStarted = false;
        attackHitConsumed = false;

        animator.SetFloat(SpeedParameter, 0f);
        animator.SetTrigger(AttackParameter);
    }

    private void UpdateAttackState()
    {
        bool isInAttackAnimation = animator
            .GetCurrentAnimatorStateInfo(0)
            .IsTag(AttackStateTag);

        if (!attackAnimationStarted)
        {
            if (isInAttackAnimation)
                attackAnimationStarted = true;

            return;
        }

        if (!isInAttackAnimation && !animator.IsInTransition(0))
            state = State.Hold;
    }

    private bool IsAttackDamageWindowActive()
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsTag(AttackStateTag)
               && !animator.IsInTransition(0);
    }

    private void UpdateStrafeDirection(float currentTime)
    {
        if (currentTime < nextStrafeChangeTime)
            return;

        strafeDirection = Random.value < 0.5f ? -1f : 1f;

        float nextChangeDelay = Random.Range(
            strafeChangeIntervalMin,
            strafeChangeIntervalMax
        );

        nextStrafeChangeTime = currentTime + nextChangeDelay;
    }

    private Vector3 CalculateMovement(Vector3 forward, float distanceToPlayer)
    {
        if (state == State.Attack)
            return Vector3.zero;

        if (state == State.Hold)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return right * strafeDirection * strafeSpeed;
        }

        float distanceError = distanceToPlayer - desiredDistance;

        if (distanceError > distanceBuffer)
            return forward * moveSpeed;

        if (distanceError < -distanceBuffer)
            return -forward * moveSpeed;

        return Vector3.zero;
    }

    private void ApplyMovement(Vector3 movement)
    {
        Vector3 newPosition = rigidBody.position + movement * Time.fixedDeltaTime;

        newPosition.x = Mathf.Clamp(newPosition.x, arenaMin.x, arenaMax.x);
        newPosition.z = Mathf.Clamp(newPosition.z, arenaMin.y, arenaMax.y);
        newPosition.y = fixedY;

        rigidBody.MovePosition(newPosition);
    }
}