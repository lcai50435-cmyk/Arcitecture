using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("面板引用")]
    public GameObject settingsPanel;
    public Button closeButton;

    [Header("音量控制")]
    public Slider volumeSlider;
    public UnityEngine.UI.Text volumeText;

    [Header("分辨率控制")]
    public TMP_Dropdown resolutionDropdown;

    void Start()
    {
        InitializeVolume();
        InitializeResolution();

        closeButton.onClick.AddListener(CloseSettings);
        settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        LoadCurrentSettings();
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        SaveSettings();
    }

    void InitializeVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
        volumeSlider.value = savedVolume;
        UpdateVolumeDisplay(savedVolume);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetVolume(value);
        UpdateVolumeDisplay(value);
    }

    void UpdateVolumeDisplay(float value)
    {
        volumeText.text = Mathf.RoundToInt(value * 100) + "%";
    }

    void InitializeResolution()
    {
        if (ResolutionManager.Instance == null)
        {
            Debug.LogError("ResolutionManager 不存在！");
            return;
        }

        string[] options = ResolutionManager.Instance.GetResolutionOptions();

        resolutionDropdown.ClearOptions();
        var dropdownOptions = new System.Collections.Generic.List<string>();
        foreach (string opt in options)
        {
            dropdownOptions.Add(opt);
        }

        resolutionDropdown.AddOptions(dropdownOptions);

        int currentIndex = ResolutionManager.Instance.GetCurrentResolutionIndex();
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    void OnResolutionChanged(int index)
    {
        if (ResolutionManager.Instance != null)
        {
            ResolutionManager.Instance.SetResolution(index);
        }
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat("GameVolume", volumeSlider.value);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);
        PlayerPrefs.Save();
    }

    void LoadCurrentSettings()
    {
        float vol = PlayerPrefs.GetFloat("GameVolume", 1f);
        volumeSlider.value = vol;
        AudioListener.volume = vol;
    }
}