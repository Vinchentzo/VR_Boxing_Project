using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Health targetHealth;
    public Slider slider;
    private float resetTime = 1.5f;

    void Start()
    {
        if (targetHealth == null) Debug.LogError("HealthBarUI: targetHealth not assigned.");
        if (slider == null) Debug.LogError("HealthBarUI: slider not assigned.");

        // Initialize
        slider.maxValue = targetHealth.maxHealth;
        slider.value = targetHealth.currentHealth;

        // Subscribe
        targetHealth.OnHealthChanged += HandleHealthChanged;
        targetHealth.OnKO += HandleKO;
    }

    void OnDestroy()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= HandleHealthChanged;
            targetHealth.OnKO -= HandleKO;
        }
    }

    private void HandleHealthChanged(float current)
    {
        //slider.maxValue = max;
        slider.value = current;
    }

    private void HandleKO()
    {
        Debug.Log($"KO! Resetting in {resetTime}s...");
        Invoke(nameof(ResetTarget), resetTime);
    }

    private void ResetTarget()
    {
        targetHealth.ResetHealth();
    }
}
