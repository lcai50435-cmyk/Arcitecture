using System.Collections;
<<<<<<< HEAD
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
=======
using UnityEngine;
using UnityEngine.UI;

public class Dialog : MonoBehaviour
{
    [Header("UI组件")]
    public GameObject dialogPanel;
    public Text descriptionText;

    [Header("点击任意处关闭用按钮（覆盖整个弹窗区域或全屏）")]
    public Button clickCloseButton;

    [Header("需要隐藏的其他UI")]
    public GameObject[] uiToHide;

    [Header("自动关闭时间")]
    public float displayDuration = 4f;

    [Header("是否允许普通弹窗显示")]
    public bool canShow = true;

    private BackpackMananger backpackManager;
    private Coroutine currentCoroutine;
    private bool waitingForClickClose = false;

    private void Start()
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
    {
        backpackManager = BackpackMananger.Instance;

        if (backpackManager != null)
        {
<<<<<<< HEAD
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
=======
            backpackManager.OnFirstTimePickItemType += ShowDialogByCrystal;
        }

        if (clickCloseButton != null)
        {
            clickCloseButton.onClick.AddListener(OnClickCloseDialog);
        }

        ForceHideImmediately();
    }

    private void OnDestroy()
    {
        if (backpackManager != null)
        {
            backpackManager.OnFirstTimePickItemType -= ShowDialogByCrystal;
        }

        if (clickCloseButton != null)
        {
            clickCloseButton.onClick.RemoveListener(OnClickCloseDialog);
        }
    }

    private void ShowDialogByCrystal(ArchitecturalCrystal crystal)
    {
        if (crystal == null) return;

>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
        string desc = string.IsNullOrEmpty(crystal.textDescription)
            ? $"获得 {crystal.type}！\n构建度 +{crystal.expValue}"
            : crystal.textDescription;

<<<<<<< HEAD
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
=======
        // 拾取物品：自动关闭
        ShowAutoDialog(desc);
    }

    /// <summary>
    /// 普通自动关闭弹窗（受 canShow 限制）
    /// </summary>
    public void ShowAutoDialog(string desc)
    {
        if (!canShow) return;
        InternalShow(desc, true);
    }

    /// <summary>
    /// 强制自动关闭弹窗（不受 canShow 限制）
    /// </summary>
    public void ShowAutoDialogForce(string desc)
    {
        InternalShow(desc, true);
    }

    /// <summary>
    /// 点击任意处关闭弹窗（不受 canShow 限制）
    /// 用于小图标介绍
    /// </summary>
    public void ShowClickCloseDialog(string desc)
    {
        InternalShow(desc, false);
    }

    private void InternalShow(string desc, bool autoClose)
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Dialog脚本所在物体是 inactive，无法显示弹窗");
            return;
        }

        if (descriptionText != null)
        {
            descriptionText.text = desc;
        }

        HideOtherUI(true);

        if (dialogPanel != null)
        {
            dialogPanel.SetActive(true);
        }

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        waitingForClickClose = !autoClose;

        if (clickCloseButton != null)
        {
            clickCloseButton.gameObject.SetActive(waitingForClickClose);
        }

        if (autoClose)
        {
            currentCoroutine = StartCoroutine(HideAfterDelay());
        }
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        ForceHideImmediately();
    }

    private void OnClickCloseDialog()
    {
        if (!waitingForClickClose) return;
        ForceHideImmediately();
    }

    public void ForceHideImmediately()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        waitingForClickClose = false;

        if (clickCloseButton != null)
        {
            clickCloseButton.gameObject.SetActive(false);
        }

        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }

        HideOtherUI(false);
    }

    private void HideOtherUI(bool hide)
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
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