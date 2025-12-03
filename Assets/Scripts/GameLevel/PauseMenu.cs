using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("UI Root")]
    [SerializeField] private GameObject pauseRoot;   // Whole pause menu canvas/root

    [Header("Panels")]
    [SerializeField] private GameObject mainPausePanel;  // Panel with Resume/Main Menu/Quit/Controls
    [SerializeField] private GameObject controlsPanel;   // Panel that shows controls text

    private RoundManager roundManager;
    private bool isPaused = false;
    public bool IsPaused => isPaused;

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

        // Start hidden & unpaused
        SetPaused(false, applyTimeScale: true);
    }

    // Called from input (ESC / Start button)
    public void TogglePause()
    {
        SetPaused(!isPaused, applyTimeScale: true);
    }

    // ----- MAIN BUTTONS -----

    public void OnResumeClicked()
    {
        SetPaused(false, applyTimeScale: true);
    }

    public void OnMainMenuClicked()
    {
        // Unpause so timeScale goes back to 1
        SetPaused(false, applyTimeScale: true);
        SceneManager.LoadScene("MainMenu");
    }

    public void OnQuitClicked()
    {
        // Unpause just in case
        SetPaused(false, applyTimeScale: true);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ----- CONTROLS PANEL BUTTONS -----

    public void OnControlsClicked()
    {
        // Show controls panel, hide main pause buttons
        if (mainPausePanel != null) mainPausePanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);
    }

    public void OnBackFromControls()
    {
        // Go back to main pause panel
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (mainPausePanel != null) mainPausePanel.SetActive(true);
    }

    // ----- CORE PAUSE LOGIC -----

    private void SetPaused(bool paused, bool applyTimeScale)
    {
        isPaused = paused;

        if (pauseRoot != null)
            pauseRoot.SetActive(paused);

        if (applyTimeScale)
            Time.timeScale = paused ? 0f : 1f;

        if (roundManager != null)
            roundManager.SetGamePaused(paused);

        // When we first pause, always start on the main pause panel
        if (paused)
        {
            if (mainPausePanel != null) mainPausePanel.SetActive(true);
            if (controlsPanel != null) controlsPanel.SetActive(false);

            // IMPORTANT: free the mouse for UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Back to gameplay: lock mouse again (P1 uses mouse)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
