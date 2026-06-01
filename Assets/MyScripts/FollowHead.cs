using UnityEngine;

[DisallowMultipleComponent]
public class FollowHead : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform headCamera;
    [SerializeField] private Transform xrOrigin;

    [Header("Body Height")]
    [SerializeField, Min(0.01f)] private float minHeight = 1.0f;
    [SerializeField, Min(0.01f)] private float maxHeight = 1.8f;

    private CapsuleCollider capsuleCollider;

    private void Awake()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (!ValidateReferences())
            enabled = false;
    }

    private void LateUpdate()
    {
        float floorY = xrOrigin.position.y;

        float bodyHeight = Mathf.Clamp(
            headCamera.position.y - floorY,
            minHeight,
            maxHeight
        );

        Vector3 bodyCenter = new Vector3(
            headCamera.position.x,
            floorY + bodyHeight * 0.5f,
            headCamera.position.z
        );

        transform.position = bodyCenter;

        Vector3 horizontalHeadForward = Vector3.ProjectOnPlane(
            headCamera.forward,
            Vector3.up
        );

        if (horizontalHeadForward.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(
                horizontalHeadForward.normalized,
                Vector3.up
            );
        }

        capsuleCollider.height = bodyHeight;
        capsuleCollider.center = Vector3.zero;
    }

    private bool ValidateReferences()
    {
        if (headCamera == null)
        {
            Debug.LogError("FollowHead requires the headset camera Transform reference.", this);
            return false;
        }

        if (xrOrigin == null)
        {
            Debug.LogError("FollowHead requires the XR Origin Transform reference.", this);
            return false;
        }

        if (capsuleCollider == null)
        {
            Debug.LogError("FollowHead requires a CapsuleCollider on the same GameObject.", this);
            return false;
        }

        if (maxHeight < minHeight)
        {
            Debug.LogError("FollowHead maximum height must be greater than or equal to minimum height.", this);
            return false;
        }

        return true;
    }
}