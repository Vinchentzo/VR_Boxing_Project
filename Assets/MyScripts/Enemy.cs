using UnityEngine;

public class Enemy : MonoBehaviour
{
    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 1.2f;
    public float desiredDistance = 1.8f;     // single target distance
    public float distanceBuffer = 0.15f;     // allowed band around desiredDistance
    public float turnSpeed = 360f;

    [Header("Arena Bounds (XZ)")]
    [SerializeField] private Vector2 arenaMin = new Vector2(-5, -5);
    [SerializeField] private Vector2 arenaMax = new Vector2(5, 5);

    [Header("Strafing")]
    public float strafeSpeed = 0.8f;
    public float strafeChangeIntervalMin = 0.8f;
    public float strafeChangeIntervalMax = 2.0f;

    [Header("Attack")]
    public float attackCooldown = 2.0f;
    public float attackChancePerCheck = 0.35f;  // 0..1
    public float attackCheckInterval = 0.25f;   // don't roll RNG every physics tick

    private Rigidbody rb;
    private Animator anim;

    private enum State { Approach, Hold, Attack }
    private State state = State.Approach;

    private float strafeDir = 1f;
    private float nextStrafeChangeTime = 0f;

    private float nextAttackTime = 0f;
    private float nextAttackCheckTime = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError($"No RigidBody on {this.name}");
        }
        anim = GetComponentInChildren<Animator>();
        if (anim == null)
        {
            Debug.LogError($"No Animator on {this.name}");
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - rb.position;
        toPlayer.y = 0f;

        float dist = toPlayer.magnitude;

        Vector3 forward = (toPlayer.sqrMagnitude > 0.0001f) ? toPlayer.normalized : transform.forward;

        // Rotate towards player (yaw only)
        if (state != State.Attack && toPlayer.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(forward);
            Quaternion newRot = Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRot);
        }

        float now = Time.time;

        // ----- State transitions -----
        if (state == State.Attack)
        {
            if (!anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack") && !anim.IsInTransition(0))
            {
                state = State.Hold;
            }
        }
        else
        {
            // Distance control to maintain ONE desired distance band
            float farThreshold = desiredDistance + distanceBuffer;
            float nearThreshold = desiredDistance - distanceBuffer;

            if (dist > farThreshold) state = State.Approach;
            else if (dist < nearThreshold) state = State.Approach; // too close: also "Approach" but we'll move backwards
            else state = State.Hold;

            // Attack only while strafing (Hold), and only on cooldown, and only check periodically
            if (state == State.Hold && now >= nextAttackTime && now >= nextAttackCheckTime)
            {
                nextAttackCheckTime = now + attackCheckInterval;

                if (Random.value < attackChancePerCheck)
                {
                    state = State.Attack;
                    nextAttackTime = now + attackCooldown;

                    if (anim != null)
                        anim.SetTrigger("Attack");
                }
            }
        }

        // ----- Strafing direction updates (Hold only) -----
        if (state == State.Hold && now >= nextStrafeChangeTime)
        {
            strafeDir = (Random.value < 0.5f) ? -1f : 1f;
            float nextIn = Random.Range(strafeChangeIntervalMin, strafeChangeIntervalMax);
            nextStrafeChangeTime = now + nextIn;
        }

        // ----- Movement -----
        Vector3 move = Vector3.zero;

        if (state == State.Approach)
        {
            // If too far: move forward. If too close: move backward.
            float delta = dist - desiredDistance;

            if (delta > distanceBuffer) move = forward * moveSpeed;
            else if (delta < -distanceBuffer) move = -forward * moveSpeed;
            else move = Vector3.zero;
        }
        else if (state == State.Hold)
        {
            Vector3 strafe = Vector3.Cross(Vector3.up, forward) * strafeDir;
            move = strafe * strafeSpeed;
        }
        else if (state == State.Attack)
        {
            // No lunge: stay roughly in place during attack (more realistic for “in-place punch”)
            move = Vector3.zero;
        }

        // Animation speed: only reflect locomotion (not attack)
        if (anim != null)
            anim.SetFloat("Speed", move.magnitude);

        // Apply movement once
        Vector3 newPos = rb.position + move * Time.fixedDeltaTime;

        // Clamp arena bounds (XZ)
        newPos.x = Mathf.Clamp(newPos.x, arenaMin.x, arenaMax.x);
        newPos.z = Mathf.Clamp(newPos.z, arenaMin.y, arenaMax.y);

        rb.MovePosition(newPos);
    }

    public bool CanDealDamageNow()
    {
        if (anim == null) return false;

        return anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack") && !anim.IsInTransition(0);
    }
}