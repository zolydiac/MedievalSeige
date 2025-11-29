using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider mouseSensitivitySlider;

    private void Awake()
    {
        // Optional auto-find if you didn’t wire them yet
        if (volumeSlider == null)
            volumeSlider = transform.Find("VolumeSlider")?.GetComponent<Slider>();

        if (mouseSensitivitySlider == null)
            mouseSensitivitySlider = transform.Find("MouseSensitivitySlider")?.GetComponent<Slider>();
    }

    private void OnEnable()
    {
        // Whenever the panel opens, sync sliders from GameSettings
        if (GameSettings.Instance == null) return;

        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(GameSettings.Instance.MasterVolume);

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.SetValueWithoutNotify(GameSettings.Instance.MouseSensitivity);
    }

    // Called by the volume slider's OnValueChanged(float)
    public void OnVolumeChanged(float value)
    {
        if (GameSettings.Instance == null) return;

        GameSettings.Instance.SetMasterVolume(value);
    }

    // Called by the sensitivity slider's OnValueChanged(float)
    public void OnMouseSensitivityChanged(float value)
    {
        Debug.Log($"[SettingsUI] Slider raw value = {value}");

        if (GameSettings.Instance == null) return;

        GameSettings.Instance.SetMouseSensitivity(value);

        var controllers = FindObjectsOfType<SplitScreenFPSController>();
        foreach (var c in controllers)
            c.SetMouseSensitivity(value);
    }

}
