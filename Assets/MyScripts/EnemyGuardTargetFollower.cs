using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyGuardTargetFollower : MonoBehaviour
{
    private enum AttackType
    {
        None,
        LeftJab,
        RightCross
    }

    [Header("References")]
    [SerializeField] private Transform bodyRoot;
    [SerializeField] private Transform head;
    [SerializeField] private Transform punchTarget;

    [Header("Animation Sync")]
    [SerializeField] private Animator animator;
    [SerializeField] private string jabAnimationTrigger = "";
    [SerializeField] private string crossAnimationTrigger = "";

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

    [Header("Right Elbow Cross Motion")]
    [SerializeField] private float rightElbowCrossSideAdd = 0.18f;
    [SerializeField] private float rightElbowCrossForwardAdd = -0.10f;
    [SerializeField] private float rightElbowCrossDownAdd = 0.02f;

    [Header("Left Elbow Jab Motion")]
    [SerializeField] private float leftElbowJabSideAdd = -0.16f;
    [SerializeField] private float leftElbowJabForwardAdd = -0.08f;
    [SerializeField] private float leftElbowJabDownAdd = 0.02f;

    [Header("Left Elbow Hint Offset")]
    [SerializeField] private float leftElbowSideOffset = -0.42f;
    [SerializeField] private float leftElbowForwardOffset = -0.05f;
    [SerializeField] private float leftElbowDownOffset = 0.55f;

    [Header("Left Jab")]
    [SerializeField] private float minJabDistance = 0.35f;
    [SerializeField] private float maxJabDistance = 1.8f;
    [SerializeField] private float jabWindupDistance = 0.08f;
    [SerializeField] private float jabDistance = 0.55f;
    [SerializeField] private float jabWindupTime = 0.10f;
    [SerializeField] private float jabExtendTime = 0.18f;
    [SerializeField] private float jabHoldTime = 0.05f;
    [SerializeField] private float jabRetractTime = 0.22f;

    [Header("Right Cross")]
    [SerializeField] private float minCrossDistance = 0.45f;
    [SerializeField] private float maxCrossDistance = 1.0f;
    [SerializeField] private float crossWindupDistance = 0.10f;
    [SerializeField] private float crossDistance = 0.70f;
    [SerializeField] private float crossWindupTime = 0.14f;
    [SerializeField] private float crossExtendTime = 0.24f;
    [SerializeField] private float crossHoldTime = 0.05f;
    [SerializeField] private float crossRetractTime = 0.28f;
    [SerializeField] private float crossBaseDamage = 8f;

    [Header("Punch Collision Stop")]
    [SerializeField] private bool stopJabOnCollision = true;
    [SerializeField] private LayerMask punchBlockMask;
    [SerializeField] private float punchGloveRadius = 0.10f;
    [SerializeField] private float punchCollisionSkin = 0.02f;
    [SerializeField] private bool useGloveCapsuleCasts = true;

    [Header("Punch Contact Classification")]
    [SerializeField] private CombatContactResolver contactResolver;
    [SerializeField] private bool logJabSurface = true;

    [Header("Punch Damage")]
    [SerializeField] private float jabBaseDamage = 5f;
    [SerializeField] private bool applyJabDamage = true;

    [Header("Smoothing")]
    [SerializeField] private float followSpeed = 18f;

    [Header("Glove Rotation")]
    [SerializeField] private bool controlGloveRotation = true;
    [SerializeField] private Vector3 rightGuardRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 leftGuardRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 rightPunchRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 leftPunchRotationOffset = Vector3.zero;

    [Header("Enemy Glove Shape Colliders")]
    [SerializeField] private Transform leftGloveHitboxRoot;
    [SerializeField] private Transform rightGloveHitboxRoot;

    private float attackTimer;
    private bool attackActive;
    private bool attackBlocked;
    private Vector3 attackBlockedPosition;
    private Vector3 lockedAttackDirection;
    private AttackType currentAttack = AttackType.None;
    private float attackBlockedTime;
    private float attackBlockedRotationInfluence;
    private CapsuleCollider[] leftGloveCapsules;
    private CapsuleCollider[] rightGloveCapsules;

    private void Awake()
    {
        CacheGloveCapsules();
    }

    private void CacheGloveCapsules()
    {
        leftGloveCapsules = leftGloveHitboxRoot != null
            ? leftGloveHitboxRoot.GetComponentsInChildren<CapsuleCollider>(true)
            : System.Array.Empty<CapsuleCollider>();

        rightGloveCapsules = rightGloveHitboxRoot != null
            ? rightGloveHitboxRoot.GetComponentsInChildren<CapsuleCollider>(true)
            : System.Array.Empty<CapsuleCollider>();

        Debug.Log(
            $"Enemy glove capsules cached: left={leftGloveCapsules.Length}, right={rightGloveCapsules.Length}",
            this
        );
    }

    private void Update()
    {
        if (attackActive)
        {
            attackTimer += Time.deltaTime;

            float totalTime = GetCurrentAttackTotalTime();

            if (attackTimer >= totalTime)
            {
                FinishAttack();
            }
        }
    }

    public bool CanStartLeftJab()
    {
        return !attackActive && IsPunchTargetInJabRange();
    }

    public bool TryStartLeftJab()
    {
        if (!CanStartLeftJab())
            return false;

        StartLeftJab();
        return true;
    }

    public bool CanStartRightCross()
    {
        return !attackActive && IsPunchTargetInCrossRange();
    }

    public bool TryStartRightCross()
    {
        if (!CanStartRightCross())
            return false;

        StartRightCross();
        return true;
    }

    private void StartRightCross()
    {
        Vector3 rightGuardPosition = GetPosition(
            rightSideOffset,
            rightForwardOffset,
            rightDownOffset
        );

        lockedAttackDirection = GetJabDirection(rightGuardPosition);

        attackActive = true;
        attackBlocked = false;
        attackTimer = 0f;
        currentAttack = AttackType.RightCross;
        attackBlockedTime = 0f;
        attackBlockedRotationInfluence = 0f;

        if (animator != null && !string.IsNullOrWhiteSpace(crossAnimationTrigger))
        {
            animator.SetTrigger(crossAnimationTrigger);
        }
    }

    private bool IsPunchTargetInCrossRange()
    {
        if (punchTarget == null || bodyRoot == null)
            return false;

        Vector3 toTarget = punchTarget.position - bodyRoot.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        return distance >= minCrossDistance && distance <= maxCrossDistance;
    }

    private void StartLeftJab()
    {
        Vector3 leftGuardPosition = GetPosition(
            leftSideOffset,
            leftForwardOffset,
            leftDownOffset
        );

        lockedAttackDirection = GetJabDirection(leftGuardPosition);

        attackActive = true;
        attackBlocked = false;
        attackTimer = 0f;
        currentAttack = AttackType.LeftJab;
        attackBlockedTime = 0f;
        attackBlockedRotationInfluence = 0f;

        if (animator != null && !string.IsNullOrWhiteSpace(jabAnimationTrigger))
        {
            animator.SetTrigger(jabAnimationTrigger);
        }
    }

    private void FinishAttack()
    {
        attackActive = false;
        attackBlocked = false;
        attackTimer = 0f;
        currentAttack = AttackType.None;
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

        if (attackActive && currentAttack == AttackType.LeftJab)
        {
            Vector3 jabDirection = lockedAttackDirection;
            Vector3 windupPosition = leftGuardPosition - jabDirection * jabWindupDistance;
            Vector3 jabEndPosition = leftGuardPosition + jabDirection * jabDistance;

            leftTargetPosition = GetJabTargetPosition(
                leftGuardPosition,
                windupPosition,
                jabEndPosition
            );

            float retractStartTime = jabWindupTime + jabExtendTime + jabHoldTime;

            if (attackBlocked)
            {
                if (attackTimer < retractStartTime)
                {
                    leftTargetPosition = attackBlockedPosition;
                }
                else
                {
                    float retractT = (attackTimer - retractStartTime) / jabRetractTime;
                    retractT = Smooth01(retractT);
                    leftTargetPosition = Vector3.Lerp(attackBlockedPosition, leftGuardPosition, retractT);
                }
            }
            else if (attackTimer >= jabWindupTime && attackTimer < retractStartTime)
            {
                leftTargetPosition = LimitPunchByCollision(
                    leftGloveTarget.position,
                    leftTargetPosition,
                    out bool blocked
                );

                if (blocked)
                {
                    attackBlocked = true;
                    attackBlockedTime = attackTimer;
                    attackBlockedRotationInfluence = GetPlannedAttackRotationInfluence01();
                    attackBlockedPosition = leftTargetPosition;
                }
            }
        }

        if (attackActive && currentAttack == AttackType.RightCross)
        {
            Vector3 crossDirection = lockedAttackDirection;
            Vector3 windupPosition = rightGuardPosition - crossDirection * crossWindupDistance;
            Vector3 crossEndPosition = rightGuardPosition + crossDirection * crossDistance;

            rightTargetPosition = GetPunchTargetPosition(
                rightGuardPosition,
                windupPosition,
                crossEndPosition,
                crossWindupTime,
                crossExtendTime,
                crossHoldTime,
                crossRetractTime
            );

            float retractStartTime = crossWindupTime + crossExtendTime + crossHoldTime;

            if (attackBlocked)
            {
                if (attackTimer < retractStartTime)
                {
                    rightTargetPosition = attackBlockedPosition;
                }
                else
                {
                    float retractT = (attackTimer - retractStartTime) / crossRetractTime;
                    retractT = Smooth01(retractT);
                    rightTargetPosition = Vector3.Lerp(attackBlockedPosition, rightGuardPosition, retractT);
                }
            }
            else if (attackTimer >= crossWindupTime && attackTimer < retractStartTime)
            {
                rightTargetPosition = LimitPunchByCollision(
                    rightGloveTarget.position,
                    rightTargetPosition,
                    out bool blocked
                );

                if (blocked)
                {
                    attackBlocked = true;
                    attackBlockedTime = attackTimer;
                    attackBlockedRotationInfluence = GetPlannedAttackRotationInfluence01();
                    attackBlockedPosition = rightTargetPosition;
                }
            }
        }

        MoveTransform(rightGloveTarget, rightTargetPosition);
        MoveTransform(leftGloveTarget, leftTargetPosition);

        float rotationInfluence = GetAttackRotationInfluence01();

        float rightPunchRotationInfluence = 0f;
        float leftPunchRotationInfluence = 0f;

        Vector3 rightPunchDirection = bodyRoot.forward;
        Vector3 leftPunchDirection = bodyRoot.forward;

        if (attackActive && currentAttack == AttackType.LeftJab)
        {
            leftPunchRotationInfluence = rotationInfluence;
            leftPunchDirection = lockedAttackDirection;
        }
        else if (attackActive && currentAttack == AttackType.RightCross)
        {
            rightPunchRotationInfluence = rotationInfluence;
            rightPunchDirection = lockedAttackDirection;
        }

        SetGloveRotation(
            rightGloveTarget,
            bodyRoot.forward,
            rightPunchDirection,
            rightGuardRotationOffset,
            rightPunchRotationOffset,
            rightPunchRotationInfluence
        );

        SetGloveRotation(
            leftGloveTarget,
            bodyRoot.forward,
            leftPunchDirection,
            leftGuardRotationOffset,
            leftPunchRotationOffset,
            leftPunchRotationInfluence
        );

        float jabElbowT = GetJabElbowInfluence01();
        float crossElbowT = GetCrossElbowInfluence01();

        MoveTarget(
            rightElbowHint,
            rightElbowSideOffset + rightElbowCrossSideAdd * crossElbowT,
            rightElbowForwardOffset + rightElbowCrossForwardAdd * crossElbowT,
            rightElbowDownOffset + rightElbowCrossDownAdd * crossElbowT
        );

        MoveTarget(
            leftElbowHint,
            leftElbowSideOffset + leftElbowJabSideAdd * jabElbowT,
            leftElbowForwardOffset + leftElbowJabForwardAdd * jabElbowT,
            leftElbowDownOffset + leftElbowJabDownAdd * jabElbowT
        );
    }

    private Vector3 GetPunchTargetPosition(
    Vector3 guardPosition,
    Vector3 windupPosition,
    Vector3 punchEndPosition,
    float windupTime,
    float extendTime,
    float holdTime,
    float retractTime
)
    {
        if (attackTimer < windupTime)
        {
            float t = attackTimer / windupTime;
            t = Smooth01(t);
            return Vector3.Lerp(guardPosition, windupPosition, t);
        }

        if (attackTimer < windupTime + extendTime)
        {
            float t = (attackTimer - windupTime) / extendTime;
            t = Smooth01(t);
            return Vector3.Lerp(windupPosition, punchEndPosition, t);
        }

        if (attackTimer < windupTime + extendTime + holdTime)
        {
            return punchEndPosition;
        }

        float retractTimer = attackTimer - windupTime - extendTime - holdTime;
        float retractT = retractTimer / retractTime;
        retractT = Smooth01(retractT);

        return Vector3.Lerp(punchEndPosition, guardPosition, retractT);
    }

    private Vector3 LimitPunchByCollision(
    Vector3 currentPosition,
    Vector3 desiredPosition,
    out bool blocked
)
    {
        blocked = false;

        if (!stopJabOnCollision || punchBlockMask.value == 0)
            return desiredPosition;

        Vector3 movement = desiredPosition - currentPosition;
        float distance = movement.magnitude;

        if (distance < 0.0001f)
            return desiredPosition;

        Vector3 direction = movement / distance;

        CapsuleCollider[] capsules = GetCurrentAttackGloveCapsules();
        bool hasCapsules = HasUsableCapsules(capsules);

        if (useGloveCapsuleCasts && hasCapsules)
        {
            if (TryGloveCapsuleCast(direction, distance, out RaycastHit capsuleHit))
            {
                Debug.Log(
                    $"Enemy {currentAttack} capsule blocked by: {capsuleHit.collider.name}, layer={LayerMask.LayerToName(capsuleHit.collider.gameObject.layer)}, distance={capsuleHit.distance:F3}",
                    capsuleHit.collider
                );

                ResolvePunchSurface(capsuleHit.collider);

                blocked = true;

                float safeDistance = Mathf.Max(0f, capsuleHit.distance - punchCollisionSkin);
                return currentPosition + direction * safeDistance;
            }

            // Important:
            // We had usable capsule colliders, but they missed.
            // So do NOT fall back to SphereCast.
            return desiredPosition;
        }

        // Only use SphereCast when capsule casts are disabled,
        // or when this attack has no usable capsule colliders.
        if (Physics.SphereCast(
                currentPosition,
                punchGloveRadius,
                direction,
                out RaycastHit sphereHit,
                distance,
                punchBlockMask,
                QueryTriggerInteraction.Collide
            ))
        {
            Debug.Log(
                $"Enemy {currentAttack} sphere blocked by: {sphereHit.collider.name}, layer={LayerMask.LayerToName(sphereHit.collider.gameObject.layer)}, distance={sphereHit.distance:F3}",
                sphereHit.collider
            );

            ResolvePunchSurface(sphereHit.collider);

            blocked = true;

            float safeDistance = Mathf.Max(0f, sphereHit.distance - punchCollisionSkin);
            return currentPosition + direction * safeDistance;
        }

        return desiredPosition;
    }


    private bool HasUsableCapsules(CapsuleCollider[] capsules)
    {
        if (capsules == null || capsules.Length == 0)
            return false;

        foreach (CapsuleCollider capsule in capsules)
        {
            if (capsule != null && capsule.enabled && capsule.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

    private bool TryGloveCapsuleCast(
    Vector3 direction,
    float distance,
    out RaycastHit bestHit
)
    {
        bestHit = default;

        CapsuleCollider[] capsules = GetCurrentAttackGloveCapsules();

        if (capsules == null || capsules.Length == 0)
            return false;

        bool foundHit = false;
        float bestDistance = float.PositiveInfinity;

        foreach (CapsuleCollider capsule in capsules)
        {
            if (capsule == null || !capsule.enabled)
                continue;

            GetWorldCapsule(capsule, out Vector3 point1, out Vector3 point2, out float radius);

            if (Physics.CapsuleCast(
                    point1,
                    point2,
                    radius,
                    direction,
                    out RaycastHit hit,
                    distance,
                    punchBlockMask,
                    QueryTriggerInteraction.Collide
                ))
            {
                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestHit = hit;
                    foundHit = true;
                }
            }
        }

        return foundHit;
    }

    private CapsuleCollider[] GetCurrentAttackGloveCapsules()
    {
        switch (currentAttack)
        {
            case AttackType.LeftJab:
                return leftGloveCapsules;

            case AttackType.RightCross:
                return rightGloveCapsules;

            default:
                return null;
        }
    }

    private void GetWorldCapsule(
        CapsuleCollider capsule,
        out Vector3 point1,
        out Vector3 point2,
        out float radius
    )
    {
        Transform t = capsule.transform;

        Vector3 center = t.TransformPoint(capsule.center);

        Vector3 lossyScale = t.lossyScale;

        Vector3 axis;
        float heightScale;
        float radiusScale;

        switch (capsule.direction)
        {
            case 0: // X axis
                axis = t.right;
                heightScale = Mathf.Abs(lossyScale.x);
                radiusScale = Mathf.Max(Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z));
                break;

            case 1: // Y axis
                axis = t.up;
                heightScale = Mathf.Abs(lossyScale.y);
                radiusScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.z));
                break;

            case 2: // Z axis
                axis = t.forward;
                heightScale = Mathf.Abs(lossyScale.z);
                radiusScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));
                break;

            default:
                axis = t.up;
                heightScale = Mathf.Abs(lossyScale.y);
                radiusScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.z));
                break;
        }

        radius = capsule.radius * radiusScale;

        float height = Mathf.Max(capsule.height * heightScale, radius * 2f);
        float cylinderLength = Mathf.Max(0f, height - radius * 2f);

        point1 = center + axis.normalized * (cylinderLength * 0.5f);
        point2 = center - axis.normalized * (cylinderLength * 0.5f);
    }


    private float GetJabElbowInfluence01()
    {
        if (!attackActive)
            return 0f;

        if (attackTimer < jabWindupTime)
        {
            float t = attackTimer / jabWindupTime;
            return Smooth01(t);
        }

        if (attackTimer < jabWindupTime + jabExtendTime)
        {
            float t = (attackTimer - jabWindupTime) / jabExtendTime;
            return Mathf.Lerp(1f, 0.35f, Smooth01(t));
        }

        if (attackTimer < jabWindupTime + jabExtendTime + jabHoldTime)
        {
            return 0.35f;
        }

        float retractTimer = attackTimer - jabWindupTime - jabExtendTime - jabHoldTime;
        float retractT = retractTimer / jabRetractTime;

        return Mathf.Lerp(0.35f, 0f, Smooth01(retractT));
    }

    private Vector3 GetJabTargetPosition(
    Vector3 guardPosition,
    Vector3 windupPosition,
    Vector3 jabEndPosition
)
    {
        if (attackTimer < jabWindupTime)
        {
            float t = attackTimer / jabWindupTime;
            t = Smooth01(t);
            return Vector3.Lerp(guardPosition, windupPosition, t);
        }

        if (attackTimer < jabWindupTime + jabExtendTime)
        {
            float t = (attackTimer - jabWindupTime) / jabExtendTime;
            t = Smooth01(t);
            return Vector3.Lerp(windupPosition, jabEndPosition, t);
        }

        if (attackTimer < jabWindupTime + jabExtendTime + jabHoldTime)
        {
            return jabEndPosition;
        }

        float retractTimer = attackTimer - jabWindupTime - jabExtendTime - jabHoldTime;
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

    private void ResolvePunchSurface(Collider hitCollider)
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
                GetCurrentAttackBaseDamage()
            );

            if (appliedDamage > 0f)
            {
                surface.OwnerHealth.TakeDamage(appliedDamage);
            }
        }

        Debug.Log(
            $"Enemy {currentAttack} classified as {result}. Surface={surface.SurfaceType}, Side={surface.Side}, Collider={hitCollider.name}, Damage={appliedDamage:F1}",
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

    private float GetCurrentAttackTotalTime()
    {
        switch (currentAttack)
        {
            case AttackType.LeftJab:
                return jabWindupTime + jabExtendTime + jabHoldTime + jabRetractTime;

            case AttackType.RightCross:
                return crossWindupTime + crossExtendTime + crossHoldTime + crossRetractTime;

            default:
                return 0f;
        }
    }

    private float GetCrossElbowInfluence01()
    {
        if (!attackActive || currentAttack != AttackType.RightCross)
            return 0f;

        if (attackTimer < crossWindupTime)
        {
            float t = attackTimer / crossWindupTime;
            return Smooth01(t);
        }

        if (attackTimer < crossWindupTime + crossExtendTime)
        {
            float t = (attackTimer - crossWindupTime) / crossExtendTime;
            return Mathf.Lerp(1f, 0.35f, Smooth01(t));
        }

        if (attackTimer < crossWindupTime + crossExtendTime + crossHoldTime)
        {
            return 0.35f;
        }

        float retractTimer = attackTimer - crossWindupTime - crossExtendTime - crossHoldTime;
        float retractT = retractTimer / crossRetractTime;

        return Mathf.Lerp(0.35f, 0f, Smooth01(retractT));
    }

    private float GetCurrentAttackBaseDamage()
    {
        switch (currentAttack)
        {
            case AttackType.LeftJab:
                return jabBaseDamage;

            case AttackType.RightCross:
                return crossBaseDamage;

            default:
                return 0f;
        }
    }

    private void SetGloveRotation(
    Transform target,
    Vector3 guardForwardDirection,
    Vector3 punchForwardDirection,
    Vector3 guardRotationOffsetEuler,
    Vector3 punchRotationOffsetEuler,
    float punchRotationInfluence
)
    {
        if (!controlGloveRotation || target == null)
            return;

        if (guardForwardDirection.sqrMagnitude < 0.0001f)
            guardForwardDirection = bodyRoot.forward;

        if (punchForwardDirection.sqrMagnitude < 0.0001f)
            punchForwardDirection = guardForwardDirection;

        Quaternion guardRotation = Quaternion.LookRotation(
            guardForwardDirection.normalized,
            Vector3.up
        ) * Quaternion.Euler(guardRotationOffsetEuler);

        Quaternion punchRotation = Quaternion.LookRotation(
            punchForwardDirection.normalized,
            Vector3.up
        ) * Quaternion.Euler(punchRotationOffsetEuler);

        float t = Smooth01(punchRotationInfluence);

        target.rotation = Quaternion.Slerp(
            guardRotation,
            punchRotation,
            t
        );
    }

    private float GetPlannedAttackRotationInfluence01()
    {
        if (!attackActive)
            return 0f;

        switch (currentAttack)
        {
            case AttackType.LeftJab:
                return GetRotationInfluenceForTiming(
                    jabWindupTime,
                    jabExtendTime,
                    jabHoldTime,
                    jabRetractTime
                );

            case AttackType.RightCross:
                return GetRotationInfluenceForTiming(
                    crossWindupTime,
                    crossExtendTime,
                    crossHoldTime,
                    crossRetractTime
                );

            default:
                return 0f;
        }
    }

    private float GetAttackRotationInfluence01()
    {
        if (!attackActive)
            return 0f;

        if (!attackBlocked)
            return GetPlannedAttackRotationInfluence01();

        float retractStartTime;
        float retractTime;

        switch (currentAttack)
        {
            case AttackType.LeftJab:
                retractStartTime = jabWindupTime + jabExtendTime + jabHoldTime;
                retractTime = jabRetractTime;
                break;

            case AttackType.RightCross:
                retractStartTime = crossWindupTime + crossExtendTime + crossHoldTime;
                retractTime = crossRetractTime;
                break;

            default:
                return 0f;
        }

        // While the glove is held at the blocked position, keep the rotation frozen.
        if (attackTimer < retractStartTime)
            return attackBlockedRotationInfluence;

        // Once the hand starts retracting, rotate back together with the hand.
        float retractT = (attackTimer - retractStartTime) / retractTime;
        retractT = Smooth01(retractT);

        return Mathf.Lerp(attackBlockedRotationInfluence, 0f, retractT);
    }

    private float GetRotationInfluenceForTiming(
        float windupTime,
        float extendTime,
        float holdTime,
        float retractTime
    )
    {
        // During windup, keep guard rotation.
        if (attackTimer < windupTime)
            return 0f;

        // During extension, rotate progressively toward punch rotation.
        if (attackTimer < windupTime + extendTime)
        {
            float extensionT = (attackTimer - windupTime) / extendTime;

            float rotationT = extensionT / 0.6f;
            rotationT = Mathf.Clamp01(rotationT);

            return Smooth01(rotationT);
        }

        // During hold/impact, keep full punch rotation.
        if (attackTimer < windupTime + extendTime + holdTime)
            return 1f;

        // During retract, rotate progressively back to guard rotation.
        float retractTimer = attackTimer - windupTime - extendTime - holdTime;
        float retractT = retractTimer / retractTime;

        return 1f - Smooth01(retractT);
    }
}