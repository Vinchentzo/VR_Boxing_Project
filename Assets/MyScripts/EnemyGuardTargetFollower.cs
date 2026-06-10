using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyGuardTargetFollower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform bodyRoot;
    [SerializeField] private Transform head;
    [SerializeField] private Transform punchTarget;

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

    [Header("Right Elbow Jab Motion")]
    [SerializeField] private float rightElbowJabSideAdd = 0.16f;
    [SerializeField] private float rightElbowJabForwardAdd = -0.08f;
    [SerializeField] private float rightElbowJabDownAdd = 0.02f;

    [Header("Left Elbow Hint Offset")]
    [SerializeField] private float leftElbowSideOffset = -0.42f;
    [SerializeField] private float leftElbowForwardOffset = -0.05f;
    [SerializeField] private float leftElbowDownOffset = 0.55f;

    [Header("Debug Right Jab")]
    [SerializeField] private bool autoJab = true;
    [SerializeField] private float autoJabInterval = 2f;
    [SerializeField] private float jabWindupDistance = 0.08f;
    [SerializeField] private float jabDistance = 0.55f;
    [SerializeField] private float jabWindupTime = 0.10f;
    [SerializeField] private float jabExtendTime = 0.18f;
    [SerializeField] private float jabHoldTime = 0.05f;
    [SerializeField] private float jabRetractTime = 0.22f;

    [Header("Smoothing")]
    [SerializeField] private float followSpeed = 18f;

    private float jabTimer;
    private float autoJabTimer;
    private bool jabActive;

    private void Update()
    {
        if (autoJab && !jabActive)
        {
            autoJabTimer += Time.deltaTime;

            if (autoJabTimer >= autoJabInterval)
            {
                StartJab();
                autoJabTimer = 0f;
            }
        }

        if (jabActive)
        {
            jabTimer += Time.deltaTime;

            float totalTime = jabWindupTime + jabExtendTime + jabHoldTime + jabRetractTime;
            if (jabTimer >= totalTime)
            {
                jabActive = false;
                jabTimer = 0f;

            }
        }
    }

    private void StartJab()
    {
        jabActive = true;
        jabTimer = 0f;
    }

    private void LateUpdate()
    {
        if (bodyRoot == null || head == null)
            return;

        Vector3 rightGuardPosition = GetPosition(
            rightSideOffset,
            rightForwardOffset,
            rightDownOffset
        );

        Vector3 leftGuardPosition = GetPosition(
            leftSideOffset,
            leftForwardOffset,
            leftDownOffset
        );

        Vector3 rightTargetPosition = rightGuardPosition;

        if (jabActive)
        {
            Vector3 jabDirection = GetJabDirection(rightGuardPosition);
            Vector3 windupPosition = rightGuardPosition - jabDirection * jabWindupDistance;
            Vector3 jabEndPosition = rightGuardPosition + jabDirection * jabDistance;

            rightTargetPosition = GetJabTargetPosition(
                rightGuardPosition,
                windupPosition,
                jabEndPosition
            );
        }

        MoveTransform(rightGloveTarget, rightTargetPosition);
        MoveTransform(leftGloveTarget, leftGuardPosition);

        float rightElbowJabT = GetRightJabElbowInfluence01();

        MoveTarget(
            rightElbowHint,
            rightElbowSideOffset + rightElbowJabSideAdd * rightElbowJabT,
            rightElbowForwardOffset + rightElbowJabForwardAdd * rightElbowJabT,
            rightElbowDownOffset + rightElbowJabDownAdd * rightElbowJabT
        );

        MoveTarget(
            leftElbowHint,
            leftElbowSideOffset,
            leftElbowForwardOffset,
            leftElbowDownOffset
        );
    }
    private float GetRightJabElbowInfluence01()
    {
        if (!jabActive)
            return 0f;

        if (jabTimer < jabWindupTime)
        {
            float t = jabTimer / jabWindupTime;
            return Smooth01(t);
        }

        if (jabTimer < jabWindupTime + jabExtendTime)
        {
            float t = (jabTimer - jabWindupTime) / jabExtendTime;
            return Mathf.Lerp(1f, 0.35f, Smooth01(t));
        }

        if (jabTimer < jabWindupTime + jabExtendTime + jabHoldTime)
        {
            return 0.35f;
        }

        float retractTimer = jabTimer - jabWindupTime - jabExtendTime - jabHoldTime;
        float retractT = retractTimer / jabRetractTime;

        return Mathf.Lerp(0.35f, 0f, Smooth01(retractT));
    }

    private Vector3 GetJabTargetPosition(
    Vector3 guardPosition,
    Vector3 windupPosition,
    Vector3 jabEndPosition
)
    {
        if (jabTimer < jabWindupTime)
        {
            float t = jabTimer / jabWindupTime;
            t = Smooth01(t);
            return Vector3.Lerp(guardPosition, windupPosition, t);
        }

        if (jabTimer < jabWindupTime + jabExtendTime)
        {
            float t = (jabTimer - jabWindupTime) / jabExtendTime;
            t = Smooth01(t);
            return Vector3.Lerp(windupPosition, jabEndPosition, t);
        }

        if (jabTimer < jabWindupTime + jabExtendTime + jabHoldTime)
        {
            return jabEndPosition;
        }

        float retractTimer = jabTimer - jabWindupTime - jabExtendTime - jabHoldTime;
        float retractT = retractTimer / jabRetractTime;
        retractT = Smooth01(retractT);

        return Vector3.Lerp(jabEndPosition, guardPosition, retractT);
    }

    private float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private Vector3 GetPosition(float sideOffset, float forwardOffset, float downOffset)
    {
        return head.position
               + bodyRoot.right * sideOffset
               + bodyRoot.forward * forwardOffset
               - Vector3.up * downOffset;
    }

    private Vector3 GetJabDirection(Vector3 fromPosition)
    {
        if (punchTarget == null)
            return bodyRoot.forward;

        Vector3 direction = punchTarget.position - fromPosition;

        if (direction.sqrMagnitude < 0.0001f)
            return bodyRoot.forward;

        return direction.normalized;
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

        MoveTransform(target, GetPosition(sideOffset, forwardOffset, downOffset));
    }

    private void MoveTransform(Transform target, Vector3 desiredPosition)
    {
        if (target == null)
            return;

        target.position = Vector3.Lerp(
            target.position,
            desiredPosition,
            1f - Mathf.Exp(-followSpeed * Time.deltaTime)
        );
    }
}