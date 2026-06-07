using UnityEngine;

public class PlayerGlovePenetrationDebug : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask blockingLayers;
    [SerializeField] private float searchPadding = 0.05f;

    [Header("Debug")]
    [SerializeField] private float logInterval = 0.25f;

    private Collider[] gloveColliders;
    private readonly Collider[] overlapResults = new Collider[32];

    private float nextLogTime;

    private void Awake()
    {
        gloveColliders = GetComponentsInChildren<Collider>();

        if (gloveColliders.Length == 0)
        {
            Debug.LogError(
                $"{nameof(PlayerGlovePenetrationDebug)} on {name} found no child colliders.",
                this
            );

            enabled = false;
            return;
        }

        if (blockingLayers.value == 0)
        {
            Debug.LogError(
                $"{nameof(PlayerGlovePenetrationDebug)} on {name} requires Blocking Layers to be assigned.",
                this
            );

            enabled = false;
            return;
        }
    }

    private void LateUpdate()
    {
        foreach (Collider gloveCollider in gloveColliders)
        {
            CheckGloveCollider(gloveCollider);
        }
    }

    private void CheckGloveCollider(Collider gloveCollider)
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

        for (int i = 0; i < hitCount; i++)
        {
            Collider enemyCollider = overlapResults[i];

            if (enemyCollider == null)
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

            if (Time.time < nextLogTime)
                return;

            nextLogTime = Time.time + logInterval;

            CombatSurface surface = enemyCollider.GetComponentInParent<CombatSurface>();

            string surfaceText = surface != null
                ? $"{surface.Side} {surface.SurfaceType}"
                : enemyCollider.name;

            Debug.Log(
                $"{name}: {gloveCollider.name} penetrates {surfaceText}. Push direction={direction}, distance={distance:F3}",
                enemyCollider
            );

            return;
        }
    }
}