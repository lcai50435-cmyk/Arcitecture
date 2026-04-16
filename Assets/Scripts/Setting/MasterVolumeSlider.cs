using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主音量滑块 - 挂载到Slider上
/// </summary>
public class MasterVolumeSlider : MonoBehaviour
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
        // 订阅事件，同步显示
        SettingsEvents.OnMasterVolumeChanged += OnVolumeChanged;

        // 获取当前值
        if (SettingsManager.Instance != null)
        {
            slider.SetValueWithoutNotify(SettingsManager.Instance.Data.masterVolume);
            UpdateText(SettingsManager.Instance.Data.masterVolume);
        }
    }

    private void OnDisable()
    {
        SettingsEvents.OnMasterVolumeChanged -= OnVolumeChanged;
    }

    private void OnValueChanged(float value)
    {
        SettingsManager.Instance?.SetMasterVolume(value);
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