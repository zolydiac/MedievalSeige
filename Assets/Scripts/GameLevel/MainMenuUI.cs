using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;        // Play / Options / How To Play / Quit
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject modeSelectPanel;
    [SerializeField] private GameObject howToPlayPanel;   // NEW

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    [Header("Mode Select Buttons")]
    [SerializeField] private Button singleplayerButton;
    [SerializeField] private Button multiplayerButton;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "scene7";
    [SerializeField] private string multiplayerSceneName = "MultiplayerScene";

    private void Awake()
    {
        ShowMainPanel();

        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        if (singleplayerButton != null)
            singleplayerButton.onClick.AddListener(OnSingleplayerClicked);

        if (multiplayerButton != null)
            multiplayerButton.onClick.AddListener(OnMultiplayerClicked);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ShowMainPanel()
    {
        mainPanel?.SetActive(true);
        optionsPanel?.SetActive(false);
        modeSelectPanel?.SetActive(false);
        howToPlayPanel?.SetActive(false);   // NEW
    }

    // ---------- MAIN BUTTONS ----------

    public void OnPlayClicked()
    {
        // Show mode select instead of loading immediately
        if (mainPanel != null) mainPanel.SetActive(false);
        if (modeSelectPanel != null) modeSelectPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
    }

    public void OnOptionsClicked()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
        if (modeSelectPanel != null) modeSelectPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
    }

    public void OnHowToPlayClicked()   // NEW
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (modeSelectPanel != null) modeSelectPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- SUB-PANELS BACK BUTTONS ----------

    public void OnBackFromOptions()
    {
        ShowMainPanel();
    }

    public void OnBackFromModeSelect()
    {
        if (modeSelectPanel != null) modeSelectPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void OnBackFromHowToPlay()   // NEW
    {
        ShowMainPanel();
    }

    // ---------- MODE SELECTION ----------

    public void OnSingleplayerClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnMultiplayerClicked()
    {
        SceneManager.LoadScene(multiplayerSceneName);
    }
}
