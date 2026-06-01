using UnityEngine;

[DisallowMultipleComponent]
public class HandVelocityTracker : MonoBehaviour
{
    public Vector3 Velocity { get; private set; }

    private Vector3 previousPosition;

    private void OnEnable()
    {
        ResetVelocity();
    }

    private void FixedUpdate()
    {
        Vector3 currentPosition = transform.position;

        Velocity = (currentPosition - previousPosition) / Time.fixedDeltaTime;
        previousPosition = currentPosition;
    }

    private void ResetVelocity()
    {
        previousPosition = transform.position;
        Velocity = Vector3.zero;
    }
}