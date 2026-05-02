using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Health")]
    public Health targetHealth;
    public Slider slider;
    [SerializeField] private float resetTime = 1.5f;

    [SerializeField] private Transform playerCamera;
    [SerializeField] private float showAngleDegrees = 22f;
    [SerializeField] private float minLookUpDot = 0.05f;
    [SerializeField] private float fadeSpeed = 8f;

    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;
    private Transform billboardRoot;
    private bool koScheduled = false;

    private void Awake()
    {
        if (targetHealth == null) Debug.LogError("HealthBarUI: targetHealth not assigned.");
        if (slider == null) Debug.LogError("HealthBarUI: slider not assigned.");

        parentCanvas = GetComponentInParent<Canvas>();

        // Rotate the world-space canvas, not only the slider child.
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
            billboardRoot = parentCanvas.transform;
        else
            billboardRoot = transform;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) Debug.LogError("HealthBarUI: canvasGroup not found.");
    }

    private void Start()
    {
        if (targetHealth == null)
        {
            Debug.LogError("HealthBarUI: targetHealth not assigned and could not be found in parent.");
            enabled = false;
            return;
        }

        if (slider == null)
        {
            Debug.LogError("HealthBarUI: slider not assigned and could not be found.");
            enabled = false;
            return;
        }

        slider.maxValue = targetHealth.maxHealth;
        slider.value = targetHealth.currentHealth;

        targetHealth.OnHealthChanged += HandleHealthChanged;
        targetHealth.OnKO += HandleKO;
    }

    private void OnDestroy()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= HandleHealthChanged;
            targetHealth.OnKO -= HandleKO;
        }
    }

    private void LateUpdate()
    {
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        if (playerCamera == null)
            return;

        FacePlayer();
        UpdateVisibility();
    }

    private void HandleHealthChanged(float current)
    {
        if (slider == null)
            return;

        slider.maxValue = targetHealth.maxHealth;
        slider.value = current;
    }

    private void HandleKO()
    {
        if (koScheduled)
            return;

        koScheduled = true;
        Debug.Log($"KO! Resetting in {resetTime}s...");
        Invoke(nameof(ResetTarget), resetTime);
    }

    private void ResetTarget()
    {
        koScheduled = false;
        targetHealth.ResetHealth();
    }

    private void FacePlayer()
    {
        if (billboardRoot == null)
            return;

        Vector3 directionFromCameraToBar = billboardRoot.position - playerCamera.position;

        if (directionFromCameraToBar.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(directionFromCameraToBar.normalized, Vector3.up);

        billboardRoot.rotation = targetRotation;
    }

    private void UpdateVisibility()
    {
        Vector3 toBar = billboardRoot.position - playerCamera.position;

        if (toBar.sqrMagnitude < 0.0001f)
        {
            FadeTo(0f);
            return;
        }

        Vector3 directionToBar = toBar.normalized;

        float angle = Vector3.Angle(playerCamera.forward, directionToBar);
        bool lookingNearBar = angle <= showAngleDegrees;

        float upwardLookAmount = Vector3.Dot(playerCamera.forward, Vector3.up);
        bool lookingUpEnough = upwardLookAmount >= minLookUpDot;

        bool shouldShow = lookingNearBar && lookingUpEnough;

        FadeTo(shouldShow ? 1f : 0f);
    }

    private void FadeTo(float targetAlpha)
    {
        canvasGroup.alpha = Mathf.MoveTowards(
            canvasGroup.alpha,
            targetAlpha,
            fadeSpeed * Time.deltaTime
        );
    }
}