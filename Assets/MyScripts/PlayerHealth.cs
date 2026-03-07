using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    void Awake() { currentHealth = maxHealth; }

    public void TakeDamage(float dmg)
    {
        currentHealth = Mathf.Max(0f, currentHealth - dmg);
        Debug.Log($"PLAYER HP: {currentHealth:F1}/{maxHealth}");
        if (currentHealth <= 0f) Debug.Log("PLAYER KO!");
    }
}