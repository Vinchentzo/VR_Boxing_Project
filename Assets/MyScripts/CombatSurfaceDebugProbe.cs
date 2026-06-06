using UnityEngine;

public class CombatSurfaceDebugProbe : MonoBehaviour
{

    [SerializeField] private CombatantSide targetSide = CombatantSide.Enemy;

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("In OnTriggerEnter().");
        ReportSurface(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("In OnCollisionEnter().");
        ReportSurface(collision.collider);
    }

    private void OnTriggerStay(Collider other)
    {
        //Debug.Log("In OnTriggerStay().");
        ReportSurface(other);
    }

    private void ReportSurface(Collider other)
    {
        CombatSurface surface = other.GetComponent<CombatSurface>();

        if (surface == null)
            return;

        if (surface.Side != targetSide)
            return;

        //if (surface == null)
        //{
        //    Debug.Log($"{name} touched non-combat collider: {other.name}", other);
        //    return;
        //}

        //Debug.Log(
        //    $"{name} touched {surface.Side} {surface.SurfaceType}",
        //    other
        //);
    }
}