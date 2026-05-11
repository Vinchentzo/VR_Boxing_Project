using System.Collections.Generic;
using UnityEngine;

public class PunchTargetHit : MonoBehaviour
{
    public float damageMultiplier = 1f;
    public float minPunchSpeed = 0.8f;
    public float maxDamage = 25f;

    // Prevent multiple hits from the same fist during continuous contact
    public float hitCooldownSeconds = 0.25f;

    private Health _health;

    // cooldown per hand object
    private Dictionary<int, float> _nextAllowedHitTime = new Dictionary<int, float>();

    void Awake()
    {
        _health = GetComponentInParent<Health>();
        if (_health == null)
            Debug.LogWarning("Health component missing on " + gameObject.name);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryApplyHit(collision);
    }

    private void TryApplyHit(Collision collision)
    {
        if (collision.contactCount == 0)
            return;

        var contact = collision.GetContact(0);

        var tracker = contact.otherCollider.GetComponentInParent<HandVelocityTracker>();

        if (tracker == null)
            return;

        GameObject hitter = tracker.gameObject;

        if (!hitter.CompareTag("Hand"))
            return;

        // cooldown by hand instance
        int id = hitter.GetInstanceID();
        float now = Time.time;
        if (_nextAllowedHitTime.TryGetValue(id, out float nextTime) && now < nextTime)
            return;

        float speed = tracker.Velocity.magnitude;

        if (speed < minPunchSpeed)
        {
            Debug.Log($"Punch too slow: {speed:F2}");
            return;
        }

        // Hit zone multiplier from the TARGET collider
        HitZone zone = contact.thisCollider.GetComponent<HitZone>();
        float zoneMultiplier = zone != null ? zone.damageMultiplier : 1f;

        float damage = Mathf.Min(speed * damageMultiplier * 10f * zoneMultiplier, maxDamage);

        Debug.Log($"HIT: \"{hitter.name}\" hit \"{contact.thisCollider.name}\" speed={speed:F2} damage={damage:F1}");
        _health?.TakeDamage(damage);

        _nextAllowedHitTime[id] = now + hitCooldownSeconds;
    }


}
