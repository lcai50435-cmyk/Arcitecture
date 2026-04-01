using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 静音开关 - 挂载到Toggle上
/// </summary>
public class MuteToggle : MonoBehaviour
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
        SettingsEvents.OnMuteChanged += OnMuteChanged;

        if (SettingsManager.Instance != null)
        {
            toggle.SetIsOnWithoutNotify(SettingsManager.Instance.Data.isMuted);
        }
    }

    private void OnDisable()
    {
        SettingsEvents.OnMuteChanged -= OnMuteChanged;
    }

    private void OnValueChanged(bool value)
    {
        SettingsManager.Instance?.SetMute(value);
    }

    private void OnMuteChanged(bool muted)
    {
        if (toggle != null && toggle.isOn != muted)
        {
            toggle.SetIsOnWithoutNotify(muted);
        }
    }
}