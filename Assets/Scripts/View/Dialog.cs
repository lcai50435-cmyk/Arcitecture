using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Dialog : MonoBehaviour
{
    [Header("UI组件")]
    public GameObject dialogPanel;
    public Text descriptionText;

    [Header("点击任意处关闭用按钮")]
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
    {
        backpackManager = BackpackMananger.Instance;

        if (backpackManager != null)
        {
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

    /// <summary>
    /// 首次拾取物品时显示弹窗
    /// </summary>
    private void ShowDialogByCrystal(ArchitecturalCrystal crystal)
    {
        // 修复点：用唯一标识判断是否为“空”（示例：假设 type 是枚举，默认值为 0/None）
        if (crystal.type == default) return;

        string desc = string.IsNullOrEmpty(crystal.textDescription)
            ? $"获得 {crystal.type}！\n经验值 +{crystal.expValue}"
            : crystal.textDescription;

        ShowAutoDialog(desc);
    }


    /// <summary>
    /// 自动关闭弹窗（拾取提示用）
    /// </summary>
    public void ShowAutoDialog(string desc)
    {
        if (!canShow) return;
        InternalShow(desc, true);
    }

    /// <summary>
    /// 点击关闭弹窗（小图标介绍用）
    /// </summary>
    public void ShowClickCloseDialog(string desc)
    {
        InternalShow(desc, false);
    }

    /// <summary>
    /// 内部统一显示逻辑
    /// </summary>
    private void InternalShow(string desc, bool autoClose)
    {
        if (descriptionText != null)
        {
            descriptionText.text = desc;
        }

        // 隐藏其他UI
        HideOtherUI(true);

        // 打开Dialog面板
        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.ShowDialog();
        }

        // 清掉旧协程
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

        // 自动关闭模式
        if (autoClose)
        {
            currentCoroutine = StartCoroutine(HideAfterDelay());
        }
    }

    /// <summary>
    /// 自动等待后关闭
    /// </summary>
    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        ForceHideImmediately();
    }

    /// <summary>
    /// 点击关闭按钮
    /// </summary>
    private void OnClickCloseDialog()
    {
        if (!waitingForClickClose) return;
        ForceHideImmediately();
    }

    /// <summary>
    /// 强制立即关闭弹窗
    /// </summary>
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

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.HideDialog();
        }

        HideOtherUI(false);
    }

    /// <summary>
    /// 打开弹窗时隐藏其他UI，关闭弹窗时恢复
    /// </summary>
    private void HideOtherUI(bool hide)
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