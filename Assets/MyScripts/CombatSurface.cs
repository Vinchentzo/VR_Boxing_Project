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

public enum CombatReactionZone
{
    None,

    // Head reactions
    HeadFront,
    HeadFrontLeft,
    HeadFrontRight,
    HeadBackLeft,
    HeadBackRight,

    // Body reactions
    BodyFront,
    BodyLeft,
    BodyRight
}

public class CombatSurface : MonoBehaviour
{
    [Header("Combat Surface")]
    [SerializeField] private CombatantSide side = CombatantSide.Unknown;
    [SerializeField] private CombatSurfaceType surfaceType = CombatSurfaceType.Unknown;

    [Header("Reaction")]
    [SerializeField] private CombatReactionZone reactionZone = CombatReactionZone.None;

    [Header("Tuning")]
    [SerializeField] private float damageMultiplier = 1f;

    private Health ownerHealth;

    public CombatantSide Side => side;
    public CombatSurfaceType SurfaceType => surfaceType;
    public CombatReactionZone ReactionZone => reactionZone;
    public float DamageMultiplier => damageMultiplier;
    public Health OwnerHealth => ownerHealth;

    public bool IsDamageTarget =>
        surfaceType == CombatSurfaceType.Head ||
        surfaceType == CombatSurfaceType.Chest ||
        surfaceType == CombatSurfaceType.Abdomen;

    public bool IsGuard =>
        surfaceType == CombatSurfaceType.GuardGlove ||
        surfaceType == CombatSurfaceType.GuardForearm;

    private void Awake()
    {
        ownerHealth = GetComponentInParent<Health>();

        if (ownerHealth == null && IsDamageTarget)
        {
            Debug.LogError(
                $"{nameof(CombatSurface)} on {name} is a damage target but could not find Health in parent objects.",
                this
            );

            enabled = false;
            return;
        }
    }
}