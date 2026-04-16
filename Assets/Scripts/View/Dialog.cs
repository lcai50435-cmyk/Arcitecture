using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 简单的首次拾取提示框
/// </summary>
public class Dialog: MonoBehaviour
{
    [Header("UI组件")]
    public GameObject dialogPanel;           // 对话框面板
    public TextMeshProUGUI descriptionText;  // 描述文本

    [Header("需要隐藏的其他UI")]
    public GameObject[] uiToHide;            // 对话框显示时隐藏的UI

    [Header("设置")]
    public float displayDuration = 4f;       // 显示时间

    private BackpackMananger backpackManager;

    void Start()
    {
        backpackManager = BackpackMananger.Instance;

        if (backpackManager != null)
        {
            backpackManager.OnFirstTimePickItemType += ShowDialog;
        }

        // 初始隐藏对话框
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (backpackManager != null)
        {
            backpackManager.OnFirstTimePickItemType -= ShowDialog;
        }
    }

    void ShowDialog(ArchitecturalCrystal crystal)
    {
        if (crystal == null) return;

        // 从 crystal 对象中获取描述文本
        string desc = string.IsNullOrEmpty(crystal.textDescription)
            ? $"获得 {crystal.type}！\n构建度 +{crystal.expValue}"
            : crystal.textDescription;

        descriptionText.text = desc;

        // 隐藏其他UI
        HideOtherUI(true);

        // 显示对话框
        dialogPanel.SetActive(true);

        // 4秒后关闭
        StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);

        // 隐藏对话框
        dialogPanel.SetActive(false);

        // 恢复其他UI
        HideOtherUI(false);
    }

    void HideOtherUI(bool hide)
    {
        foreach (GameObject ui in uiToHide)
        {
            if (ui != null)
            {
                ui.SetActive(!hide);
            }
        }
    }
}