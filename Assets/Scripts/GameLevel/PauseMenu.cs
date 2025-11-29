using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("UI Root")]
    [SerializeField] private GameObject pauseRoot;   // assign PauseMenuCanvas here

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private RoundManager roundManager;
    private bool isPaused = false;

    // <-- This is what SplitScreenFPSController is looking for
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

        // start hidden & unpaused
        SetPaused(false, applyTimeScale: true);
    }

    // Called from input (ESC / Start button)
    public void TogglePause()
    {
        SetPaused(!isPaused, applyTimeScale: true);
    }

    // Resume button
    public void OnResumeClicked()
    {
        SetPaused(false, applyTimeScale: true);
    }

    // "Main Menu" button
    public void OnMainMenuClicked()
    {
        // unpause and go back to menu
        SetPaused(false, applyTimeScale: true);

        // make mouse usable in the menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // "Quit Game" button (from pause menu)
    public void OnQuitGameClicked()
    {
        SetPaused(false, applyTimeScale: true);

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

        // handle cursor when pausing/unpausing
        if (paused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // when going back to gameplay, lock to centre again
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
