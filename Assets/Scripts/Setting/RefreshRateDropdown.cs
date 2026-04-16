using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 刷新率下拉框 - 挂载到Dropdown上
/// </summary>
public class RefreshRateDropdown : MonoBehaviour
{
    [SerializeField] private Dropdown dropdown;
    [SerializeField] private Text captionText;

    private List<string> refreshRateOptions = new List<string>();
    private List<int> refreshRateValues = new List<int>();

    private void Awake()
    {
        if (dropdown == null)
            dropdown = GetComponent<Dropdown>();

        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener(OnValueChanged);
        }
    }

    private void OnEnable()
    {
        SettingsEvents.OnResolutionIndexChanged += OnResolutionChanged;
        SettingsEvents.OnRefreshRateIndexChanged += OnRefreshRateChanged;
        SettingsEvents.OnRefreshRatesChanged += OnRefreshRatesChanged;

        // 初始加载
        if (SettingsManager.Instance != null)
        {
            int currentIndex = SettingsManager.Instance.Data.resolutionIndex;
            var rates = SettingsManager.Instance.GetRefreshRates(currentIndex);
            UpdateOptions(rates);
        }
    }

    private void OnDisable()
    {
        SettingsEvents.OnResolutionIndexChanged -= OnResolutionChanged;
        SettingsEvents.OnRefreshRateIndexChanged -= OnRefreshRateChanged;
        SettingsEvents.OnRefreshRatesChanged -= OnRefreshRatesChanged;
    }

    private void OnRefreshRatesChanged(int resolutionIndex, int defaultRate)
    {
        if (SettingsManager.Instance != null)
        {
            var rates = SettingsManager.Instance.GetRefreshRates(resolutionIndex);
            UpdateOptions(rates);
        }
    }

    private void OnResolutionChanged(int index)
    {
        if (SettingsManager.Instance != null)
        {
            var rates = SettingsManager.Instance.GetRefreshRates(index);
            UpdateOptions(rates);
        }
    }

    private void UpdateOptions(List<int> rates)
    {
        refreshRateValues.Clear();
        refreshRateOptions.Clear();

        foreach (var rate in rates)
        {
            refreshRateValues.Add(rate);
            refreshRateOptions.Add($"{rate} Hz");
        }

        if (dropdown != null)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(refreshRateOptions);

            // 恢复保存的索引
            if (SettingsManager.Instance != null)
            {
                int index = SettingsManager.Instance.Data.refreshRateIndex;
                if (index >= 0 && index < refreshRateOptions.Count)
                {
                    dropdown.SetValueWithoutNotify(index);
                    UpdateCaption(index);
                }
            }
        }
    }

    private void OnValueChanged(int index)
    {
        SettingsManager.Instance?.SetRefreshRateIndex(index);
        UpdateCaption(index);
    }

    private void OnRefreshRateChanged(int index)
    {
        if (dropdown != null && dropdown.value != index && index < refreshRateOptions.Count)
        {
            dropdown.SetValueWithoutNotify(index);
            UpdateCaption(index);
        }
    }

    private void UpdateCaption(int index)
    {
        if (captionText != null && index < refreshRateOptions.Count)
        {
            captionText.text = refreshRateOptions[index];
        }
    }
}