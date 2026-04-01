using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// “Ù¿÷“Ù¡øª¨øÈ - π“‘ÿµΩSlider…œ
/// </summary>
public class MusicVolumeSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Text valueText;

    private void Awake()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.onValueChanged.AddListener(OnValueChanged);
        }
    }

    private void OnEnable()
    {
        SettingsEvents.OnMusicVolumeChanged += OnVolumeChanged;

        if (SettingsManager.Instance != null)
        {
            slider.SetValueWithoutNotify(SettingsManager.Instance.Data.musicVolume);
            UpdateText(SettingsManager.Instance.Data.musicVolume);
        }
    }

    private void OnDisable()
    {
        SettingsEvents.OnMusicVolumeChanged -= OnVolumeChanged;
    }

    private void OnValueChanged(float value)
    {
        SettingsManager.Instance?.SetMusicVolume(value);
        UpdateText(value);
    }

    private void OnVolumeChanged(float value)
    {
        if (slider != null && !Mathf.Approximately(slider.value, value))
        {
            slider.SetValueWithoutNotify(value);
            UpdateText(value);
        }
    }

    private void UpdateText(float value)
    {
        if (valueText != null)
        {
            valueText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }
}