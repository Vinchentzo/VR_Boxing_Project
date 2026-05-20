using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerDamageEffects : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Volume damageVolume;
    [SerializeField] private Image fullFadeImage;
    [SerializeField] private TMPro.TextMeshProUGUI koText;

    [Header("Low Health Vignette")]
    [SerializeField] private float maxLowHealthIntensity = 0.65f;

    [Header("Hit Flash")]
    [SerializeField] private float hitFlashIntensity = 0.25f;

    [Header("KO Fade")]
    [SerializeField] private float fadeToKOScreen = 0.35f; // used ones to fade to white, then second time to fade to black
    [SerializeField] private float restartDelayAfterBlack = 5f;

    private Vignette vignette;
    private float lowHealthIntensity = 0f;
    private float flashIntensity = 0f;
    private float lastHealth;
    private bool koStarted = false;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponentInParent<PlayerHealth>();

        if (damageVolume != null)
            damageVolume.profile.TryGet(out vignette);
    }

    private void Start()
    {
        if (playerHealth == null)
        {
            Debug.LogError("PlayerDamageEffects: PlayerHealth is not assigned.");
            enabled = false;
            return;
        }

        if (damageVolume == null)
        {
            Debug.LogError("PlayerDamageEffects: Damage Volume is not assigned.");
            enabled = false;
            return;
        }

        if (vignette == null)
        {
            Debug.LogError("PlayerDamageEffects: Vignette override not found in Volume profile.");
            enabled = false;
            return;
        }

        if (fullFadeImage == null)
        {
            Debug.LogError("PlayerDamageEffects: FullFadeImage is not assigned.");
            enabled = false;
            return;
        }

        if (koText == null)
        {
            Debug.LogError("PlayerDamageEffects: KOText is not assigned.");
            enabled = false;
            return;
        }

        koText.enabled = false;

        lastHealth = playerHealth.currentHealth;

        vignette.intensity.value = 0f;
        SetImageAlpha(fullFadeImage, 0f);

        UpdateLowHealthIntensity(playerHealth.currentHealth);

        playerHealth.OnHealthChanged += HandleHealthChanged;
        playerHealth.OnKO += HandleKO;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
            playerHealth.OnKO -= HandleKO;
        }
    }

    private void Update()
    {
        if (koStarted)
            return;

        flashIntensity = Mathf.MoveTowards(
            flashIntensity,
            0f,
            Time.deltaTime
        );

        float finalIntensity = Mathf.Clamp01(lowHealthIntensity + flashIntensity);
        vignette.intensity.value = finalIntensity;
    }

    private void HandleHealthChanged(float currentHealth)
    {
        if (currentHealth < lastHealth && !koStarted)
        {
            flashIntensity = hitFlashIntensity;
        }

        lastHealth = currentHealth;

        UpdateLowHealthIntensity(currentHealth);
    }

    private void UpdateLowHealthIntensity(float currentHealth)
    {
        float healthLost = 1f - Mathf.Clamp01(currentHealth / playerHealth.maxHealth);

        lowHealthIntensity = healthLost * maxLowHealthIntensity;
    }

    private void HandleKO()
    {
        if (koStarted)
            return;

        StartCoroutine(KOSequence());
    }

    private IEnumerator KOSequence()
    {
        koStarted = true;

        koText.text = "KO";
        koText.enabled = true;

        // Remove the edge vignette so the full-screen KO fade takes over.
        vignette.intensity.value = 0f;

        float t = 0f;

        // Fade transparent -> white.
        while (t < fadeToKOScreen)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / fadeToKOScreen);

            //slightly gray color like e small flash
            fullFadeImage.color = new Color(0.01f, 0.01f, 0.01f, a);

            yield return null;
        }

        // Fade white -> black.
        t = 0f;

        while (t < fadeToKOScreen)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / fadeToKOScreen);

            Color prev_color = fullFadeImage.color;
            Color color = Color.Lerp(prev_color, Color.black, progress);
            color.a = 1f;

            fullFadeImage.color = color;

            yield return null;
        }

        fullFadeImage.color = Color.black;

        yield return new WaitForSeconds(restartDelayAfterBlack);

        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        koText.enabled = false;

        fullFadeImage.enabled = false;
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}