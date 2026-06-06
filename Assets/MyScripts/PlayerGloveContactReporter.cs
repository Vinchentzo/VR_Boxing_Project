using UnityEngine;

public class PlayerGloveContactReporter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatContactResolver resolver;

    [Header("Hit Filtering")]
    [SerializeField] private float contactCooldown = 0.15f;

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
        }

        Debug.Log(
            $"{name} contact resolved as {result} against {surface.Side} {surface.SurfaceType}. Speed={punchSpeed:F2}, Damage={damage:F1}",
            other
        );
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