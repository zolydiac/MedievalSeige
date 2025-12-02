using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject modeSelectPanel; // NEW

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    [Header("Mode Select Buttons")]
    [SerializeField] private Button singleplayerButton; // NEW
    [SerializeField] private Button multiplayerButton;  // NEW

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "scene7";
    [SerializeField] private string multiplayerSceneName = "MultiplayerScene"; // NEW

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
        modeSelectPanel?.SetActive(false); // NEW
    }

    public void OnPlayClicked()
    {
        // Show mode select instead of loading immediately
        mainPanel.SetActive(false);
        modeSelectPanel.SetActive(true);
    }

    public void OnSingleplayerClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnMultiplayerClicked()
    {
        SceneManager.LoadScene(multiplayerSceneName);
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnOptionsClicked()
    {
        mainPanel?.SetActive(false);
        optionsPanel?.SetActive(true);
    }

    public void OnBackFromOptions()
    {
        ShowMainPanel();
    }

    public void OnBackFromModeSelect()
    {
        modeSelectPanel?.SetActive(false);
        mainPanel?.SetActive(true);
    }
}
