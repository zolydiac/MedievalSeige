using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [Tooltip("Number of round wins needed to win the match (e.g. 3 for BO5).")]
    [SerializeField] private int roundsToWin = 3;
    [Tooltip("Maximum number of rounds that will ever be played (e.g. 5 for BO5).")]
    [SerializeField] private int maxRounds = 5;
    [Tooltip("Time (seconds) between rounds, AFTER win banner etc.")]
    [SerializeField] private float roundRestartDelay = 3f;
    [Tooltip("Pre-round countdown before players can move.")]
    [SerializeField] private float preRoundCountdown = 3f;

    [Header("Score UI (can be any canvas)")]
    [SerializeField] private Text roundText;
    [SerializeField] private Text player1ScoreText;
    [SerializeField] private Text player2ScoreText;

    [Header("Round / Win Banners")]
    [SerializeField] private GameObject bannerRootP1;
    [SerializeField] private TMP_Text bannerTextP1;
    [SerializeField] private GameObject bannerRootP2;
    [SerializeField] private TMP_Text bannerTextP2;
    [SerializeField] private float bannerDisplayTime = 2f;

    [Header("Bomb Settings")]
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private Transform[] bombSites;
    [SerializeField] private float bombPlantRadius = 3f;
    [SerializeField] private float bombDuration = 40f;
    [SerializeField] private float bombDefuseTime = 8f;

    [Header("Bomb Timer UI")]
    [SerializeField] private TMP_Text bombTimerTextP1;
    [SerializeField] private TMP_Text bombTimerTextP2;

    [Header("Sides")]
    [Tooltip("If true, Player 1 starts as attacker, Player 2 as defender.")]
    [SerializeField] private bool player1IsAttacker = true;

    // --------- PAUSE SUPPORT ----------
    private bool gamePaused = false;
    public bool IsGamePaused => gamePaused;

    public void SetGamePaused(bool paused)
    {
        gamePaused = paused;
        // NOTE: we do NOT disable player input here � Time.timeScale handles the actual pause.
    }
    // ----------------------------------

    // Internal state
    private int player1Score = 0;
    private int player2Score = 0;
    private int currentRound = 1;

    private bool roundOver = false;
    private bool matchOver = false;

    private BombController activeBomb = null;

    public BombController ActiveBomb => activeBomb;

    private Coroutine bannerHideRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (player1 == null || player2 == null)
        {
            Debug.LogError("[RoundManager] Players not assigned!");
            return;
        }

        // Ensure bomb timer UI is hidden at start.
        SetBombTimerVisible(false);

        StartMatch();
    }

    // ---------------------------------------------------------------------
    // MATCH / ROUND LIFECYCLE
    // ---------------------------------------------------------------------

    private void StartMatch()
    {
        matchOver = false;
        roundOver = false;
        player1Score = 0;
        player2Score = 0;
        currentRound = 1;

        UpdateScoreUI();
        StartNewRound();
    }

    private void StartNewRound()
    {
        roundOver = false;

        // Clean up any existing bomb.
        if (activeBomb != null)
        {
            Destroy(activeBomb.gameObject);
            activeBomb = null;
        }
        SetBombTimerVisible(false);

        // Respawn both players at their spawns.
        if (player1 != null) player1.RespawnAt(player1Spawn);
        if (player2 != null) player2.RespawnAt(player2Spawn);

        UpdateScoreUI();

        // Start pre-round countdown that also locks controls.
        StartCoroutine(PreRoundCountdownRoutine());
    }

    private IEnumerator PreRoundCountdownRoutine()
    {
        // Lock controls for both players during countdown.
        SetPlayersInputEnabled(false);

        float countdown = Mathf.Max(0.1f, preRoundCountdown);
        int lastShown = -1;

        while (countdown > 0f)
        {
            int seconds = Mathf.CeilToInt(countdown);
            if (seconds != lastShown)
            {
                lastShown = seconds;
                ShowBannerForBoth(seconds.ToString());
            }

            countdown -= Time.deltaTime;
            yield return null;
        }

        // Brief "FIGHT!" banner.
        ShowBannerForBoth($"ROUND {currentRound} - FIGHT!");
        yield return new WaitForSeconds(0.7f);

        HideBannerImmediate();

        // Unlock controls so round can actually play.
        SetPlayersInputEnabled(true);
    }

    // ---------------------------------------------------------------------
    // PUBLIC API CALLED BY OTHER SCRIPTS
    // ---------------------------------------------------------------------

    public void OnPlayerDied(PlayerHealth deadPlayer)
    {
        if (roundOver || matchOver) return;
        roundOver = true;

        bool p1WonRound = false;
        string message = "";

        if (deadPlayer == player1)
        {
            player2Score++;
            p1WonRound = false;
            message = "PLAYER 2 WINS THE ROUND!";
        }
        else if (deadPlayer == player2)
        {
            player1Score++;
            p1WonRound = true;
            message = "PLAYER 1 WINS THE ROUND!";
        }
        else
        {
            Debug.LogWarning("[RoundManager] Unknown player died.");
            return;
        }

        Debug.Log("[RoundManager] " + message);
        UpdateScoreUI();
        ShowBannerForBoth(message);

        HandleRoundFinished(p1WonRound);
    }

    public void OnBombExploded()
    {
        if (roundOver || matchOver) return;
        roundOver = true;

        bool attackerWonRound;
        string message;

        if (player1IsAttacker)
        {
            player1Score++;
            attackerWonRound = true;
            message = "ATTACKER (PLAYER 1) WINS � BOMB EXPLODED!";
        }
        else
        {
            player2Score++;
            attackerWonRound = false;
            message = "ATTACKER (PLAYER 2) WINS � BOMB EXPLODED!";
        }

        Debug.Log("[RoundManager] " + message);
        UpdateScoreUI();
        ShowBannerForBoth(message);

        activeBomb = null;
        SetBombTimerVisible(false);

        HandleRoundFinished(attackerWonRound);
    }

    public void OnBombDefused()
    {
        if (roundOver || matchOver) return;
        roundOver = true;

        bool p1Defender = !player1IsAttacker;
        bool p1WonRound = p1Defender;

        string message = p1Defender
            ? "DEFENDER (PLAYER 1) WINS � BOMB DEFUSED!"
            : "DEFENDER (PLAYER 2) WINS � BOMB DEFUSED!";

        if (p1WonRound) player1Score++; else player2Score++;

        Debug.Log("[RoundManager] " + message);
        UpdateScoreUI();
        ShowBannerForBoth(message);

        activeBomb = null;
        SetBombTimerVisible(false);

        HandleRoundFinished(p1WonRound);
    }

    public void UpdateBombTimerUI(float remainingTime)
    {
        remainingTime = Mathf.Max(remainingTime, 0f);
        int seconds = Mathf.CeilToInt(remainingTime);
        string text = seconds.ToString();

        if (bombTimerTextP1 != null)
        {
            bombTimerTextP1.gameObject.SetActive(true);
            bombTimerTextP1.text = text;
        }

        if (bombTimerTextP2 != null)
        {
            bombTimerTextP2.gameObject.SetActive(true);
            bombTimerTextP2.text = text;
        }
    }

    public void ClearBombTimer()
    {
        SetBombTimerVisible(false);
    }

    // Planting / defusing interface used by players  ----------------------

    public bool TryPlantBomb(SplitScreenFPSController planter)
    {
        if (matchOver || roundOver) return false;
        if (bombPrefab == null || bombSites == null || bombSites.Length == 0) return false;

        // Only attacker may plant
        bool planterIsP1 = (planter != null && planter.GetComponent<PlayerHealth>() == player1);
        bool isAttacker =
            (player1IsAttacker && planterIsP1) ||
            (!player1IsAttacker && !planterIsP1);

        if (!isAttacker)
            return false;

        // Do not plant if bomb already active
        if (activeBomb != null && activeBomb.IsActive)
            return false;

        // Find closest site within radius
        Transform closestSite = null;
        float bestDist = Mathf.Infinity;
        Vector3 pos = planter.transform.position;

        foreach (Transform site in bombSites)
        {
            if (site == null) continue;
            float d = Vector3.Distance(pos, site.position);
            if (d < bestDist && d <= bombPlantRadius)
            {
                bestDist = d;
                closestSite = site;
            }
        }

        if (closestSite == null)
            return false;

        // Spawn bomb at site
        GameObject bombGO = Instantiate(bombPrefab, closestSite.position, closestSite.rotation);
        activeBomb = bombGO.GetComponent<BombController>();

        if (activeBomb != null)
        {
            activeBomb.Initialize(this, bombDuration, bombDefuseTime);
        }

        Debug.Log("[RoundManager] Bomb planted at site " + closestSite.name);

        ShowBannerForBoth("BOMB PLANTED!");
        SetBombTimerVisible(true);

        return true;
    }

    public void TryStartDefuse(SplitScreenFPSController player)
    {
        if (activeBomb == null) return;
        activeBomb.StartDefuse(player);
    }

    public void StopDefuse(SplitScreenFPSController player)
    {
        if (activeBomb == null) return;
        activeBomb.StopDefuse(player);
    }

    // ---------------------------------------------------------------------
    // INTERNAL HELPERS
    // ---------------------------------------------------------------------

    private void HandleRoundFinished(bool p1WonRound)
    {
        // Lock inputs immediately when a round ends.
        SetPlayersInputEnabled(false);

        // Check match-end conditions
        bool someoneReachedScore = (player1Score >= roundsToWin || player2Score >= roundsToWin);
        bool reachedMaxRounds = (currentRound >= maxRounds);

        if (someoneReachedScore || reachedMaxRounds)
        {
            matchOver = true;

            string matchWinner;
            if (player1Score > player2Score) matchWinner = "PLAYER 1";
            else if (player2Score > player1Score) matchWinner = "PLAYER 2";
            else matchWinner = "DRAW";

            string msg = (matchWinner == "DRAW")
                ? "MATCH DRAW!"
                : matchWinner + " WINS THE MATCH!";

            ShowBannerForBoth(msg + "\nPress ENTER to play again.");

            Debug.Log("[RoundManager] " + msg + " Waiting for restart key.");

            // We do NOT start another round here. Update() will listen for Enter.
            return;
        }

        // Not match over � schedule next round.
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

    private void ShowBannerForBoth(string message)
    {
        if (bannerHideRoutine != null)
            StopCoroutine(bannerHideRoutine);

        if (bannerRootP1 != null) bannerRootP1.SetActive(true);
        if (bannerRootP2 != null) bannerRootP2.SetActive(true);

        if (bannerTextP1 != null) bannerTextP1.text = message;
        if (bannerTextP2 != null) bannerTextP2.text = message;

        bannerHideRoutine = StartCoroutine(BannerAutoHideRoutine());
    }

    private IEnumerator BannerAutoHideRoutine()
    {
        yield return new WaitForSeconds(bannerDisplayTime);

        if (!matchOver)
        {
            if (bannerRootP1 != null) bannerRootP1.SetActive(false);
            if (bannerRootP2 != null) bannerRootP2.SetActive(false);
        }
    }

    private void HideBannerImmediate()
    {
        if (bannerHideRoutine != null)
        {
            StopCoroutine(bannerHideRoutine);
            bannerHideRoutine = null;
        }

        if (bannerRootP1 != null) bannerRootP1.SetActive(false);
        if (bannerRootP2 != null) bannerRootP2.SetActive(false);
    }

    private void SetBombTimerVisible(bool visible)
    {
        if (bombTimerTextP1 != null)
            bombTimerTextP1.gameObject.SetActive(visible);

        if (bombTimerTextP2 != null)
            bombTimerTextP2.gameObject.SetActive(visible);
    }

    private void SetPlayersInputEnabled(bool enabled)
{
    if (player1 != null)
    {
        // Multiplayer controller
        var mp1 = player1.GetComponent<SplitScreenFPSController>();
        if (mp1 != null) mp1.SetInputEnabled(enabled);

        // Singleplayer controller
        var sp1 = player1.GetComponent<SinglePlayerFPSController>();
        if (sp1 != null) sp1.SetInputEnabled(enabled);
    }

    if (player2 != null)
    {
        // Multiplayer controller
        var mp2 = player2.GetComponent<SplitScreenFPSController>();
        if (mp2 != null) mp2.SetInputEnabled(enabled);

        // Singleplayer controller (AI uses this!)
        var sp2 = player2.GetComponent<SinglePlayerFPSController>();
        if (sp2 != null) sp2.SetInputEnabled(enabled);
    }
}


    // ---------------------------------------------------------------------
    // UPDATE � only used for restart key when match over
    // ---------------------------------------------------------------------

    private void Update()
    {
        if (!matchOver) return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("[RoundManager] Restarting match via Enter key.");
            StartMatch();
        }
    }
}
