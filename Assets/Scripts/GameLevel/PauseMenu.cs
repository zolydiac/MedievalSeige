using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("UI Root")]
    [SerializeField] private GameObject pauseRoot;   // Assign your PauseMenuCanvas here

    private RoundManager roundManager;
    private bool isPaused = false;
    public bool IsPaused => isPaused;                // <-- used by the player controller

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
        roundManager = RoundManager.Instance;
        SetPaused(false, applyTimeScale: true);
    }

    // Called from input (ESC / Start)
    public void TogglePause()
    {
        SetPaused(!isPaused, applyTimeScale: true);
    }

    // UI button: Resume
    public void OnResumeClicked()
    {
        SetPaused(false, applyTimeScale: true);
    }

    // UI button: Main Menu
    public void OnMainMenuClicked()
    {
        // Make sure time is running again
        Time.timeScale = 1f;

        isPaused = false;
        if (pauseRoot != null)
            pauseRoot.SetActive(false);

        // Show mouse for main menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("MainMenu");
    }

    // UI button: Quit game entirely
    public void OnQuitClicked()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetPaused(bool paused, bool applyTimeScale)
    {
        isPaused = paused;

        if (pauseRoot != null)
            pauseRoot.SetActive(paused);

        if (applyTimeScale)
            Time.timeScale = paused ? 0f : 1f;

        if (roundManager != null)
            roundManager.SetGamePaused(paused);

        // Cursor behaviour while paused / unpaused
        if (paused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
