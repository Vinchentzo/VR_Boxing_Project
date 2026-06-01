using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PunchTargetHit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health targetHealth;

    [Header("Punch Damage")]
    [SerializeField, Min(0f)] private float minimumPunchSpeed = 0.8f;
    [SerializeField, Min(0f)] private float damagePerSpeedUnit = 10f;
    [SerializeField, Min(0f)] private float maximumDamage = 25f;
    [SerializeField, Min(0f)] private float hitCooldown = 0.25f;

    private readonly Dictionary<int, float> nextAllowedHitTimes = new();

    private void Awake()
    {
        if (targetHealth != null)
            return;

        Debug.LogError("PunchTargetHit requires a target Health reference.", this);
        enabled = false;
    }

    private void OnDisable()
    {
        nextAllowedHitTimes.Clear();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contactCount == 0 || targetHealth.IsKnockedOut)
            return;

        ContactPoint contact = collision.GetContact(0);

        HandVelocityTracker handTracker =
            contact.otherCollider.GetComponentInParent<HandVelocityTracker>();

        if (handTracker == null || !handTracker.CompareTag("Hand"))
            return;

        HitZone hitZone = contact.thisCollider.GetComponent<HitZone>();

        if (hitZone == null)
            return;

        int handId = handTracker.gameObject.GetInstanceID();
        float currentTime = Time.time;

        if (nextAllowedHitTimes.TryGetValue(handId, out float nextAllowedTime)
            && currentTime < nextAllowedTime)
        {
            return;
        }

        float punchSpeed = handTracker.Velocity.magnitude;

        if (punchSpeed < minimumPunchSpeed)
        {
            Debug.Log($"Punch too slow: {punchSpeed:F2}");
            return;
        }

        float damage = Mathf.Min(
            punchSpeed * damagePerSpeedUnit * hitZone.DamageMultiplier,
            maximumDamage
        );

        targetHealth.TakeDamage(damage);

        nextAllowedHitTimes[handId] = currentTime + hitCooldown;

        Debug.Log(
            $"{handTracker.name} hit {hitZone.name}: speed={punchSpeed:F2}, damage={damage:F1}.",
            this
        );
    }
}