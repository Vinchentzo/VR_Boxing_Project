//using UnityEngine;

//public class Enemy : MonoBehaviour
//{
//    public Transform player;
//    public float moveSpeed = 1.2f;
//    public float stopDistance = 1.8f;
//    public float retreatDistance = 1.2f;
//    public float turnSpeed = 360f;
//    [SerializeField] private Vector2 arenaMin = new Vector2(0, 0); //set from unity
//    [SerializeField] private Vector2 arenaMax = new Vector2(0, 0); //set from unity

//    // hysteresis to prevent flip-flopping near thresholds
//    public float distanceBuffer = 0.15f;

//    private Rigidbody rb;

//    private enum State { Approach, Hold, Retreat }
//    private State state = State.Approach;

//    void Awake()
//    {
//        rb = GetComponent<Rigidbody>();
//    }

//    void FixedUpdate()
//    {
//        if (player == null) return;

//        Vector3 toPlayer = player.position - rb.position;
//        toPlayer.y = 0f;

//        float dist = toPlayer.magnitude;

//        // Smooth rotate (yaw only)
//        if (toPlayer.sqrMagnitude > 0.0001f)
//        {
//            Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized);
//            Quaternion newRot = Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
//            rb.MoveRotation(newRot);
//        }

//        // State transitions with buffer (prevents jitter)
//        if (dist > stopDistance + distanceBuffer) state = State.Approach;
//        else if (dist < retreatDistance - distanceBuffer) state = State.Retreat;
//        else state = State.Hold;

//        Vector3 move = Vector3.zero;
//        if (state == State.Approach) move = toPlayer.normalized * moveSpeed;
//        else if (state == State.Retreat) move = -toPlayer.normalized * moveSpeed;

//        rb.MovePosition(rb.position + move * Time.fixedDeltaTime);

//        Vector3 p = rb.position;
//        p.x = Mathf.Clamp(p.x, arenaMin.x, arenaMax.x);
//        p.z = Mathf.Clamp(p.z, arenaMin.y, arenaMax.y);
//        rb.MovePosition(p);
//    }
//}

//using UnityEngine;

//public class Enemy : MonoBehaviour
//{
//    public Transform player;

//    [Header("Movement")]
//    public float moveSpeed = 1.2f;
//    public float stopDistance = 1.8f;
//    public float retreatDistance = 1.2f;
//    public float turnSpeed = 360f;
//    public float distanceBuffer = 0.15f;

//    [Header("Arena Bounds (XZ)")]
//    [SerializeField] private Vector2 arenaMin = new Vector2(-3, -3);
//    [SerializeField] private Vector2 arenaMax = new Vector2(3, 3);

//    [Header("Strafing")]
//    public float strafeSpeed = 0.8f;
//    public float strafeChangeIntervalMin = 0.8f;
//    public float strafeChangeIntervalMax = 2.0f;

//    [Header("Attack")]
//    public float attackRangeMin = 1.2f;
//    public float attackRangeMax = 2.0f;
//    public float attackCooldown = 2.0f;
//    public float attackChancePerCheck = 0.35f;   // 0..1
//    public float attackLungeSpeed = 3.0f;
//    public float attackDuration = 0.25f;
//    public float recoverDuration = 0.35f;
//    public float recoverBackoffSpeed = 1.5f;

//    private Rigidbody rb;

//    private enum State { Approach, Hold, Retreat, Attack, Recover }
//    private State state = State.Approach;

//    private float strafeDir = 1f;
//    private float nextStrafeChangeTime = 0f;

//    private float nextAttackTime = 0f;
//    private float stateEndTime = 0f;

//    private Animator anim;
//    private Vector3 lastMove;  // store movement vector we computed

//    void Awake()
//    {
//        rb = GetComponent<Rigidbody>();
//        anim = GetComponentInChildren<Animator>();
//    }

//    void FixedUpdate()
//    {
//        if (player == null) return;

//        Vector3 toPlayer = player.position - rb.position;
//        toPlayer.y = 0f;

//        float dist = toPlayer.magnitude;

//        // Define "forward" safely
//        Vector3 forward = (toPlayer.sqrMagnitude > 0.0001f) ? toPlayer.normalized : transform.forward;

//        // Smooth rotate (yaw only)
//        if (toPlayer.sqrMagnitude > 0.0001f)
//        {
//            Quaternion targetRot = Quaternion.LookRotation(forward);
//            Quaternion newRot = Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
//            rb.MoveRotation(newRot);
//        }

//        float now = Time.time;

//        // --- State machine ---
//        // If currently in timed states, let them finish
//        if (state == State.Attack)
//        {
//            if (now >= stateEndTime) { state = State.Recover; stateEndTime = now + recoverDuration; }
//        }
//        else if (state == State.Recover)
//        {
//            if (now >= stateEndTime) state = State.Hold;
//        }
//        else
//        {
//            // Decide whether to attack (only when in a reasonable range, and cooldown passed)
//            if (now >= nextAttackTime && dist >= attackRangeMin && dist <= attackRangeMax)
//            {
//                // Don’t decide every physics tick—make it probabilistic
//                if (Random.value < attackChancePerCheck)
//                {
//                    state = State.Attack;
//                    stateEndTime = now + attackDuration;
//                    nextAttackTime = now + attackCooldown;
//                    if (anim != null)
//                        anim.SetTrigger("Attack");
//                }
//            }

//            // If not attacking, do distance control
//            if (state != State.Attack)
//            {
//                if (dist > stopDistance + distanceBuffer) state = State.Approach;
//                else if (dist < retreatDistance - distanceBuffer) state = State.Retreat;
//                else state = State.Hold;
//            }
//        }

//        // --- Strafing control (only in Hold) ---
//        if (state == State.Hold && now >= nextStrafeChangeTime)
//        {
//            strafeDir = (Random.value < 0.5f) ? -1f : 1f;
//            float nextIn = Random.Range(strafeChangeIntervalMin, strafeChangeIntervalMax);
//            nextStrafeChangeTime = now + nextIn;
//        }

//        // --- Movement vector ---
//        Vector3 move = Vector3.zero;

//        if (state == State.Approach)
//        {
//            move = forward * moveSpeed;
//        }
//        else if (state == State.Retreat)
//        {
//            move = -forward * moveSpeed;
//        }
//        else if (state == State.Hold)
//        {
//            // Strafe around player on ground plane
//            Vector3 strafe = Vector3.Cross(Vector3.up, forward) * strafeDir;
//            move = strafe * strafeSpeed;
//        }
//        else if (state == State.Attack)
//        {
//            // Lunge towards player
//            move = forward * attackLungeSpeed;
//        }
//        else if (state == State.Recover)
//        {
//            // Back off slightly after attack
//            move = -forward * recoverBackoffSpeed;
//        }

//        // Apply movement ONCE
//        Vector3 newPos = rb.position + move * Time.fixedDeltaTime;


//        if (anim != null)
//            anim.SetFloat("Speed", move.magnitude);

//        // Clamp in arena bounds (XZ)
//        newPos.x = Mathf.Clamp(newPos.x, arenaMin.x, arenaMax.x);
//        newPos.z = Mathf.Clamp(newPos.z, arenaMin.y, arenaMax.y);

//        rb.MovePosition(newPos);
//    }
//}



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
    [SerializeField] private Vector2 arenaMin = new Vector2(-3, -3);
    [SerializeField] private Vector2 arenaMax = new Vector2(3, 3);

    [Header("Strafing")]
    public float strafeSpeed = 0.8f;
    public float strafeChangeIntervalMin = 0.8f;
    public float strafeChangeIntervalMax = 2.0f;

    [Header("Attack")]
    public float attackCooldown = 2.0f;
    public float attackChancePerCheck = 0.35f;  // 0..1
    public float attackDuration = 0.6f;         // how long we stay in Attack state
    public float attackCheckInterval = 0.25f;   // don't roll RNG every physics tick

    private Rigidbody rb;
    private Animator anim;

    private enum State { Approach, Hold, Attack }
    private State state = State.Approach;

    private float strafeDir = 1f;
    private float nextStrafeChangeTime = 0f;

    private float nextAttackTime = 0f;
    private float attackEndTime = 0f;
    private float nextAttackCheckTime = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - rb.position;
        toPlayer.y = 0f;

        float dist = toPlayer.magnitude;

        Vector3 forward = (toPlayer.sqrMagnitude > 0.0001f) ? toPlayer.normalized : transform.forward;

        // Rotate towards player (yaw only)
        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(forward);
            Quaternion newRot = Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRot);
        }

        float now = Time.time;

        // ----- State transitions -----
        if (state == State.Attack)
        {
            if (now >= attackEndTime)
                state = State.Hold; // after attack, go back to strafing/holding distance
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
                    attackEndTime = now + attackDuration;
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

            // Optional: tiny strafe during attack so it doesn't look frozen:
            // Vector3 strafe = Vector3.Cross(Vector3.up, forward) * strafeDir;
            // move = strafe * (strafeSpeed * 0.2f);
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
}