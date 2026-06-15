using UnityEngine;

public class EnemyHitReaction : MonoBehaviour
{
    private enum DebugReactionType
    {
        HeadFront,
        HeadFrontLeft,
        HeadFrontRight,
        HeadBackLeft,
        HeadBackRight,
        BodyFront,
        BodyLeft,
        BodyRight
    }

    [Header("Bones")]
    [SerializeField] private Transform headBone;
    [SerializeField] private Transform chestBone;

    [Header("Head Reaction")]
    [SerializeField] private float headImpulseScale = 1.0f;
    [SerializeField] private float headMaxAngle = 18f;
    [SerializeField] private float headSpring = 90f;
    [SerializeField] private float headDamping = 14f;

    [Header("Body Reaction")]
    [SerializeField] private float bodyImpulseScale = 1.0f;
    [SerializeField] private float bodyMaxAngle = 10f;
    [SerializeField] private float bodySpring = 70f;
    [SerializeField] private float bodyDamping = 12f;

    [Header("Debug Test")]
    [SerializeField] private DebugReactionType debugReactionType;
    [SerializeField] private bool testReaction;

    [Header("Head Hit Regions")]
    [SerializeField] private Collider[] frontHeadColliders;
    [SerializeField] private Collider[] backHeadColliders;

    private Vector3 headOffset;
    private Vector3 headVelocity;

    private Vector3 bodyOffset;
    private Vector3 bodyVelocity;

    private void LateUpdate()
    {
        HandleDebugTest();

        UpdateSpring(ref headOffset, ref headVelocity, headSpring, headDamping, headMaxAngle);
        UpdateSpring(ref bodyOffset, ref bodyVelocity, bodySpring, bodyDamping, bodyMaxAngle);

        ApplyReaction();
    }

    public void AddHeadImpulse(Vector3 localRotationImpulse)
    {
        Vector3 impulse = localRotationImpulse * headImpulseScale;

        // Small immediate reaction, mostly velocity-driven.
        headOffset += impulse * 0.25f;
        headVelocity += impulse * 14f;

        ClampOffset(ref headOffset, headMaxAngle);
    }

    public void AddBodyImpulse(Vector3 localRotationImpulse)
    {
        Vector3 impulse = localRotationImpulse * bodyImpulseScale;

        // Body should be less snappy because it moves many child bones.
        bodyOffset += impulse * 0.15f;
        bodyVelocity += impulse * 10f;

        ClampOffset(ref bodyOffset, bodyMaxAngle);
    }

    public void ReactToHeadFrontHit(float strength = 1f)
    {
        // Front face hit: head goes slightly backward.
        AddHeadImpulse(new Vector3(-8f, 0f, 0f) * strength);
    }

    public void ReactToHeadFrontLeftHit(float strength = 1f)
    {
        AddHeadImpulse(new Vector3(0f, -8f, 3f) * strength);
    }

    public void ReactToHeadFrontRightHit(float strength = 1f)
    {
        AddHeadImpulse(new Vector3(0f, 8f, -3f) * strength);
    }

    public void ReactToHeadBackLeftHit(float strength = 1f)
    {
        AddHeadImpulse(new Vector3(0f, 0f, 8f) * strength);
    }

    public void ReactToHeadBackRightHit(float strength = 1f)
    {
        AddHeadImpulse(new Vector3(0f, 0f, -8f) * strength);
    }

    public void ReactToBodyFrontHit(float strength = 1f)
    {
        // Body curls forward slightly.
        AddBodyImpulse(new Vector3(10f, 0f, 0f) * strength);
    }

    public void ReactToBodyLeftHit(float strength = 1f)
    {
        // Side abdomen/rib hit: curl forward and bend left.
        AddBodyImpulse(new Vector3(7f, 0f, -8f) * strength);
    }

    public void ReactToBodyRightHit(float strength = 1f)
    {
        // Side abdomen/rib hit: curl forward and bend right.
        AddBodyImpulse(new Vector3(7f, 0f, 8f) * strength);
    }

    public bool IsFrontHeadCollider(Collider hitCollider)
    {
        return ContainsCollider(frontHeadColliders, hitCollider);
    }

    public bool IsBackHeadCollider(Collider hitCollider)
    {
        return ContainsCollider(backHeadColliders, hitCollider);
    }

    private bool ContainsCollider(Collider[] colliders, Collider target)
    {
        if (target == null || colliders == null)
            return false;

        foreach (Collider collider in colliders)
        {
            if (collider == target)
                return true;
        }

        return false;
    }

    private void HandleDebugTest()
    {
        if (!testReaction)
            return;

        testReaction = false;

        switch (debugReactionType)
        {
            case DebugReactionType.HeadFront:
                ReactToHeadFrontHit();
                break;

            case DebugReactionType.HeadFrontLeft:
                ReactToHeadFrontLeftHit();
                break;

            case DebugReactionType.HeadFrontRight:
                ReactToHeadFrontRightHit();
                break;

            case DebugReactionType.HeadBackLeft:
                ReactToHeadBackLeftHit();
                break;

            case DebugReactionType.HeadBackRight:
                ReactToHeadBackRightHit();
                break;

            case DebugReactionType.BodyFront:
                ReactToBodyFrontHit();
                break;

            case DebugReactionType.BodyLeft:
                ReactToBodyLeftHit();
                break;

            case DebugReactionType.BodyRight:
                ReactToBodyRightHit();
                break;
        }
    }

    private void UpdateSpring(
        ref Vector3 offset,
        ref Vector3 velocity,
        float spring,
        float damping,
        float maxAngle
    )
    {
        float dt = Time.deltaTime;

        velocity += -offset * spring * dt;
        velocity *= Mathf.Exp(-damping * dt);

        offset += velocity * dt;

        ClampOffset(ref offset, maxAngle);
    }

    private void ClampOffset(ref Vector3 offset, float maxAngle)
    {
        offset.x = Mathf.Clamp(offset.x, -maxAngle, maxAngle);
        offset.y = Mathf.Clamp(offset.y, -maxAngle, maxAngle);
        offset.z = Mathf.Clamp(offset.z, -maxAngle, maxAngle);
    }

    private void ApplyReaction()
    {
        if (chestBone != null)
        {
            chestBone.localRotation *= Quaternion.Euler(bodyOffset);
        }

        if (headBone != null)
        {
            headBone.localRotation *= Quaternion.Euler(headOffset);
        }
    }
}