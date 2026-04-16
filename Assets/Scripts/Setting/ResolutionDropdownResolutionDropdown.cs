using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 分辨率下拉框 - 挂载到Dropdown上
/// </summary>
public class ResolutionDropdown : MonoBehaviour
{
    [SerializeField] private Dropdown dropdown;
    [SerializeField] private Text captionText;

    private List<string> resolutionOptions = new List<string>();

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
        SettingsEvents.OnResolutionsLoaded += OnResolutionsLoaded;
        SettingsEvents.OnResolutionIndexChanged += OnResolutionChanged;

        // 如果有数据，立即加载
        if (SettingsManager.Instance != null && SettingsManager.Instance.AvailableResolutions != null)
        {
            OnResolutionsLoaded(SettingsManager.Instance.AvailableResolutions);
        }
    }

    private void OnDisable()
    {
        SettingsEvents.OnResolutionsLoaded -= OnResolutionsLoaded;
        SettingsEvents.OnResolutionIndexChanged -= OnResolutionChanged;
    }

    private void OnResolutionsLoaded(Resolution[] resolutions)
    {
        resolutionOptions.Clear();

        for (int i = 0; i < resolutions.Length; i++)
        {
            resolutionOptions.Add($"{resolutions[i].width} × {resolutions[i].height}");
        }

        if (dropdown != null)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(resolutionOptions);

            // 恢复保存的索引
            if (SettingsManager.Instance != null)
            {
                int index = SettingsManager.Instance.Data.resolutionIndex;
                if (index >= 0 && index < resolutionOptions.Count)
                {
                    dropdown.SetValueWithoutNotify(index);
                    UpdateCaption(index);
                }
            }
        }
    }

    private void OnValueChanged(int index)
    {
        SettingsManager.Instance?.SetResolutionIndex(index);
        UpdateCaption(index);
    }

    private void OnResolutionChanged(int index)
    {
        if (dropdown != null && dropdown.value != index)
        {
            dropdown.SetValueWithoutNotify(index);
            UpdateCaption(index);
        }
    }

    private void UpdateCaption(int index)
    {
        if (captionText != null && index < resolutionOptions.Count)
        {
            captionText.text = resolutionOptions[index];
        }
    }
}