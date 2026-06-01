using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health targetHealth;
    [SerializeField] private Slider slider;
    [SerializeField] private Transform playerCamera;

    [Header("Visibility")]
    [SerializeField, Range(0f, 180f)] private float showAngleDegrees = 22f;
    [SerializeField, Range(-1f, 1f)] private float minLookUpDot = 0.1f;
    [SerializeField, Min(0f)] private float fadeSpeed = 8f;

    private CanvasGroup canvasGroup;
    private Transform billboardRoot;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        Canvas parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
            billboardRoot = parentCanvas.transform;

        if (!ValidateReferences())
            enabled = false;
    }

    private void OnEnable()
    {
        if (targetHealth == null || slider == null)
            return;

        targetHealth.HealthChanged += HandleHealthChanged;
        UpdateSlider(targetHealth.CurrentHealth);
    }

    private void OnDisable()
    {
        if (targetHealth != null)
            targetHealth.HealthChanged -= HandleHealthChanged;
    }

    private void LateUpdate()
    {
        FacePlayer();
        UpdateVisibility();
    }

    private bool ValidateReferences()
    {
        if (targetHealth == null)
        {
            Debug.LogError("HealthBarUI requires a target Health reference.", this);
            return false;
        }

        if (slider == null)
        {
            Debug.LogError("HealthBarUI requires a Slider reference.", this);
            return false;
        }

        if (playerCamera == null)
        {
            Debug.LogError("HealthBarUI requires a player camera Transform reference.", this);
            return false;
        }

        if (canvasGroup == null)
        {
            Debug.LogError("HealthBarUI requires a CanvasGroup on the same GameObject.", this);
            return false;
        }

        if (billboardRoot == null)
        {
            Debug.LogError("HealthBarUI must be placed under a world-space Canvas.", this);
            return false;
        }

        return true;
    }

    private void HandleHealthChanged(float currentHealth)
    {
        UpdateSlider(currentHealth);
    }

    private void UpdateSlider(float currentHealth)
    {
        slider.maxValue = targetHealth.MaxHealth;
        slider.value = currentHealth;
    }

    private void FacePlayer()
    {
        Vector3 directionFromCameraToBar = billboardRoot.position - playerCamera.position;

        if (directionFromCameraToBar.sqrMagnitude < 0.0001f)
            return;

        billboardRoot.rotation = Quaternion.LookRotation(
            directionFromCameraToBar.normalized,
            Vector3.up
        );
    }

    private void UpdateVisibility()
    {
        Vector3 directionToBar = billboardRoot.position - playerCamera.position;

        if (directionToBar.sqrMagnitude < 0.0001f)
        {
            FadeTo(0f);
            return;
        }

        directionToBar.Normalize();

        float angle = Vector3.Angle(playerCamera.forward, directionToBar);
        bool lookingNearBar = angle <= showAngleDegrees;

        float upwardLookAmount = Vector3.Dot(playerCamera.forward, Vector3.up);
        bool lookingUpEnough = upwardLookAmount >= minLookUpDot;

        FadeTo(lookingNearBar && lookingUpEnough ? 1f : 0f);
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