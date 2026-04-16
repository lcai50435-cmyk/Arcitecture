using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 应用按钮 - 挂载到Button上
/// </summary>
public class ApplyButton : MonoBehaviour
{
    [SerializeField] private Button button;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        SettingsManager.Instance?.ApplyResolution();
        SettingsEvents.OnShowMessage?.Invoke("画面设置已应用");
    }
}