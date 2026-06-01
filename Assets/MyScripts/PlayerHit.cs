using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private HitZone hitZone;

    private void Awake()
    {
        if (!ValidateReferences())
            enabled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contactCount == 0)
            return;

        ContactPoint contact = collision.GetContact(0);

        if (!contact.otherCollider.CompareTag("EnemyHand"))
            return;

        Enemy enemy = contact.otherCollider.GetComponentInParent<Enemy>();

        if (enemy == null)
        {
            Debug.LogWarning(
                $"PlayerHit received a collision from an EnemyHand without an Enemy component: {contact.otherCollider.name}.",
                contact.otherCollider
            );
            return;
        }

        if (!enemy.TryConsumeAttackHit(out float attackDamage))
            return;

        float damage = attackDamage * hitZone.DamageMultiplier;

        Debug.Log(
            $"{enemy.name} hit {hitZone.name}: damage={damage:F1}.",
            this
        );

        playerHealth.TakeDamage(damage);
    }

    private bool ValidateReferences()
    {
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHit requires the player's Health component.", this);
            return false;
        }

        if (hitZone == null)
        {
            Debug.LogError("PlayerHit requires a HitZone reference.", this);
            return false;
        }

        return true;
    }
}