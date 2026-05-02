using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public event Action<float> OnHealthChanged;
    public event Action OnKO;

    private bool isKOed = false;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void TakeDamage(float dmg)
    {
        if (isKOed)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - dmg);
        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log($"PLAYER HP: {currentHealth:F1}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            isKOed = true;
            Debug.Log("PLAYER KO!");
            OnKO?.Invoke();
        }
    }

    public void ResetHealth()
    {
        isKOed = false;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }
}