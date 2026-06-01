using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsKnockedOut { get; private set; }

    public event Action<float> HealthChanged;
    public event Action KnockedOut;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        IsKnockedOut = false;
    }

    public void TakeDamage(float damage)
    {
        if (IsKnockedOut || damage <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        HealthChanged?.Invoke(CurrentHealth);

        Debug.Log($"{name} HP: {CurrentHealth:F1}/{MaxHealth:F1}", this);

        if (CurrentHealth > 0f)
            return;

        IsKnockedOut = true;

        Debug.Log($"{name} knocked out.", this);
        KnockedOut?.Invoke();
    }

    public void ResetHealth()
    {
        IsKnockedOut = false;
        CurrentHealth = maxHealth;
        HealthChanged?.Invoke(CurrentHealth);
    }
}