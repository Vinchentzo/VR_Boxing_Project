using System.Collections.Generic;
using UnityEngine;

public class HitZoneTrigger : MonoBehaviour
{
    public float damageMultiplier = 1f;
    public float baseDamageMultiplier = 0.6f;
    public float minPunchSpeed = 0.8f;
    public float maxDamage = 25f;

    public float hitCooldownSeconds = 0.25f;

    private Health _health;

    // cooldown per hand instance
    private Dictionary<int, float> _nextAllowedHitTime = new Dictionary<int, float>();

    void Awake()
    {
        _health = GetComponentInParent<Health>();
        if (_health == null)
            Debug.LogWarning("HitZoneTrigger: No Health found in parent hierarchy.");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only hands can cause damage
        if (!other.CompareTag("Hand"))
            return;

        int id = other.gameObject.GetInstanceID();

        float now = Time.time;
        if (_nextAllowedHitTime.TryGetValue(id, out float nextTime) && now < nextTime)
            return;

        var tracker = other.GetComponent<HandVelocityTracker>();
        if (tracker == null)
        {
            Debug.LogWarning("HitZoneTrigger: HandVelocityTracker missing on " + other.name);
            return;
        }

        float speed = tracker.Velocity.magnitude;
        if (speed < minPunchSpeed)
            return;

        float damage = Mathf.Min(speed * baseDamageMultiplier * 10f * damageMultiplier, maxDamage);

        Debug.Log($"HIT ZONE {gameObject.name} by {other.name} speed={speed:F2} damage={damage:F1}");

        _health?.TakeDamage(damage);

        _nextAllowedHitTime[id] = now + hitCooldownSeconds;
    }
}
