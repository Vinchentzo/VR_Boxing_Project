using UnityEngine;

public class EnemyGuardTargetFollower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform bodyRoot;
    [SerializeField] private Transform head;

    [Header("Glove Targets")]
    [SerializeField] private Transform rightGloveTarget;
    [SerializeField] private Transform leftGloveTarget;

    [Header("Elbow Hint Targets")]
    [SerializeField] private Transform rightElbowHint;
    [SerializeField] private Transform leftElbowHint;

    [Header("Right Guard Offset")]
    [SerializeField] private float rightSideOffset = 0.22f;
    [SerializeField] private float rightForwardOffset = 0.12f;
    [SerializeField] private float rightDownOffset = 0.10f;

    [Header("Left Guard Offset")]
    [SerializeField] private float leftSideOffset = -0.10f;
    [SerializeField] private float leftForwardOffset = 0.22f;
    [SerializeField] private float leftDownOffset = 0.12f;

    [Header("Right Elbow Hint Offset")]
    [SerializeField] private float rightElbowSideOffset = 0.42f;
    [SerializeField] private float rightElbowForwardOffset = -0.05f;
    [SerializeField] private float rightElbowDownOffset = 0.55f;

    [Header("Left Elbow Hint Offset")]
    [SerializeField] private float leftElbowSideOffset = -0.42f;
    [SerializeField] private float leftElbowForwardOffset = -0.05f;
    [SerializeField] private float leftElbowDownOffset = 0.55f;

    [Header("Smoothing")]
    [SerializeField] private float followSpeed = 18f;

    private void LateUpdate()
    {
        if (bodyRoot == null || head == null)
            return;

        MoveTarget(
            rightGloveTarget,
            rightSideOffset,
            rightForwardOffset,
            rightDownOffset
        );

        MoveTarget(
            leftGloveTarget,
            leftSideOffset,
            leftForwardOffset,
            leftDownOffset
        );

        MoveTarget(
            rightElbowHint,
            rightElbowSideOffset,
            rightElbowForwardOffset,
            rightElbowDownOffset
        );

        MoveTarget(
            leftElbowHint,
            leftElbowSideOffset,
            leftElbowForwardOffset,
            leftElbowDownOffset
        );
    }

    private void MoveTarget(
        Transform target,
        float sideOffset,
        float forwardOffset,
        float downOffset
    )
    {
        if (target == null)
            return;

        Vector3 desiredPosition =
            head.position
            + bodyRoot.right * sideOffset
            + bodyRoot.forward * forwardOffset
            - Vector3.up * downOffset;

        //target.position = Vector3.Lerp(
        //    target.position,
        //    desiredPosition,
        //    1f - Mathf.Exp(-followSpeed * Time.deltaTime)
        //);
        target.position = desiredPosition;
    }
}