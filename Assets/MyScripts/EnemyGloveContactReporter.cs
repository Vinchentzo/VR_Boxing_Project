using UnityEngine;

public class EnemyGloveContactReporter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatContactResolver resolver;

    private Enemy enemy;
    private bool referencesValid;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
        referencesValid = ValidateReferences();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!referencesValid)
            return;

        if (!enemy.CanDealAttackHit)
            return;

        CombatSurface surface = other.GetComponentInParent<CombatSurface>();

        if (surface == null)
            return;

        CombatContactResult result = resolver.ResolveEnemyGloveContact(surface);

        if (result == CombatContactResult.None)
            return;

        if (!enemy.TryConsumeAttackHit(out float baseDamage))
            return;

        float appliedDamage = resolver.CalculateEnemyPunchDamage(result, surface, baseDamage);

        if (appliedDamage > 0f)
        {
            surface.OwnerHealth.TakeDamage(appliedDamage);
        }

        Debug.Log(
            $"{name} enemy active punch resolved as {result} against {surface.Side} {surface.SurfaceType}. BaseDamage={baseDamage:F1}, AppliedDamage={appliedDamage:F1}",
            other
        );
    }

    private bool ValidateReferences()
    {
        if (resolver == null)
        {
            Debug.LogError(
                $"{nameof(EnemyGloveContactReporter)} on {name} requires a CombatContactResolver reference.",
                this
            );
            return false;
        }

        if (enemy == null)
        {
            Debug.LogError(
                $"{nameof(EnemyGloveContactReporter)} on {name} could not find Enemy in parent objects.",
                this
            );
            return false;
        }

        return true;
    }
}