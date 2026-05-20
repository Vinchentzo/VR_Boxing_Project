using System.Collections;
using UnityEngine;
using TMPro;

public class GameFlowManager : MonoBehaviour
{
    public enum GameState
    {
        MainMenu,
        Fighting,
        Ended
    }

    [Header("State")]
    [SerializeField] private GameState currentState = GameState.MainMenu;
    [SerializeField] private bool startFightOnSceneLoad = false;

    [Header("Enemy")]
    [SerializeField] private Enemy enemy;
    [SerializeField] private Health enemyHealth;
    [SerializeField] private GameObject enemyHealthBarRoot;
    [SerializeField] private PunchTargetHit[] enemyHitReceivers;

    [Header("Player")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerHit[] playerHitReceivers;

    [Header("UI")]
    [SerializeField] private GameObject mainMenuRoot;

    [Header("Menu Interaction")]
    [SerializeField] private GameObject rightMenuRayInteractor;
    [SerializeField] private GameObject leftMenuRayInteractor;

    [Header("Countdown")]
    [SerializeField] private GameObject countdownRoot;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private float countdownStepTime = 1f;

    [Header("Result UI")]
    [SerializeField] private GameObject resultRoot;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private string enemyKOMessage = "YOU WIN";
    [SerializeField] private string playerKOMessage = "KNOCKED OUT";

    public GameState CurrentState => currentState;

    private void Awake()
    {
        FindMissingReferences();

        // Important: stop gameplay immediately before the first frame.
        SetGameplayActive(false);
    }

    private void Start()
    {
        if (enemyHealth != null)
            enemyHealth.OnKO += HandleEnemyKO;

        if (playerHealth != null)
            playerHealth.OnKO += HandlePlayerKO;

        EnterMainMenu();

        if (startFightOnSceneLoad)
            StartFight();
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
            enemyHealth.OnKO -= HandleEnemyKO;

        if (playerHealth != null)
            playerHealth.OnKO -= HandlePlayerKO;
    }

    private void FindMissingReferences()
    {
        if (enemy == null)
        {
            Debug.Log("GameFlowManager: No enemy found.");
            enabled = false;
            return;
        }

        if (enemyHealth == null)
        {
            Debug.Log("GameFlowManager: No enemyHealth found.");
            enabled = false;
            return;
        }

        if (playerHealth == null)
        {
            Debug.Log("GameFlowManager: No playerHealth found.");
            enabled = false;
            return;
        }

        if ((enemyHitReceivers == null || enemyHitReceivers.Length == 0) && enemyHealth != null)
            enemyHitReceivers = enemyHealth.GetComponentsInChildren<PunchTargetHit>(true);

        if ((playerHitReceivers == null || playerHitReceivers.Length == 0) && playerHealth != null)
            playerHitReceivers = playerHealth.GetComponentsInChildren<PlayerHit>(true);
    }

    public void EnterMainMenu()
    {
        currentState = GameState.MainMenu;

        ResetHealth();

        SetGameplayActive(false);

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(true);

        if (rightMenuRayInteractor != null)
            rightMenuRayInteractor.SetActive(true);

        if (leftMenuRayInteractor != null)
            leftMenuRayInteractor.SetActive(true);

        if (enemyHealthBarRoot != null)
            enemyHealthBarRoot.SetActive(false);

        Debug.Log("GameFlowManager: Entered Main Menu state.");
    }

    [ContextMenu("Start Fight")]
    public void StartFight()
    {
        if (currentState == GameState.Fighting)
            return;

        StartCoroutine(StartFightRoutine());
    }

    private IEnumerator StartFightRoutine()
    {
        currentState = GameState.MainMenu;

        ResetHealth();

        SetGameplayActive(false);

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(false);

        if (enemyHealthBarRoot != null)
            enemyHealthBarRoot.SetActive(false);

        if (rightMenuRayInteractor != null)
            rightMenuRayInteractor.SetActive(false);

        if (leftMenuRayInteractor != null)
            leftMenuRayInteractor.SetActive(false);

        if (countdownRoot != null)
            countdownRoot.SetActive(true);

        yield return ShowCountdownText("3");
        yield return ShowCountdownText("2");
        yield return ShowCountdownText("1");
        yield return ShowCountdownText("FIGHT!");

        if (countdownRoot != null)
            countdownRoot.SetActive(false);

        currentState = GameState.Fighting;

        if (enemyHealthBarRoot != null)
            enemyHealthBarRoot.SetActive(true);

        SetGameplayActive(true);

        Debug.Log("GameFlowManager: Fight started.");
    }

    private IEnumerator ShowCountdownText(string text)
    {
        if (countdownText != null)
            countdownText.text = text;

        yield return new WaitForSeconds(countdownStepTime);
    }

    private void ResetHealth()
    {
        if (enemyHealth != null)
            enemyHealth.ResetHealth();

        if (playerHealth != null)
            playerHealth.ResetHealth();
    }

    private void SetGameplayActive(bool active)
    {
        if (enemy != null)
            enemy.enabled = active;

        if (enemyHitReceivers != null)
        {
            foreach (PunchTargetHit hitReceiver in enemyHitReceivers)
            {
                if (hitReceiver != null)
                    hitReceiver.enabled = active;
            }
        }

        if (playerHitReceivers != null)
        {
            foreach (PlayerHit hitReceiver in playerHitReceivers)
            {
                if (hitReceiver != null)
                    hitReceiver.enabled = active;
            }
        }
    }

    private void HandleEnemyKO()
    {
        if (currentState != GameState.Fighting)
            return;

        ShowResult(enemyKOMessage);
    }

    private void HandlePlayerKO()
    {
        if (currentState != GameState.Fighting)
            return;

        ShowResult(playerKOMessage);
    }

    private void ShowResult(string message)
    {
        currentState = GameState.Ended;

        SetGameplayActive(false);

        if (enemyHealthBarRoot != null)
            enemyHealthBarRoot.SetActive(false);

        if (countdownRoot != null)
            countdownRoot.SetActive(false);

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(false);

        if (resultTitleText != null)
            resultTitleText.text = message;

        if (resultRoot != null)
            resultRoot.SetActive(true);

        if (rightMenuRayInteractor != null)
            rightMenuRayInteractor.SetActive(true);

        Debug.Log($"GameFlowManager: Result shown: {message}");
    }

    public void RestartFight()
    {
        if (resultRoot != null)
            resultRoot.SetActive(false);

        StartFight();
    }

    public void BackToMainMenu()
    {
        if (resultRoot != null)
            resultRoot.SetActive(false);

        EnterMainMenu();
    }
}