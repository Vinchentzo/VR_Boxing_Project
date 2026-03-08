using System.Collections.Generic;
using UnityEngine;

public class PlayerHit : MonoBehaviour
{
    public float damageMultiplier = 1f;
    public float minPunchSpeed = 0.8f;
    public float maxDamage = 25f;

    // Prevent multiple hits from the same fist during continuous contact
    public float hitCooldownSeconds = 0.25f;

    private PlayerHealth _health;

    // cooldown per hand object
    private Dictionary<int, float> _nextAllowedHitTime = new Dictionary<int, float>();

    void Awake()
    {
        _health = GetComponentInParent<PlayerHealth>();
        if (_health == null)
            Debug.LogWarning("Health component missing on " + gameObject.name);

    }

    private void OnCollisionEnter(Collision collision)
    {
        TryApplyHit(collision);
    }

    //private void OnCollisionStay(Collision collision)
    //{
    //    Debug.LogWarning("STAY with " + collision.collider.name);
    //}

    //private void OnTriggerEnter(Collider other)
    //{
    //    Debug.LogWarning("TRIGGER with " + other.name);
    //}

    private void TryApplyHit(Collision collision)
    {
        var contact = collision.GetContact(0);

        // Make sure the OTHER collider is a hand
        if (!contact.otherCollider.CompareTag("EnemyHand")) 
            return;

        var hitter = contact.otherCollider.gameObject;

        // cooldown by hand instance
        int id = hitter.GetInstanceID();
        float now = Time.time;
        if (_nextAllowedHitTime.TryGetValue(id, out float nextTime) && now < nextTime)
            return;

        var tracker = hitter.GetComponent<HandVelocityTracker>();
        float speed;

        if (tracker == null) speed = 1f;
        else speed = tracker.Velocity.magnitude;

        if (speed < minPunchSpeed) return;

        // Hit zone multiplier from the TARGET collider
        HitZone zone = contact.thisCollider.GetComponent<HitZone>();
        float zoneMultiplier = zone != null ? zone.damageMultiplier : 1f;

        float damage = Mathf.Min(speed * damageMultiplier * 10f * zoneMultiplier, maxDamage);

        Debug.Log($"HIT: \"{hitter.name}\" hit \"{contact.thisCollider.name}\" speed={speed:F2} damage={damage:F1}");
        _health?.TakeDamage(damage);

        _nextAllowedHitTime[id] = now + hitCooldownSeconds;
    }


}
