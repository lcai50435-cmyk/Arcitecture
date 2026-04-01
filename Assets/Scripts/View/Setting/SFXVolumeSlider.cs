using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 音效音量滑块 - 挂载到Slider上
/// </summary>
public class SFXVolumeSlider : MonoBehaviour
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
        SettingsEvents.OnSFXVolumeChanged += OnVolumeChanged;

        if (SettingsManager.Instance != null)
        {
            slider.SetValueWithoutNotify(SettingsManager.Instance.Data.sfxVolume);
            UpdateText(SettingsManager.Instance.Data.sfxVolume);
        }
    }

    private void OnDisable()
    {
        SettingsEvents.OnSFXVolumeChanged -= OnVolumeChanged;
    }

    private void OnValueChanged(float value)
    {
        SettingsManager.Instance?.SetSFXVolume(value);
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