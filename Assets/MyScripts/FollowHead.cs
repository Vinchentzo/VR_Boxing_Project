using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class FollowHead : MonoBehaviour
{
    [SerializeField] private Transform headCamera;
    [SerializeField] private Transform xrOrigin;

    [Header("Body Size")]
    [SerializeField] private float minHeight = 1.0f;
    [SerializeField] private float maxHeight = 1.8f;

    private CapsuleCollider capsule;

    private void Awake()
    {
        capsule = GetComponent<CapsuleCollider>();

        if (headCamera == null && Camera.main != null)
            headCamera = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (headCamera == null)
            return;

        float floorY = xrOrigin != null ? xrOrigin.position.y : 0f;

        float headHeight = Mathf.Clamp(
            headCamera.position.y - floorY,
            minHeight,
            maxHeight
        );

        Vector3 bodyCenter = new Vector3(
            headCamera.position.x,
            floorY + headHeight * 0.5f,
            headCamera.position.z
        );

        transform.position = bodyCenter;

        Vector3 headForwardFlat = Vector3.ProjectOnPlane(headCamera.forward, Vector3.up);

        if (headForwardFlat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(headForwardFlat.normalized, Vector3.up);

        capsule.height = headHeight;
        //capsule.radius = radius // to change hitboxes
        capsule.center = Vector3.zero;
    }
}