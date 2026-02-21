using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public event Action<float> OnHealthChanged; // (current, max)
    public event Action OnKO;

    void Awake()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void TakeDamage(float dmg)
    {
        currentHealth = Mathf.Max(0f, currentHealth - dmg);
        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log($"{gameObject.name} HP: {currentHealth:F1}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            OnKO?.Invoke();
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }
}
    