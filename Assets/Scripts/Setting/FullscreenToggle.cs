using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全屏开关 - 挂载到Toggle上
/// </summary>
public class FullscreenToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    private void Awake()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();

        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnValueChanged);
        }
    }

    private void OnEnable()
    {
        SettingsEvents.OnFullscreenChanged += OnFullscreenChanged;

        if (SettingsManager.Instance != null)
        {
            toggle.SetIsOnWithoutNotify(SettingsManager.Instance.Data.fullscreen);
        }
    }

    private void OnDisable()
    {
        SettingsEvents.OnFullscreenChanged -= OnFullscreenChanged;
    }

    private void OnValueChanged(bool value)
    {
        SettingsManager.Instance?.SetFullscreen(value);
    }

    private void OnFullscreenChanged(bool fullscreen)
    {
        if (toggle != null && toggle.isOn != fullscreen)
        {
            toggle.SetIsOnWithoutNotify(fullscreen);
        }
    }
}