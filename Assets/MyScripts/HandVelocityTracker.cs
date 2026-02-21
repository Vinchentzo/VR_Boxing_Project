using UnityEngine;

public class HandVelocityTracker : MonoBehaviour
{
    public Vector3 Velocity { get; private set; }

    private Vector3 _prevPos;

    void Start()
    {
        _prevPos = transform.position;
    }

    void FixedUpdate()
    {
        // Use FixedUpdate because physics collisions happen on fixed timesteps
        var currentPos = transform.position;
        Velocity = (currentPos - _prevPos) / Time.fixedDeltaTime;
        _prevPos = currentPos;
    }
}
