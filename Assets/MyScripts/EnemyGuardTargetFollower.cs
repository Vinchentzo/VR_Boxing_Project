using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyGuardTargetFollower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform bodyRoot;
    [SerializeField] private Transform head;
    [SerializeField] private Transform punchTarget;

    [Header("Animation Sync")]
    [SerializeField] private Animator animator;
    [SerializeField] private string jabAnimationTrigger = "";

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

    [Header("Left Elbow Jab Motion")]
    [SerializeField] private float leftElbowJabSideAdd = -0.16f;
    [SerializeField] private float leftElbowJabForwardAdd = -0.08f;
    [SerializeField] private float leftElbowJabDownAdd = 0.02f;

    [Header("Left Elbow Hint Offset")]
    [SerializeField] private float leftElbowSideOffset = -0.42f;
    [SerializeField] private float leftElbowForwardOffset = -0.05f;
    [SerializeField] private float leftElbowDownOffset = 0.55f;

    [Header("Debug Right Jab")]
    [SerializeField] private bool autoJab = true;
    [SerializeField] private float autoJabInterval = 2f;
    [SerializeField] private float minJabDistance = 0.35f;
    [SerializeField] private float maxJabDistance = 1.8f;
    [SerializeField] private float jabWindupDistance = 0.08f;
    [SerializeField] private float jabDistance = 0.55f;
    [SerializeField] private float jabWindupTime = 0.10f;
    [SerializeField] private float jabExtendTime = 0.18f;
    [SerializeField] private float jabHoldTime = 0.05f;
    [SerializeField] private float jabRetractTime = 0.22f;

    [Header("Jab Collision Stop")]
    [SerializeField] private bool stopJabOnCollision = true;
    [SerializeField] private LayerMask jabBlockMask;
    [SerializeField] private float jabGloveRadius = 0.10f;
    [SerializeField] private float jabCollisionSkin = 0.02f;

    [Header("Jab Contact Classification")]
    [SerializeField] private CombatContactResolver contactResolver;
    [SerializeField] private bool logJabSurface = true;

    [Header("Jab Damage")]
    [SerializeField] private float jabBaseDamage = 5f;
    [SerializeField] private bool applyJabDamage = true;

    [Header("Smoothing")]
    [SerializeField] private float followSpeed = 18f;

    private float jabTimer;
    private float autoJabTimer;
    private bool jabActive;
    private bool jabBlocked;
    private Vector3 jabBlockedPosition;
    private Vector3 lockedJabDirection;

    private void Update()
    {
        if (autoJab && !jabActive)
        {
            autoJabTimer += Time.deltaTime;

            if (autoJabTimer >= autoJabInterval)
            {
                if (TryStartJab())
                {
                    autoJabTimer = 0f;
                }
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

    public bool TryStartJab()
    {
        if (jabActive)
            return false;

        if (!IsPunchTargetInJabRange())
            return false;

        StartJab();
        return true;
    }

    private void StartJab()
    {
        Vector3 leftGuardPosition = GetPosition(
            leftSideOffset,
            leftForwardOffset,
            leftDownOffset
        );

        lockedJabDirection = GetJabDirection(leftGuardPosition);

        jabActive = true;
        jabBlocked = false;
        jabTimer = 0f;

        if (animator != null && !string.IsNullOrWhiteSpace(jabAnimationTrigger))
        {
            animator.SetTrigger(jabAnimationTrigger);
        }
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
        Vector3 leftTargetPosition = leftGuardPosition;

        if (jabActive)
        {
            Vector3 jabDirection = lockedJabDirection;
            Vector3 windupPosition = leftGuardPosition - jabDirection * jabWindupDistance;
            Vector3 jabEndPosition = leftGuardPosition + jabDirection * jabDistance;

            leftTargetPosition = GetJabTargetPosition(
                leftGuardPosition,
                windupPosition,
                jabEndPosition
            );

            float retractStartTime = jabWindupTime + jabExtendTime + jabHoldTime;

            if (jabBlocked)
            {
                if (jabTimer < retractStartTime)
                {
                    leftTargetPosition = jabBlockedPosition;
                }
                else
                {
                    float retractT = (jabTimer - retractStartTime) / jabRetractTime;
                    retractT = Smooth01(retractT);
                    leftTargetPosition = Vector3.Lerp(jabBlockedPosition, leftGuardPosition, retractT);
                }
            }
            else if (jabTimer >= jabWindupTime && jabTimer < retractStartTime)
            {
                leftTargetPosition = LimitJabByCollision(
                    leftGloveTarget.position,
                    leftTargetPosition,
                    out bool blocked
                );

                if (blocked)
                {
                    jabBlocked = true;
                    jabBlockedPosition = leftTargetPosition;
                }
            }
        }

        MoveTransform(rightGloveTarget, rightTargetPosition);
        MoveTransform(leftGloveTarget, leftTargetPosition);

        float jabElbowT = GetJabElbowInfluence01();

        MoveTarget(
            rightElbowHint,
            rightElbowSideOffset,
            rightElbowForwardOffset,
            rightElbowDownOffset
        );

        MoveTarget(
            leftElbowHint,
            leftElbowSideOffset + leftElbowJabSideAdd * jabElbowT,
            leftElbowForwardOffset + leftElbowJabForwardAdd * jabElbowT,
            leftElbowDownOffset + leftElbowJabDownAdd * jabElbowT
        );
    }

    private Vector3 LimitJabByCollision(
    Vector3 currentPosition,
    Vector3 desiredPosition,
    out bool blocked
)
    {
        blocked = false;

        if (!stopJabOnCollision || jabBlockMask.value == 0)
            return desiredPosition;

        Vector3 movement = desiredPosition - currentPosition;
        float distance = movement.magnitude;

        if (distance < 0.0001f)
            return desiredPosition;

        Vector3 direction = movement / distance;

        if (Physics.SphereCast(
                currentPosition,
                jabGloveRadius,
                direction,
                out RaycastHit hit,
                distance,
                jabBlockMask,
                QueryTriggerInteraction.Collide
            ))
        {
            Debug.Log(
                $"Enemy jab blocked by: {hit.collider.name}, layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}, distance={hit.distance:F3}",
                hit.collider
            );

            LogJabSurface(hit.collider);

            blocked = true;

            float safeDistance = Mathf.Max(0f, hit.distance - jabCollisionSkin);
            return currentPosition + direction * safeDistance;
        }

        return desiredPosition;
    }


    private float GetJabElbowInfluence01()
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

    private void LogJabSurface(Collider hitCollider)
    {
        if (!logJabSurface || hitCollider == null)
            return;

        CombatSurface surface = hitCollider.GetComponentInParent<CombatSurface>();

        if (surface == null)
        {
            Debug.Log(
                $"Enemy jab hit {hitCollider.name}, but it has no CombatSurface.",
                hitCollider
            );
            return;
        }

        CombatContactResult result = CombatContactResult.None;

        if (contactResolver != null)
        {
            result = contactResolver.ResolveEnemyGloveContact(surface);
        }

        float appliedDamage = 0f;

        if (applyJabDamage &&
            contactResolver != null &&
            surface.OwnerHealth != null)
        {
            appliedDamage = contactResolver.CalculateEnemyPunchDamage(
                result,
                surface,
                jabBaseDamage
            );

            if (appliedDamage > 0f)
            {
                surface.OwnerHealth.TakeDamage(appliedDamage);
            }
        }

        Debug.Log(
            $"Enemy jab classified as {result}. Surface={surface.SurfaceType}, Side={surface.Side}, Collider={hitCollider.name}, Damage={appliedDamage:F1}",
            hitCollider
        );
    }

        private bool IsPunchTargetInJabRange()
    {
        if (punchTarget == null || bodyRoot == null)
            return false;

        Vector3 toTarget = punchTarget.position - bodyRoot.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        return distance >= minJabDistance && distance <= maxJabDistance;
    }
}