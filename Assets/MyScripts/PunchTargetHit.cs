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

    //not sure if there is a difference
    private void OnCollisionStay(Collision collision)
    {
        // Optional: allow repeated hits while staying in contact, gated by cooldown.
        // This helps if contact persists for a short time.
        TryApplyHit(collision);
        //TryApplyHit2(collision.gameObject);
    }

    // on the colliders code (not working without rigid bodies)
    //private void TryApplyHit(Collision collision)
    //{

    //    GameObject hitter = collision.gameObject;
    //    if (!hitter.CompareTag("Hand"))
    //        return;

    //    int id = hitter.GetInstanceID();
    //    float now = Time.time;

    //    if (_nextAllowedHitTime.TryGetValue(id, out float nextTime) && now < nextTime)
    //        return;

    //    var tracker = hitter.GetComponent<HandVelocityTracker>();
    //    if (tracker == null) return;

    //    float speed = tracker.Velocity.magnitude;
    //    if (speed < minPunchSpeed)
    //        return;

    //    float damage = Mathf.Min(speed * damageMultiplier * 10f, maxDamage);

    //    Debug.Log($"HIT {collision.collider.name}  speed={speed:F2}  damage={damage:F1}");

    //    if (_health != null)
    //        _health.TakeDamage(damage);

    //    _nextAllowedHitTime[id] = now + hitCooldownSeconds;
    //}

    // old code 
    //private void TryApplyHit(GameObject hitter)
    //{
    //    //if (!hitter.name.Contains("Hand"))
    //    //    return;
    //    if (!hitter.CompareTag("Hand"))
    //        return;

    //    int id = hitter.GetInstanceID();

    //    float now = Time.time;
    //    if (_nextAllowedHitTime.TryGetValue(id, out float nextTime) && now < nextTime)
    //        return;

    //    var tracker = hitter.GetComponent<HandVelocityTracker>();
    //    if (tracker == null) return;

    //    float speed = tracker.Velocity.magnitude;
    //    if (speed < minPunchSpeed)
    //        return;

    //    float damage = Mathf.Min(speed * damageMultiplier * 10f, maxDamage);

    //    Debug.Log($"PUNCH! {hitter.name} speed={speed:F2}  damage={damage:F1}");

    //    if (_health != null)
    //        _health.TakeDamage(damage);

    //    _nextAllowedHitTime[id] = now + hitCooldownSeconds;
    //}






    //getContact method
    private void TryApplyHit(Collision collision)
    {
        var contact = collision.GetContact(0);

        // Make sure the OTHER collider is a hand
        if (!contact.otherCollider.CompareTag("Hand"))
            return;

        var hitter = contact.otherCollider.gameObject;

        // cooldown by hand instance
        int id = hitter.GetInstanceID();
        float now = Time.time;
        if (_nextAllowedHitTime.TryGetValue(id, out float nextTime) && now < nextTime)
            return;

        var tracker = hitter.GetComponent<HandVelocityTracker>();
        if (tracker == null) return;

        float speed = tracker.Velocity.magnitude;
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
