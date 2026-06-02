using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class PlayerDamageEffects : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private Volume damageVolume;

    [Header("Damage Vignette")]
    [SerializeField, Range(0f, 1f)] private float maxLowHealthIntensity = 0.65f;
    [SerializeField, Range(0f, 1f)] private float hitFlashIntensity = 0.25f;
    [SerializeField, Min(0f)] private float hitFlashFadeSpeed = 1f;

    private Vignette vignette;

    private float lowHealthIntensity;
    private float flashIntensity;
    private float previousHealth;

    private bool referencesValid;

    private void Awake()
    {
        referencesValid = ValidateReferences();

        if (!referencesValid)
        {
            enabled = false;
            return;
        }

        if (!damageVolume.profile.TryGet(out vignette))
        {
            Debug.LogError(
                "PlayerDamageEffects requires a Vignette override in the assigned Volume profile.",
                this
            );

            referencesValid = false;
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (!referencesValid)
            return;

        playerHealth.HealthChanged += HandleHealthChanged;
    }

    private void Start()
    {
        if (referencesValid)
            ResetEffects();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.HealthChanged -= HandleHealthChanged;
    }

    private void Update()
    {
        flashIntensity = Mathf.MoveTowards(
            flashIntensity,
            0f,
            hitFlashFadeSpeed * Time.deltaTime
        );

        vignette.intensity.value = Mathf.Clamp01(lowHealthIntensity + flashIntensity);
    }

    private void HandleHealthChanged(float currentHealth)
    {
        if (Mathf.Approximately(currentHealth, playerHealth.MaxHealth))
        {
            ResetEffects();
            return;
        }

        if (currentHealth < previousHealth)
            flashIntensity = hitFlashIntensity;

        previousHealth = currentHealth;
        UpdateLowHealthIntensity(currentHealth);
    }

    private void UpdateLowHealthIntensity(float currentHealth)
    {
        float healthLostFraction = 1f - Mathf.Clamp01(currentHealth / playerHealth.MaxHealth);
        lowHealthIntensity = healthLostFraction * maxLowHealthIntensity;
    }

    private void ResetEffects()
    {
        flashIntensity = 0f;
        previousHealth = playerHealth.CurrentHealth;

        UpdateLowHealthIntensity(playerHealth.CurrentHealth);
        vignette.intensity.value = lowHealthIntensity;
    }

    private bool ValidateReferences()
    {
        if (playerHealth == null)
        {
            Debug.LogError("PlayerDamageEffects requires the player's Health component.", this);
            return false;
        }

        if (damageVolume == null)
        {
            Debug.LogError("PlayerDamageEffects requires a damage Volume reference.", this);
            return false;
        }

        if (damageVolume.profile == null)
        {
            Debug.LogError("PlayerDamageEffects requires a Volume profile.", this);
            return false;
        }

        return true;
    }
}