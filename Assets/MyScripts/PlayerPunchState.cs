using UnityEngine;

[RequireComponent(typeof(HandVelocityTracker))]
public class PlayerPunchState : MonoBehaviour
{
    [Header("Punch State")]
    [SerializeField] private float armSpeed = 1.0f;
    [SerializeField] private float resetSpeed = 0.35f;

    private HandVelocityTracker handVelocityTracker;

    private bool punchAvailable;
    private bool waitingForReset;

    public bool IsPunchAvailable => punchAvailable;

    public float CurrentSpeed =>
        handVelocityTracker != null ? handVelocityTracker.Velocity.magnitude : 0f;

    private void Awake()
    {
        handVelocityTracker = GetComponent<HandVelocityTracker>();

        if (handVelocityTracker == null)
        {
            Debug.LogError(
                $"{nameof(PlayerPunchState)} on {name} requires a {nameof(HandVelocityTracker)} on the same GameObject.",
                this
            );

            enabled = false;
            return;
        }
    }

    private void Update()
    {
        float speed = handVelocityTracker.Velocity.magnitude;

        if (waitingForReset)
        {
            punchAvailable = false;

            if (speed <= resetSpeed)
                waitingForReset = false;

            return;
        }

        if (!punchAvailable && speed >= armSpeed)
        {
            punchAvailable = true;
        }
        else if (punchAvailable && speed <= resetSpeed)
        {
            // The hand slowed down before hitting anything, so this punch attempt is cancelled.
            punchAvailable = false;
        }
    }

    public bool TryConsumePunch()
    {
        if (!punchAvailable)
            return false;

        punchAvailable = false;
        waitingForReset = true;

        return true;
    }
}