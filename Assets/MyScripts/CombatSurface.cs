using UnityEngine;

public enum CombatantSide
{
    Unknown,
    Player,
    Enemy
}

public enum CombatSurfaceType
{
    Unknown,

    // Damage targets
    Head,
    Chest,
    Abdomen,

    // Blocking surfaces
    GuardGlove,
    GuardForearm,

    // Optional later use
    OtherBody
}

public class CombatSurface : MonoBehaviour
{
    [Header("Combat Surface")]
    [SerializeField] private CombatantSide side = CombatantSide.Unknown;
    [SerializeField] private CombatSurfaceType surfaceType = CombatSurfaceType.Unknown;

    public CombatantSide Side => side;
    public CombatSurfaceType SurfaceType => surfaceType;

    public bool IsDamageTarget =>
        surfaceType == CombatSurfaceType.Head ||
        surfaceType == CombatSurfaceType.Chest ||
        surfaceType == CombatSurfaceType.Abdomen;

    public bool IsGuard =>
        surfaceType == CombatSurfaceType.GuardGlove ||
        surfaceType == CombatSurfaceType.GuardForearm;
}