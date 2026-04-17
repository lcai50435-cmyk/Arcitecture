using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 退出按钮 - 挂载到Button上
/// </summary>
public class ExitButton : MonoBehaviour
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
        SettingsManager.Instance?.ExitGame();
    }
}