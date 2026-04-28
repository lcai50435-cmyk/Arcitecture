using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsManager : MonoBehaviour
{
    private const float SettingsPanelExpandedHeight = 430f;
    private const float TitleTopOffset = 145f;
    private const float CloseButtonTopOffset = 183f;
    private const float MasterRowOffsetY = 58f;
    private const float MusicRowOffsetY = 4f;
    private const float SfxRowOffsetY = -50f;
    private const float ResolutionRowOffsetY = -118f;

    [Header("设置面板")]
    public GameObject settingsPanel;
    public Button closeButton;

    [Header("音量控制")]
    public Slider volumeSlider;
    public Text volumeText;

    [Header("分辨率控制")]
    public Dropdown resolutionDropdown;

    private Slider musicVolumeSlider;
    private Text musicVolumeText;
    private Slider sfxVolumeSlider;
    private Text sfxVolumeText;
    private bool audioControlsReady;
    private GameSettingsDraft currentDraft;

    private void Start()
    {
        EnsureAudioControls();
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

        volumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }
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
            UpdateVolumeDisplay(volumeText, draft.masterVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(draft.musicVolume);
            UpdateVolumeDisplay(musicVolumeText, draft.musicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(draft.sfxVolume);
            UpdateVolumeDisplay(sfxVolumeText, draft.sfxVolume);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.SetValueWithoutNotify(Mathf.Clamp(draft.resolutionIndex, 0, GameSettingsStore.ResolutionOptionCount - 1));
            resolutionDropdown.RefreshShownValue();
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (currentDraft == null)
        {
            currentDraft = GameSettingsStore.CreateDraftFromSaved();
        }

        currentDraft.masterVolume = Mathf.Clamp01(value);
        GameSettingsStore.PreviewDraftAudio(currentDraft);
        UpdateVolumeDisplay(volumeText, currentDraft.masterVolume);
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (currentDraft == null)
        {
            currentDraft = GameSettingsStore.CreateDraftFromSaved();
        }

        currentDraft.musicVolume = Mathf.Clamp01(value);
        GameSettingsStore.PreviewDraftAudio(currentDraft);
        UpdateVolumeDisplay(musicVolumeText, currentDraft.musicVolume);
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (currentDraft == null)
        {
            currentDraft = GameSettingsStore.CreateDraftFromSaved();
        }

        currentDraft.sfxVolume = Mathf.Clamp01(value);
        GameSettingsStore.PreviewDraftAudio(currentDraft);
        UpdateVolumeDisplay(sfxVolumeText, currentDraft.sfxVolume);
    }

    private void OnVolumeChanged(float value)
    {
        OnMasterVolumeChanged(value);
    }

    private void UpdateVolumeDisplay(Text targetText, float value)
    {
        if (targetText != null)
        {
            targetText.text = $"{Mathf.RoundToInt(value * 100f)}%";
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
        EnsureAudioControls();
        RuntimeTextFontRepair.RepairLegacyText(volumeText);
        RuntimeTextFontRepair.RepairLegacyText(musicVolumeText);
        RuntimeTextFontRepair.RepairLegacyText(sfxVolumeText);

        if (resolutionDropdown == null)
        {
            return;
        }

        RuntimeTextFontRepair.RepairLegacyText(resolutionDropdown.captionText);
        RuntimeTextFontRepair.RepairLegacyText(resolutionDropdown.itemText);
    }

    private void EnsureAudioControls()
    {
        if (audioControlsReady || settingsPanel == null || volumeSlider == null || volumeText == null)
        {
            return;
        }

        RectTransform panelRect = settingsPanel.transform as RectTransform;
        RectTransform legacyVolumeRow = volumeSlider.transform.parent as RectTransform;
        RectTransform resolutionRow = resolutionDropdown != null ? resolutionDropdown.transform.parent as RectTransform : null;
        RectTransform titleRect = settingsPanel.transform.Find("Title") as RectTransform;
        RectTransform closeRect = closeButton != null ? closeButton.transform as RectTransform : null;

        if (panelRect != null)
        {
            panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, SettingsPanelExpandedHeight);
        }

        if (titleRect != null)
        {
            titleRect.anchoredPosition = new Vector2(titleRect.anchoredPosition.x, TitleTopOffset);
        }

        if (closeRect != null)
        {
            closeRect.anchoredPosition = new Vector2(closeRect.anchoredPosition.x, CloseButtonTopOffset);
        }

        if (legacyVolumeRow != null)
        {
            legacyVolumeRow.gameObject.SetActive(false);
        }

        if (resolutionRow != null)
        {
            resolutionRow.anchoredPosition = new Vector2(resolutionRow.anchoredPosition.x, ResolutionRowOffsetY);
        }

        Slider sliderTemplate = volumeSlider;
        Text valueTemplate = volumeText;

        CreateAudioRow("MasterVolumeRow", "总音量", MasterRowOffsetY, sliderTemplate, valueTemplate, out volumeSlider, out volumeText);
        CreateAudioRow("MusicVolumeRow", "音乐音量", MusicRowOffsetY, sliderTemplate, valueTemplate, out musicVolumeSlider, out musicVolumeText);
        CreateAudioRow("SfxVolumeRow", "音效音量", SfxRowOffsetY, sliderTemplate, valueTemplate, out sfxVolumeSlider, out sfxVolumeText);
        audioControlsReady = true;
    }

    private void CreateAudioRow(
        string rowName,
        string label,
        float anchoredY,
        Slider sliderTemplate,
        Text valueTemplate,
        out Slider createdSlider,
        out Text createdValueText)
    {
        GameObject rowObject = new GameObject(rowName, typeof(RectTransform));
        rowObject.transform.SetParent(settingsPanel.transform, false);
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(420f, 36f);
        rowRect.anchoredPosition = new Vector2(0f, anchoredY);

        Text labelText = Instantiate(valueTemplate, rowRect);
        labelText.name = $"{rowName}_Label";
        labelText.text = label;
        labelText.alignment = TextAnchor.MiddleLeft;
        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);
        labelRect.sizeDelta = new Vector2(110f, 30f);

        createdSlider = Instantiate(sliderTemplate, rowRect);
        createdSlider.name = $"{rowName}_Slider";
        RectTransform sliderRect = createdSlider.transform as RectTransform;
        if (sliderRect != null)
        {
            sliderRect.anchorMin = new Vector2(0f, 0.5f);
            sliderRect.anchorMax = new Vector2(0f, 0.5f);
            sliderRect.pivot = new Vector2(0f, 0.5f);
            sliderRect.anchoredPosition = new Vector2(128f, 0f);
            sliderRect.sizeDelta = new Vector2(210f, 20f);
        }

        createdValueText = Instantiate(valueTemplate, rowRect);
        createdValueText.name = $"{rowName}_Value";
        createdValueText.text = "100%";
        createdValueText.alignment = TextAnchor.MiddleRight;
        RectTransform valueRect = createdValueText.rectTransform;
        valueRect.anchorMin = new Vector2(1f, 0.5f);
        valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.anchoredPosition = new Vector2(0f, 0f);
        valueRect.sizeDelta = new Vector2(72f, 30f);

        RuntimeTextFontRepair.RepairLegacyText(labelText);
        RuntimeTextFontRepair.RepairLegacyText(createdValueText);
    }
}
