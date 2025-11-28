using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Players")]
    [SerializeField] private PlayerHealth player1;
    [SerializeField] private PlayerHealth player2;

    [Header("Spawn Points")]
    [SerializeField] private Transform player1Spawn;
    [SerializeField] private Transform player2Spawn;

    [Header("Round Settings")]
    [SerializeField] private int roundsToWin = 3;
    [SerializeField] private float roundRestartDelay = 3f;

    [Header("UI")]
    [SerializeField] private Text roundText;
    [SerializeField] private Text player1ScoreText;
    [SerializeField] private Text player2ScoreText;
    [SerializeField] private GameObject bannerRoot;
    [SerializeField] private Text bannerText;
    [SerializeField] private float bannerDisplayTime = 2f;

    private int player1Score = 0;
    private int player2Score = 0;
    private int currentRound = 1;
    private bool roundOver = false;
    private bool matchOver = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (player1 == null || player2 == null)
        {
            Debug.LogError("[RoundManager] Players not assigned!");
            return;
        }

        // Hide banner at start
        if (bannerRoot != null)
            bannerRoot.SetActive(false);

        StartNewRound();
    }

    private void StartNewRound()
    {
        roundOver = false;
        Debug.Log($"[RoundManager] Starting round {currentRound}");

        // Respawn both players
        if (player1 != null) player1.RespawnAt(player1Spawn);
        if (player2 != null) player2.RespawnAt(player2Spawn);

        UpdateScoreUI();

        // Show "ROUND X" banner
        ShowBanner($"ROUND {currentRound}");
    }

    public void OnPlayerDied(PlayerHealth deadPlayer)
    {
        if (roundOver || matchOver) return;
        roundOver = true;

        string roundWinMessage = "";

        if (deadPlayer == player1)
        {
            player2Score++;
            roundWinMessage = "PLAYER 2 WINS THE ROUND!";
            Debug.Log($"[RoundManager] Player 2 wins round {currentRound}. Score P1:{player1Score} P2:{player2Score}");
        }
        else if (deadPlayer == player2)
        {
            player1Score++;
            roundWinMessage = "PLAYER 1 WINS THE ROUND!";
            Debug.Log($"[RoundManager] Player 1 wins round {currentRound}. Score P1:{player1Score} P2:{player2Score}");
        }
        else
        {
            Debug.LogWarning("[RoundManager] Unknown player died.");
        }

        UpdateScoreUI();
        ShowBanner(roundWinMessage);

        // Check for match win
        if (player1Score >= roundsToWin || player2Score >= roundsToWin)
        {
            matchOver = true;
            string matchWinner = (player1Score > player2Score) ? "PLAYER 1" : "PLAYER 2";
            Debug.Log($"[RoundManager] MATCH OVER! Winner = {matchWinner}");
            ShowBanner($"{matchWinner} WINS THE MATCH!");

            // Here you could: disable controls, show main menu button, etc.
            return;
        }

        // Otherwise, start next round after delay
        StartCoroutine(RoundRestartRoutine());
    }

    private IEnumerator RoundRestartRoutine()
    {
        yield return new WaitForSeconds(roundRestartDelay);
        currentRound++;
        StartNewRound();
    }

    private void UpdateScoreUI()
    {
        if (roundText != null)
            roundText.text = $"Round {currentRound}";

        if (player1ScoreText != null)
            player1ScoreText.text = $"P1: {player1Score}";

        if (player2ScoreText != null)
            player2ScoreText.text = $"P2: {player2Score}";
    }

    private void ShowBanner(string message)
    {
        if (bannerRoot == null || bannerText == null)
            return;

        bannerText.text = message;
        bannerRoot.SetActive(true);

        // restart timer coroutine
        StopAllCoroutines();
        StartCoroutine(BannerAutoHideRoutine());
    }

    private IEnumerator BannerAutoHideRoutine()
    {
        yield return new WaitForSeconds(bannerDisplayTime);
        if (!matchOver && bannerRoot != null)
            bannerRoot.SetActive(false);
    }
}


