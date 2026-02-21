using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float dmg)
    {
        currentHealth = Mathf.Max(0f, currentHealth - dmg);
        Debug.Log($"{gameObject.name} HP: {currentHealth:F1}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Debug.Log($"{gameObject.name} KO!");
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        Debug.Log($"{gameObject.name} reset to full health.");
    }
}

