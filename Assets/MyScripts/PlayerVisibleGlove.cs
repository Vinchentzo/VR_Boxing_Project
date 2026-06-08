using UnityEngine;

public class PlayerVisibleGlove : MonoBehaviour
{
    [Header("Blocking")]
    [SerializeField] private LayerMask blockingLayers;
    [SerializeField] private float searchPadding = 0.05f;
    [SerializeField] private float skinDistance = 0.005f;

    [Header("Solver")]
    [SerializeField] private float maxStepDistance = 0.025f;
    [SerializeField] private int maxMoveSteps = 12;
    [SerializeField] private int penetrationIterations = 4;

    private Transform trackedRoot;

    private Vector3 defaultLocalPosition;
    private Quaternion defaultLocalRotation;

    private Vector3 resolvedWorldPosition;
    private Quaternion resolvedWorldRotation;

    private Collider[] gloveColliders;
    private readonly Collider[] overlapResults = new Collider[32];

    private void Awake()
    {
        trackedRoot = transform.parent;

        if (trackedRoot == null)
        {
            Debug.LogError(
                $"{nameof(PlayerVisibleGlove)} on {name} requires a parent tracked glove object.",
                this
            );

            enabled = false;
            return;
        }

        defaultLocalPosition = transform.localPosition;
        defaultLocalRotation = transform.localRotation;

        gloveColliders = GetComponentsInChildren<Collider>();

        if (gloveColliders.Length == 0)
        {
            Debug.LogError(
                $"{nameof(PlayerVisibleGlove)} on {name} found no child glove colliders.",
                this
            );

            enabled = false;
            return;
        }

        if (blockingLayers.value == 0)
        {
            Debug.LogError(
                $"{nameof(PlayerVisibleGlove)} on {name} requires Blocking Layers to be assigned.",
                this
            );

            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (trackedRoot == null)
            return;

        resolvedWorldPosition = trackedRoot.TransformPoint(defaultLocalPosition);
        resolvedWorldRotation = trackedRoot.rotation * defaultLocalRotation;

        transform.SetPositionAndRotation(resolvedWorldPosition, resolvedWorldRotation);
    }

    private void LateUpdate()
    {
        Vector3 desiredWorldPosition = trackedRoot.TransformPoint(defaultLocalPosition);
        Quaternion desiredWorldRotation = trackedRoot.rotation * defaultLocalRotation;

        // Important:
        // Start solving from the previous allowed world position,
        // not from transform.position, because the parent follows the controller.
        resolvedWorldRotation = desiredWorldRotation;
        transform.SetPositionAndRotation(resolvedWorldPosition, resolvedWorldRotation);

        resolvedWorldPosition = SolvePositionToward(
            resolvedWorldPosition,
            desiredWorldPosition
        );

        // Rotation follows the real hand even if position is blocked.
        transform.SetPositionAndRotation(resolvedWorldPosition, resolvedWorldRotation);
    }

    private Vector3 SolvePositionToward(Vector3 currentPosition, Vector3 desiredPosition)
    {
        // First, make sure current position is not already penetrating.
        // This can happen if the enemy animation/body moves into the glove.
        transform.position = currentPosition;
        transform.rotation = resolvedWorldRotation;

        Vector3 startCorrection = ResolvePenetrations();

        if (startCorrection.sqrMagnitude > 0.000001f)
        {
            currentPosition += startCorrection;
            transform.position = currentPosition;
            transform.rotation = resolvedWorldRotation;
        }

        Vector3 toTarget = desiredPosition - currentPosition;
        float totalDistance = toTarget.magnitude;

        if (totalDistance <= 0.0001f)
            return currentPosition;

        int stepCount = Mathf.CeilToInt(totalDistance / maxStepDistance);
        stepCount = Mathf.Clamp(stepCount, 1, maxMoveSteps);

        Vector3 lastSafePosition = currentPosition;

        for (int step = 1; step <= stepCount; step++)
        {
            float t = step / (float)stepCount;
            Vector3 candidatePosition = Vector3.Lerp(currentPosition, desiredPosition, t);

            transform.position = candidatePosition;
            transform.rotation = resolvedWorldRotation;

            if (HasAnyPenetration())
            {
                // Direct path is blocked.
                // Do not slide sideways.
                transform.position = lastSafePosition;
                transform.rotation = resolvedWorldRotation;
                return lastSafePosition;
            }

            lastSafePosition = candidatePosition;
        }

        return desiredPosition;
    }


    private bool HasAnyPenetration()
    {
        foreach (Collider gloveCollider in gloveColliders)
        {
            if (gloveCollider == null || !gloveCollider.enabled)
                continue;

            Bounds bounds = gloveCollider.bounds;
            float searchRadius = bounds.extents.magnitude + searchPadding;

            int hitCount = Physics.OverlapSphereNonAlloc(
                bounds.center,
                searchRadius,
                overlapResults,
                blockingLayers,
                QueryTriggerInteraction.Collide
            );

            for (int i = 0; i < hitCount; i++)
            {
                Collider enemyCollider = overlapResults[i];

                if (enemyCollider == null || !enemyCollider.enabled)
                    continue;

                bool penetrating = Physics.ComputePenetration(
                    gloveCollider,
                    gloveCollider.transform.position,
                    gloveCollider.transform.rotation,
                    enemyCollider,
                    enemyCollider.transform.position,
                    enemyCollider.transform.rotation,
                    out _,
                    out float distance
                );

                if (penetrating && distance > 0.0001f)
                    return true;
            }
        }

        return false;
    }


    private Vector3 ResolvePenetrations()
    {
        Vector3 totalCorrection = Vector3.zero;

        for (int iteration = 0; iteration < penetrationIterations; iteration++)
        {
            Vector3 iterationCorrection = Vector3.zero;

            foreach (Collider gloveCollider in gloveColliders)
            {
                if (gloveCollider == null || !gloveCollider.enabled)
                    continue;

                iterationCorrection += ComputeColliderCorrection(gloveCollider);
            }

            if (iterationCorrection.sqrMagnitude <= 0.000001f)
                break;

            transform.position += iterationCorrection;
            totalCorrection += iterationCorrection;
        }

        return totalCorrection;
    }

    

    private Vector3 ComputeColliderCorrection(Collider gloveCollider)
    {
        Bounds bounds = gloveCollider.bounds;
        float searchRadius = bounds.extents.magnitude + searchPadding;

        int hitCount = Physics.OverlapSphereNonAlloc(
            bounds.center,
            searchRadius,
            overlapResults,
            blockingLayers,
            QueryTriggerInteraction.Collide
        );

        Vector3 correction = Vector3.zero;

        for (int i = 0; i < hitCount; i++)
        {
            Collider enemyCollider = overlapResults[i];

            if (enemyCollider == null || !enemyCollider.enabled)
                continue;

            bool penetrating = Physics.ComputePenetration(
                gloveCollider,
                gloveCollider.transform.position,
                gloveCollider.transform.rotation,
                enemyCollider,
                enemyCollider.transform.position,
                enemyCollider.transform.rotation,
                out Vector3 direction,
                out float distance
            );

            if (!penetrating)
                continue;

            if (distance <= 0.0001f)
                continue;

            correction += direction * (distance + skinDistance);
        }

        return correction;
    }
}