using UnityEngine;

public class PlayerGloveContactReporter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatContactResolver resolver;

    [Header("Hit Filtering")]
    [SerializeField] private float contactCooldown = 0.15f;

    [Header("Enemy Reaction Selection")]
    [SerializeField] private float bodySideThreshold = 0.10f;
    [SerializeField] private float headSideThreshold = 0.04f;
    [SerializeField] private float headFrontZThreshold = 0.22f;

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

        if (damage > 0f)
        {
            surface.OwnerHealth.TakeDamage(damage);
            ApplyEnemyReaction(result, surface, other);
        }

        Debug.Log(
            $"{name} contact resolved as {result} against {surface.Side} {surface.SurfaceType}. Speed={punchSpeed:F2}, Damage={damage:F1}",
            other
        );
    }

    private void ApplyEnemyReaction(
    CombatContactResult result,
    CombatSurface surface,
    Collider hitCollider
)
    {
        if (surface == null || surface.OwnerHealth == null)
            return;

        EnemyHitReaction hitReaction = surface.OwnerHealth.GetComponentInChildren<EnemyHitReaction>();

        if (hitReaction == null)
            return;

        Vector3 hitPoint = hitCollider.ClosestPoint(transform.position);
        Vector3 localHit = surface.OwnerHealth.transform.InverseTransformPoint(hitPoint);

        Debug.Log(
            $"Enemy reaction debug: result={result}, surface={surface.SurfaceType}, collider={hitCollider.name}, localHit={localHit}",
            hitCollider
        );

        // Manual override/fallback if you set Reaction Zone in the Inspector later.
        if (surface.ReactionZone != CombatReactionZone.None)
        {
            ApplyReactionZone(hitReaction, surface.ReactionZone);
            return;
        }

        switch (result)
        {
            case CombatContactResult.HeadHit:
                ApplyHeadReaction(hitReaction, localHit);
                break;

            case CombatContactResult.BodyHit:
                ApplyBodyReaction(hitReaction, localHit);
                break;
        }
    }

    private void ApplyHeadReaction(EnemyHitReaction hitReaction, Vector3 localHit)
    {
        // Enemy right side is negative local X.
        // Check side FIRST, otherwise side/front punches can be incorrectly classified as front.
        if (localHit.x <= -headSideThreshold)
        {
            hitReaction.ReactToHeadFrontRightHit();
            return;
        }

        // Enemy left side is positive local X.
        if (localHit.x >= headSideThreshold)
        {
            hitReaction.ReactToHeadFrontLeftHit();
            return;
        }

        // If it is not clearly left/right, treat it as a front face hit.
        hitReaction.ReactToHeadFrontHit();
    }

    private void ApplyBodyReaction(EnemyHitReaction hitReaction, Vector3 localHit)
    {
        // Enemy right side is negative local X.
        if (localHit.x <= -bodySideThreshold)
        {
            hitReaction.ReactToBodyRightHit();
            return;
        }

        // Enemy left side is positive local X.
        if (localHit.x >= bodySideThreshold)
        {
            hitReaction.ReactToBodyLeftHit();
            return;
        }

        hitReaction.ReactToBodyFrontHit();
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