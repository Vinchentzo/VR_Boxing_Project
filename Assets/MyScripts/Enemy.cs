//using UnityEngine;

//public class Enemy : MonoBehaviour
//{
//    [SerializeField] public Transform player;                 // XR camera transform
//    [SerializeField] private float moveSpeed = 1.2f;
//    [SerializeField] private float stopDistance = 1.8f;        // desired fighting distance
//    [SerializeField] private float retreatDistance = 1.2f;     // too close -> retreat
//    [SerializeField] private float turnSpeed = 360f;           // deg/sec
//    [SerializeField] private Vector2 arenaMin = new Vector2(-300, -300);
//    [SerializeField] private Vector2 arenaMax = new Vector2(100, 100);


//    private CharacterController cc;

//    private enum State { Approach, Hold, Retreat }
//    private State state = State.Approach;

//    void Awake()
//    {
//        cc = GetComponent<CharacterController>();
//    }

//    void Update()
//    {
//        if (player == null) return;

//        // Look at player (yaw only)
//        Vector3 toPlayer = player.position - transform.position;
//        toPlayer.y = 0f;
//        float dist = toPlayer.magnitude;

//        if (toPlayer.sqrMagnitude > 0.0001f)
//        {
//            Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized);
//            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
//        }

//        // State transitions
//        if (dist > stopDistance) state = State.Approach;
//        else if (dist < retreatDistance) state = State.Retreat;
//        else state = State.Hold;

//        // Movement
//        Vector3 move = Vector3.zero;
//        if (state == State.Approach) move = toPlayer.normalized * moveSpeed;
//        else if (state == State.Retreat) move = -toPlayer.normalized * moveSpeed;

//        // CharacterController.Move expects meters/sec * dt
//        cc.Move(move * Time.deltaTime);

//        Vector3 p = transform.position;
//        p.x = Mathf.Clamp(p.x, arenaMin.x, arenaMax.x);
//        p.z = Mathf.Clamp(p.z, arenaMin.y, arenaMax.y);
//        transform.position = p;

//    }
//}


using UnityEngine;

public class Enemy : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 1.2f;
    public float stopDistance = 1.8f;
    public float retreatDistance = 1.2f;
    public float turnSpeed = 360f;
    [SerializeField] private Vector2 arenaMin = new Vector2(0, 0); //set from unity
    [SerializeField] private Vector2 arenaMax = new Vector2(0, 0); //set from unity

    // hysteresis to prevent flip-flopping near thresholds
    public float distanceBuffer = 0.15f;

    private Rigidbody rb;

    private enum State { Approach, Hold, Retreat }
    private State state = State.Approach;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - rb.position;
        toPlayer.y = 0f;

        float dist = toPlayer.magnitude;

        // Smooth rotate (yaw only)
        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized);
            Quaternion newRot = Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRot);
        }

        // State transitions with buffer (prevents jitter)
        if (dist > stopDistance + distanceBuffer) state = State.Approach;
        else if (dist < retreatDistance - distanceBuffer) state = State.Retreat;
        else state = State.Hold;

        Vector3 move = Vector3.zero;
        if (state == State.Approach) move = toPlayer.normalized * moveSpeed;
        else if (state == State.Retreat) move = -toPlayer.normalized * moveSpeed;

        rb.MovePosition(rb.position + move * Time.fixedDeltaTime);

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, arenaMin.x, arenaMax.x);
        p.z = Mathf.Clamp(p.z, arenaMin.y, arenaMax.y);
        transform.position = p;
    }
}