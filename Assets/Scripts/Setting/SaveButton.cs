using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 保存按钮 - 挂载到Button上
/// </summary>
public class SaveButton : MonoBehaviour
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
        SettingsManager.Instance?.SaveSettings();
        SettingsEvents.OnShowMessage?.Invoke("设置已保存！");
    }
}