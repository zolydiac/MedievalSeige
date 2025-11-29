using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    [Header("Default Values")]
    [SerializeField] private float defaultMasterVolume = 1f;
    [SerializeField] private float defaultMouseSensitivity = 2f;

    public float MasterVolume { get; private set; }
    public float MouseSensitivity { get; private set; }

    private const string VolumeKey = "MasterVolume";
    private const string MouseSensKey = "MouseSensitivity";

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Debug.Log("[GameSettings] Duplicate detected, destroying this one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved values or defaults
        MasterVolume = PlayerPrefs.GetFloat(VolumeKey, defaultMasterVolume);
        MouseSensitivity = PlayerPrefs.GetFloat(MouseSensKey, defaultMouseSensitivity);

        Debug.Log($"[GameSettings] Awake. Loaded volume={MasterVolume}, sens={MouseSensitivity}");

        ApplyVolume();
    }

    private void ApplyVolume()
    {
        AudioListener.volume = MasterVolume;
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        ApplyVolume();
        PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
        Debug.Log($"[GameSettings] SetMasterVolume -> {MasterVolume}");
    }

    public void SetMouseSensitivity(float value)
    {
        // Allow a reasonable range for sensitivity
        MouseSensitivity = Mathf.Clamp(value, 0.2f, 10f);
        PlayerPrefs.SetFloat(MouseSensKey, MouseSensitivity);
        Debug.Log($"[GameSettings] SetMouseSensitivity -> {MouseSensitivity}");
    }
}
