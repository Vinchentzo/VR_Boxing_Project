using UnityEngine;

public class PlayerGloveContactReporter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatContactResolver resolver;

    [Header("Hit Filtering")]
    [SerializeField] private float contactCooldown = 0.15f;

    [Header("Enemy Reaction Selection")]
    [SerializeField] private float minimumSideReactionVelocity = 1.5f;
    [SerializeField] private float sideReactionDominance = 1.2f;
    [SerializeField] private float reactionMinSpeed = 1.0f;
    [SerializeField] private float reactionMaxSpeed = 5.0f;
    [SerializeField] private float minimumReactionStrength = 0.75f;
    [SerializeField] private float maximumReactionStrength = 1.45f;

    private HandVelocityTracker handVelocityTracker;
    private PlayerPunchState punchState;
    private bool referencesValid;
    private float nextAllowedContactTime;

    private void Awake()
    {
        handVelocityTracker = GetComponentInParent<HandVelocityTracker>();
        punchState = GetComponentInParent<PlayerPunchState>();

        referencesValid = ValidateReferences();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!referencesValid)
            return;

        if (Time.time < nextAllowedContactTime)
            return;

        if (!punchState.IsPunchAvailable)
            return;

        CombatSurface surface = other.GetComponentInParent<CombatSurface>();

        if (surface == null)
            return;

        float punchSpeed = handVelocityTracker.Velocity.magnitude;
        CombatContactResult result = resolver.ResolvePlayerGloveContact(surface, punchSpeed);

        if (result == CombatContactResult.None)
            return;

        if (result == CombatContactResult.TooSlow)
            return;

        if (!punchState.TryConsumePunch())
            return;

        nextAllowedContactTime = Time.time + contactCooldown;

        float damage = resolver.CalculatePlayerPunchDamage(result, surface, punchSpeed);
        float reactionStrength = CalculateReactionStrength(punchSpeed);

        if (damage > 0f)
        {
            surface.OwnerHealth.TakeDamage(damage);
            ApplyEnemyReaction(result, surface, other, reactionStrength);
        }
        else if (result == CombatContactResult.Blocked)
        {
            ApplyEnemyGuardReaction(surface, other, reactionStrength);
        }

        Debug.Log(
            $"{name} contact resolved as {result} against {surface.Side} {surface.SurfaceType}. Speed={punchSpeed:F2}, Damage={damage:F1}",
            other
        );
    }

    private void ApplyEnemyGuardReaction(
    CombatSurface surface,
    Collider hitCollider,
    float reactionStrength
)
    {
        if (surface == null || surface.OwnerHealth == null)
            return;

        EnemyGuardTargetFollower guardFollower =
            surface.OwnerHealth.GetComponentInChildren<EnemyGuardTargetFollower>();

        if (guardFollower == null)
            return;

        Transform enemyRoot = surface.OwnerHealth.transform;
        Vector3 localVelocity = enemyRoot.InverseTransformDirection(handVelocityTracker.Velocity);

        guardFollower.AddGuardImpact(hitCollider, localVelocity, reactionStrength);
    }

    private float CalculateReactionStrength(float punchSpeed)
    {
        float t = Mathf.InverseLerp(reactionMinSpeed, reactionMaxSpeed, punchSpeed);
        return Mathf.Lerp(minimumReactionStrength, maximumReactionStrength, t);
    }

    private void ApplyEnemyReaction(
    CombatContactResult result,
    CombatSurface surface,
    Collider hitCollider,
    float reactionStrength
)
    {
        if (surface == null || surface.OwnerHealth == null)
            return;

        EnemyHitReaction hitReaction = surface.OwnerHealth.GetComponentInChildren<EnemyHitReaction>();

        if (hitReaction == null)
            return;

        Transform enemyRoot = surface.OwnerHealth.transform;

        Vector3 hitPoint = hitCollider.ClosestPoint(transform.position);
        Vector3 localHit = enemyRoot.InverseTransformPoint(hitPoint);

        // This is the center/position of your glove hitbox relative to the enemy.
        // It may be better for deciding left/right than ClosestPoint.
        Vector3 localGlove = enemyRoot.InverseTransformPoint(transform.position);

        // This is the punch movement direction relative to the enemy.
        Vector3 localVelocity = enemyRoot.InverseTransformDirection(handVelocityTracker.Velocity);

        //Debug.Log(
        //    $"Enemy reaction debug: result={result}, surface={surface.SurfaceType}, collider={hitCollider.name}, " +
        //    $"localHit={localHit}, localGlove={localGlove}, localVelocity={localVelocity}",
        //    hitCollider
        //);

        // Manual override/fallback if you set Reaction Zone in the Inspector later.
        if (surface.ReactionZone != CombatReactionZone.None)
        {
            ApplyReactionZone(hitReaction, surface.ReactionZone);
            return;
        }

        switch (result)
        {
            case CombatContactResult.HeadHit:
                ApplyHeadReaction(hitReaction, localVelocity, hitCollider, reactionStrength);
                break;

            case CombatContactResult.BodyHit:
                ApplyBodyReaction(hitReaction, localVelocity, reactionStrength);
                break;
        }
    }

    private void ApplyHeadReaction(
    EnemyHitReaction hitReaction,
    Vector3 localVelocity,
    Collider hitCollider,
    float strength
)
    {
        bool isSidePunch = IsDominantSidePunch(localVelocity);
        bool isBackHeadHit = hitReaction.IsBackHeadCollider(hitCollider);

        if (!isSidePunch)
        {
            hitReaction.ReactToHeadFrontHit(strength);
            return;
        }

        // From your logs:
        // localVelocity.x > 0 means punch comes from enemy right side.
        if (localVelocity.x > 0f)
        {
            if (isBackHeadHit)
                hitReaction.ReactToHeadBackRightHit(strength);
            else
                hitReaction.ReactToHeadFrontRightHit(strength);

            return;
        }

        if (isBackHeadHit)
            hitReaction.ReactToHeadBackLeftHit(strength);
        else
            hitReaction.ReactToHeadFrontLeftHit(strength);
    }

    private void ApplyBodyReaction(
    EnemyHitReaction hitReaction,
    Vector3 localVelocity,
    float strength
)
    {
        if (IsDominantSidePunch(localVelocity))
        {
            // From your logs:
            // localVelocity.x > 0 means punch comes from enemy right side.
            if (localVelocity.x > 0f)
            {
                hitReaction.ReactToBodyRightHit(strength);
                return;
            }

            hitReaction.ReactToBodyLeftHit(strength);
            return;
        }

        hitReaction.ReactToBodyFrontHit(strength);
    }

    private bool IsDominantSidePunch(Vector3 localVelocity)
    {
        float sideSpeed = Mathf.Abs(localVelocity.x);
        float forwardSpeed = Mathf.Abs(localVelocity.z);

        return sideSpeed >= minimumSideReactionVelocity &&
               sideSpeed >= forwardSpeed * sideReactionDominance;
    }

    private void ApplyReactionZone(EnemyHitReaction hitReaction, CombatReactionZone reactionZone)
    {
        switch (reactionZone)
        {
            case CombatReactionZone.HeadFront:
                hitReaction.ReactToHeadFrontHit();
                break;

            case CombatReactionZone.HeadFrontLeft:
                hitReaction.ReactToHeadFrontLeftHit();
                break;

            case CombatReactionZone.HeadFrontRight:
                hitReaction.ReactToHeadFrontRightHit();
                break;

            case CombatReactionZone.HeadBackLeft:
                hitReaction.ReactToHeadBackLeftHit();
                break;

            case CombatReactionZone.HeadBackRight:
                hitReaction.ReactToHeadBackRightHit();
                break;

            case CombatReactionZone.BodyFront:
                hitReaction.ReactToBodyFrontHit();
                break;

            case CombatReactionZone.BodyLeft:
                hitReaction.ReactToBodyLeftHit();
                break;

            case CombatReactionZone.BodyRight:
                hitReaction.ReactToBodyRightHit();
                break;
        }
    }

    private bool ValidateReferences()
    {
        if (resolver == null)
        {
            Debug.LogError(
                $"{nameof(PlayerGloveContactReporter)} on {name} requires a CombatContactResolver reference.",
                this
            );
            return false;
        }

        if (handVelocityTracker == null)
        {
            Debug.LogError(
                $"{nameof(PlayerGloveContactReporter)} on {name} could not find HandVelocityTracker in parent objects.",
                this
            );
            return false;
        }

        if (punchState == null)
        {
            Debug.LogError(
                $"{nameof(PlayerGloveContactReporter)} on {name} could not find PlayerPunchState in parent objects.",
                this
            );
            return false;
        }

        return true;
    }
}