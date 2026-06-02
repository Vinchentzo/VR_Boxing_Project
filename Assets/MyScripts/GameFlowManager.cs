using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameFlowManager : MonoBehaviour
{
    private enum GameState
    {
        MainMenu,
        Countdown,
        Fighting,
        Ended
    }

    [Header("State")]
    [SerializeField] private GameState currentState = GameState.MainMenu;
    [SerializeField] private bool startFightOnSceneLoad;

    [Header("Enemy")]
    [SerializeField] private Enemy enemy;
    [SerializeField] private Health enemyHealth;
    [SerializeField] private PunchTargetHit enemyHitReceiver;
    [SerializeField] private GameObject enemyHealthBarRoot;

    [Header("Player")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerHit[] playerHitReceivers;
    [SerializeField] private GameObject locomotionSystem;

    [Header("Result Transition")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Image blackFadeImage;
    [SerializeField, Min(0f)] private float resultDelay = 2f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.35f;

    [Header("Menu")]
    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private GameObject rightMenuRayInteractor;
    [SerializeField] private GameObject leftMenuRayInteractor;

    [Header("Countdown")]
    [SerializeField] private GameObject countdownRoot;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField, Min(0.01f)] private float countdownStepTime = 1f;

    [Header("Result")]
    [SerializeField] private GameObject resultRoot;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private string enemyKnockoutMessage = "YOU WIN";
    [SerializeField] private string playerKnockoutMessage = "KNOCKED OUT";

    private Coroutine countdownRoutine;
    private bool referencesValid;

    private Vector3 startingPlayerViewPosition;

    private void Awake()
    {
        referencesValid = ValidateReferences();

        if (!referencesValid)
        {
            enabled = false;
            return;
        }

        startingPlayerViewPosition = playerCamera.position;

        SetBlackFadeAlpha(0f);
        blackFadeImage.enabled = false;

        SetGameplayActive(false);
    }

    private void OnEnable()
    {
        if (!referencesValid)
            return;

        enemyHealth.KnockedOut += HandleEnemyKnockedOut;
        playerHealth.KnockedOut += HandlePlayerKnockedOut;
    }

    private void Start()
    {
        if (!referencesValid)
            return;

        if (startFightOnSceneLoad)
        {
            StartFight();
            return;
        }

        EnterMainMenu();
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
            enemyHealth.KnockedOut -= HandleEnemyKnockedOut;

        if (playerHealth != null)
            playerHealth.KnockedOut -= HandlePlayerKnockedOut;

        StopCountdown();
    }

    public void EnterMainMenu()
    {
        if (!referencesValid)
            return;

        StopCountdown();

        currentState = GameState.MainMenu;

        SetGameplayActive(false);
        ResetFight();

        mainMenuRoot.SetActive(true);
        countdownRoot.SetActive(false);
        resultRoot.SetActive(false);
        enemyHealthBarRoot.SetActive(false);

        SetMenuInteractionActive(true);

        Debug.Log("Entered Main Menu state.", this);
    }

    public void StartFight()
    {
        if (!referencesValid)
            return;

        if (currentState == GameState.Countdown || currentState == GameState.Fighting)
            return;

        StopCountdown();
        countdownRoutine = StartCoroutine(StartFightRoutine());
    }

    public void RestartFight()
    {
        if (currentState != GameState.Ended)
            return;

        StartFight();
    }

    public void BackToMainMenu()
    {
        EnterMainMenu();
    }

    private IEnumerator StartFightRoutine()
    {
        currentState = GameState.Countdown;

        SetGameplayActive(false);
        ResetFight();

        mainMenuRoot.SetActive(false);
        resultRoot.SetActive(false);
        enemyHealthBarRoot.SetActive(false);
        SetMenuInteractionActive(false);

        countdownRoot.SetActive(true);

        yield return ShowCountdownText("3");
        yield return ShowCountdownText("2");
        yield return ShowCountdownText("1");
        yield return ShowCountdownText("FIGHT!");

        countdownRoot.SetActive(false);

        currentState = GameState.Fighting;

        enemyHealthBarRoot.SetActive(true);
        SetGameplayActive(true);

        countdownRoutine = null;

        Debug.Log("Fight started.", this);
    }

    private IEnumerator ShowCountdownText(string text)
    {
        countdownText.text = text;
        yield return new WaitForSeconds(countdownStepTime);
    }

    private void HandleEnemyKnockedOut()
    {
        if (currentState != GameState.Fighting)
            return;

        StartCoroutine(ShowResultTransition(enemyKnockoutMessage));
    }

    private void HandlePlayerKnockedOut()
    {
        if (currentState != GameState.Fighting)
            return;

        StartCoroutine(ShowResultTransition(playerKnockoutMessage));
    }

    private IEnumerator ShowResultTransition(string message)
    {
        currentState = GameState.Ended;

        SetGameplayActive(false);

        mainMenuRoot.SetActive(false);
        countdownRoot.SetActive(false);
        resultRoot.SetActive(false);
        enemyHealthBarRoot.SetActive(false);

        SetMenuInteractionActive(false);

        yield return new WaitForSeconds(resultDelay);

        yield return FadeScreen(1f);

        RecenterPlayerToStartingPosition();
        ResetFight();

        resultTitleText.text = message;
        resultRoot.SetActive(true);

        yield return FadeScreen(0f);

        SetMenuInteractionActive(true);

        Debug.Log($"Result shown: {message}", this);
    }

    private void ResetFight()
    {
        enemy.ResetForFight();
        enemyHealth.ResetHealth();
        playerHealth.ResetHealth();
    }

    private void RecenterPlayerToStartingPosition()
    {
        Vector3 positionDifference = startingPlayerViewPosition - playerCamera.position;

        xrOrigin.position += new Vector3(
            positionDifference.x,
            0f,
            positionDifference.z
        );
    }

    private IEnumerator FadeScreen(float targetAlpha)
    {
        blackFadeImage.enabled = true;

        float startingAlpha = blackFadeImage.color.a;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / fadeDuration);
            float alpha = Mathf.Lerp(startingAlpha, targetAlpha, progress);

            SetBlackFadeAlpha(alpha);

            yield return null;
        }

        SetBlackFadeAlpha(targetAlpha);

        if (Mathf.Approximately(targetAlpha, 0f))
            blackFadeImage.enabled = false;
    }

    private void SetBlackFadeAlpha(float alpha)
    {
        blackFadeImage.color = new Color(0f, 0f, 0f, alpha);
    }

    private void SetGameplayActive(bool active)
    {
        enemy.enabled = active;
        enemyHitReceiver.enabled = active;
        locomotionSystem.SetActive(active);

        foreach (PlayerHit playerHitReceiver in playerHitReceivers)
            playerHitReceiver.enabled = active;
    }

    private void SetMenuInteractionActive(bool active)
    {
        rightMenuRayInteractor.SetActive(active);
        leftMenuRayInteractor.SetActive(active);
    }

    private void StopCountdown()
    {
        if (countdownRoutine == null)
            return;

        StopCoroutine(countdownRoutine);
        countdownRoutine = null;

        if (countdownRoot != null)
            countdownRoot.SetActive(false);
    }

    private bool ValidateReferences()
    {
        if (enemy == null)
        {
            Debug.LogError("GameFlowManager requires an Enemy reference.", this);
            return false;
        }

        if (enemyHealth == null)
        {
            Debug.LogError("GameFlowManager requires the enemy Health reference.", this);
            return false;
        }

        if (enemyHitReceiver == null)
        {
            Debug.LogError("GameFlowManager requires the enemy PunchTargetHit reference.", this);
            return false;
        }

        if (enemyHealthBarRoot == null)
        {
            Debug.LogError(
                "GameFlowManager requires the enemy health bar root object while the health bar is in use.",
                this
            );
            return false;
        }

        if (playerHealth == null)
        {
            Debug.LogError("GameFlowManager requires the player Health reference.", this);
            return false;
        }

        if (locomotionSystem == null)
        {
            Debug.LogError("GameFlowManager requires the player's Locomotion System object.", this);
            return false;
        }

        if (xrOrigin == null)
        {
            Debug.LogError("GameFlowManager requires the XR Origin Transform.", this);
            return false;
        }

        if (playerCamera == null)
        {
            Debug.LogError("GameFlowManager requires the player camera Transform.", this);
            return false;
        }

        if (blackFadeImage == null)
        {
            Debug.LogError("GameFlowManager requires the black fade Image.", this);
            return false;
        }

        if (enemyHealth == playerHealth)
        {
            Debug.LogError("Enemy Health and Player Health must reference different Health components.", this);
            return false;
        }

        if (playerHitReceivers == null || playerHitReceivers.Length == 0)
        {
            Debug.LogError("GameFlowManager requires at least one player PlayerHit receiver.", this);
            return false;
        }

        foreach (PlayerHit playerHitReceiver in playerHitReceivers)
        {
            if (playerHitReceiver == null)
            {
                Debug.LogError("GameFlowManager has an unassigned player hit receiver.", this);
                return false;
            }
        }

        if (mainMenuRoot == null)
        {
            Debug.LogError("GameFlowManager requires the main menu root object.", this);
            return false;
        }

        if (rightMenuRayInteractor == null || leftMenuRayInteractor == null)
        {
            Debug.LogError("GameFlowManager requires both menu ray interactor objects.", this);
            return false;
        }

        if (countdownRoot == null || countdownText == null)
        {
            Debug.LogError("GameFlowManager requires its countdown root and countdown text.", this);
            return false;
        }

        if (resultRoot == null || resultTitleText == null)
        {
            Debug.LogError("GameFlowManager requires its result root and result title text.", this);
            return false;
        }

        return true;
    }
}