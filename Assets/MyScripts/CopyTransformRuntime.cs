using UnityEngine;

public class CopyTransformRuntime : MonoBehaviour
{
    [SerializeField] private Transform source;
    [SerializeField] private Transform target;
    [SerializeField] private bool copyPosition = true;
    [SerializeField] private bool copyRotation = true;

    private void LateUpdate()
    {
        if (source == null || target == null)
            return;

        if (copyPosition)
            target.position = source.position;

        if (copyRotation)
            target.rotation = source.rotation;
    }
}