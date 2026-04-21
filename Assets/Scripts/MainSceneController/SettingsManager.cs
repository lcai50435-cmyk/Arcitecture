using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsManager : MonoBehaviour
{
    [Header("设置面板")]
    public GameObject settingsPanel;
    public Button closeButton;

    [Header("音量控制")]
    public Slider volumeSlider;
    public Text volumeText;

    [Header("分辨率控制")]
    public Dropdown resolutionDropdown;

    private GameSettingsDraft currentDraft;

    private void Start()
    {
        InitializeVolume();
        InitializeResolution();
        ApplyRuntimeFonts();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseSettings);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void OpenSettings()
    {
        currentDraft = GameSettingsStore.CreateDraftFromSaved();
        RefreshPanelValues();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (currentDraft != null)
        {
            GameSettingsStore.ApplyDraft(currentDraft);
            currentDraft = GameSettingsStore.CreateDraftFromSaved();
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void InitializeVolume()
    {
        if (volumeSlider == null)
        {
            return;
        }

        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void InitializeResolution()
    {
        if (resolutionDropdown == null)
        {
            return;
        }

        if (ResolutionManager.Instance == null)
        {
            Debug.LogError("ResolutionManager 不存在。");
            return;
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(new System.Collections.Generic.List<string>(ResolutionManager.Instance.GetResolutionOptions()));
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        RefreshPanelValues();
    }

    private void RefreshPanelValues()
    {
        GameSettingsDraft draft = currentDraft ?? GameSettingsStore.CreateDraftFromSaved();

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(draft.masterVolume);
            UpdateVolumeDisplay(draft.masterVolume);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.SetValueWithoutNotify(Mathf.Clamp(draft.resolutionIndex, 0, GameSettingsStore.ResolutionOptionCount - 1));
            resolutionDropdown.RefreshShownValue();
        }
    }

    private void OnVolumeChanged(float value)
    {
        if (currentDraft == null)
        {
            currentDraft = GameSettingsStore.CreateDraftFromSaved();
        }

        currentDraft.masterVolume = Mathf.Clamp01(value);
        GameSettingsStore.PreviewDraftAudio(currentDraft);
        UpdateVolumeDisplay(currentDraft.masterVolume);
    }

    private void UpdateVolumeDisplay(float value)
    {
        if (volumeText != null)
        {
            volumeText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }

    public void OnResolutionChanged(int index)
    {
        if (currentDraft == null)
        {
            currentDraft = GameSettingsStore.CreateDraftFromSaved();
        }

        currentDraft.resolutionIndex = Mathf.Clamp(index, 0, GameSettingsStore.ResolutionOptionCount - 1);
    }

    private void ApplyRuntimeFonts()
    {
        RuntimeTextFontRepair.RepairLegacyText(volumeText);

        if (resolutionDropdown == null)
        {
            return;
        }

        RuntimeTextFontRepair.RepairLegacyText(resolutionDropdown.captionText);
        RuntimeTextFontRepair.RepairLegacyText(resolutionDropdown.itemText);
    }
}
