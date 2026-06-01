using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerDamageEffects : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private Volume damageVolume;
    [SerializeField] private Image fullFadeImage;
    [SerializeField] private TextMeshProUGUI koText;

    [Header("Damage Vignette")]
    [SerializeField, Range(0f, 1f)] private float maxLowHealthIntensity = 0.65f;
    [SerializeField, Range(0f, 1f)] private float hitFlashIntensity = 0.25f;
    [SerializeField, Min(0f)] private float hitFlashFadeSpeed = 1f;

    [Header("KO Screen")]
    [SerializeField, Min(0.01f)] private float koFadeDuration = 0.35f;
    [SerializeField, Min(0f)] private float koScreenDuration = 5f;

    private Vignette vignette;
    private Coroutine knockoutSequence;
    private float lowHealthIntensity;
    private float flashIntensity;
    private float previousHealth;
    private bool knockoutStarted;
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

            enabled = false;
            referencesValid = false;
        }
    }

    private void OnEnable()
    {
        if (!referencesValid)
            return;

        playerHealth.HealthChanged += HandleHealthChanged;
        playerHealth.KnockedOut += HandleKnockedOut;
    }

    private void Start()
    {
        if (referencesValid)
            ResetEffects();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= HandleHealthChanged;
            playerHealth.KnockedOut -= HandleKnockedOut;
        }

        if (knockoutSequence != null)
        {
            StopCoroutine(knockoutSequence);
            knockoutSequence = null;
        }
    }

    private void Update()
    {
        if (knockoutStarted)
            return;

        flashIntensity = Mathf.MoveTowards(
            flashIntensity,
            0f,
            hitFlashFadeSpeed * Time.deltaTime
        );

        vignette.intensity.value = Mathf.Clamp01(lowHealthIntensity + flashIntensity);
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

        if (fullFadeImage == null)
        {
            Debug.LogError("PlayerDamageEffects requires a full-screen fade Image.", this);
            return false;
        }

        if (koText == null)
        {
            Debug.LogError("PlayerDamageEffects requires a KO text reference.", this);
            return false;
        }

        return true;
    }

    private void HandleHealthChanged(float currentHealth)
    {
        if (Mathf.Approximately(currentHealth, playerHealth.MaxHealth))
        {
            ResetEffects();
            return;
        }

        if (currentHealth < previousHealth && !knockoutStarted)
            flashIntensity = hitFlashIntensity;

        previousHealth = currentHealth;
        UpdateLowHealthIntensity(currentHealth);
    }

    private void HandleKnockedOut()
    {
        if (knockoutStarted)
            return;

        knockoutSequence = StartCoroutine(KnockoutSequence());
    }

    private void UpdateLowHealthIntensity(float currentHealth)
    {
        float healthLostFraction = 1f - Mathf.Clamp01(currentHealth / playerHealth.MaxHealth);
        lowHealthIntensity = healthLostFraction * maxLowHealthIntensity;
    }

    private void ResetEffects()
    {
        if (knockoutSequence != null)
        {
            StopCoroutine(knockoutSequence);
            knockoutSequence = null;
        }

        knockoutStarted = false;
        flashIntensity = 0f;
        previousHealth = playerHealth.CurrentHealth;

        UpdateLowHealthIntensity(playerHealth.CurrentHealth);
        vignette.intensity.value = lowHealthIntensity;

        koText.enabled = false;

        fullFadeImage.enabled = true;
        SetFadeImageAlpha(0f);
    }

    private IEnumerator KnockoutSequence()
    {
        knockoutStarted = true;

        koText.text = "KO";
        koText.enabled = true;

        vignette.intensity.value = 0f;

        fullFadeImage.enabled = true;
        fullFadeImage.color = Color.black;
        SetFadeImageAlpha(0f);

        float elapsedTime = 0f;

        while (elapsedTime < koFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            SetFadeImageAlpha(Mathf.Clamp01(elapsedTime / koFadeDuration));
            yield return null;
        }

        SetFadeImageAlpha(1f);

        yield return new WaitForSeconds(koScreenDuration);

        koText.enabled = false;
        fullFadeImage.enabled = false;

        knockoutSequence = null;
    }

    private void SetFadeImageAlpha(float alpha)
    {
        Color color = fullFadeImage.color;
        color.a = alpha;
        fullFadeImage.color = color;
    }
}