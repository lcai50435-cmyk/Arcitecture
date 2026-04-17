using System.Collections;
using TMPro;
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

    private void ShowDialogByCrystal(ArchitecturalCrystal crystal)
    {
        if (crystal == null) return;

        // 从 crystal 对象中获取描述文本
        string desc = string.IsNullOrEmpty(crystal.textDescription)
            ? $"获得 {crystal.type}！\n构建度 +{crystal.expValue}"
            : crystal.textDescription;

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

        // 隐藏其他UI
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

        // 恢复其他UI
        HideOtherUI(false);
    }

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