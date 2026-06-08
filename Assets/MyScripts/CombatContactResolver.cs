using UnityEngine;

public enum CombatContactResult
{
    None,
    TooSlow,
    HeadHit,
    BodyHit,
    Blocked
}

public class CombatContactResolver : MonoBehaviour
{
    [Header("Player Punch Detection")]
    [SerializeField] private float minimumPlayerPunchSpeed = 1.0f;

    [Header("Player Punch Damage")]
    [SerializeField] private float damagePerSpeedUnit = 5f;
    [SerializeField] private float maximumPlayerPunchDamage = 30f;

    public CombatContactResult ResolvePlayerGloveContact(CombatSurface surface, float punchSpeed)
    {
        if (surface == null)
            return CombatContactResult.None;

        if (surface.Side != CombatantSide.Enemy)
            return CombatContactResult.None;

        if (punchSpeed < minimumPlayerPunchSpeed)
        {
            Debug.Log("Punch too slow!!!!!");
            return CombatContactResult.TooSlow;
        }
            

        switch (surface.SurfaceType)
        {
            case CombatSurfaceType.Head:
                return CombatContactResult.HeadHit;

            case CombatSurfaceType.Chest:
            case CombatSurfaceType.Abdomen:
                return CombatContactResult.BodyHit;

            case CombatSurfaceType.GuardGlove:
            case CombatSurfaceType.GuardForearm:
                return CombatContactResult.Blocked;

            default:
                return CombatContactResult.None;
        }
    }

    public float CalculatePlayerPunchDamage(
        CombatContactResult result,
        CombatSurface surface,
        float punchSpeed)
    {
        if (surface == null)
            return 0f;

        if (result == CombatContactResult.Blocked)
            return 0f;

        if (result != CombatContactResult.HeadHit &&
            result != CombatContactResult.BodyHit)
            return 0f;

        float rawDamage = punchSpeed * damagePerSpeedUnit * surface.DamageMultiplier;
        return Mathf.Min(rawDamage, maximumPlayerPunchDamage);
    }

    public CombatContactResult ResolveEnemyGloveContact(CombatSurface surface)
    {
        if (surface == null)
            return CombatContactResult.None;

        if (surface.Side != CombatantSide.Player)
            return CombatContactResult.None;

        switch (surface.SurfaceType)
        {
            case CombatSurfaceType.Head:
                return CombatContactResult.HeadHit;

            case CombatSurfaceType.Chest:
            case CombatSurfaceType.Abdomen:
                return CombatContactResult.BodyHit;

            case CombatSurfaceType.GuardGlove:
            case CombatSurfaceType.GuardForearm:
                return CombatContactResult.Blocked;

            default:
                return CombatContactResult.None;
        }
    }
    public float CalculateEnemyPunchDamage(
    CombatContactResult result,
    CombatSurface surface,
    float baseAttackDamage)
    {
        if (surface == null)
            return 0f;

        if (result == CombatContactResult.Blocked)
            return 0f;

        if (result != CombatContactResult.HeadHit &&
            result != CombatContactResult.BodyHit)
            return 0f;

        return baseAttackDamage * surface.DamageMultiplier;
    }
}