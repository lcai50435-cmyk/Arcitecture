using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Dialog : MonoBehaviour
{
    [Header("UI 组件")]
    public GameObject dialogPanel;
    public Text descriptionText;

    [Header("点击关闭按钮")]
    public Button clickCloseButton;

    [Header("需要隐藏的其他 UI")]
    public GameObject[] uiToHide;

    [Header("自动关闭时间")]
    public float displayDuration = 4f;

    [Header("是否允许普通弹窗显示")]
    public bool canShow = true;

    private BackpackMananger backpackManager;
    private Coroutine currentCoroutine;
    private bool waitingForClickClose;
    private bool pausedByFirstPickDialog;
    private float timeScaleBeforeDialog = 1f;

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
        if (crystal.isUnlockMaterial) return;

        string desc = BuildSpiritIntro(crystal);
        if (InternalShow(desc, false))
        {
            PauseGameForFirstPickDialog();
        }
    }

    public void ShowAutoDialog(string desc)
    {
        if (!canShow) return;
        InternalShow(desc, true);
    }

    public void ShowAutoDialogForce(string desc)
    {
        InternalShow(desc, true);
    }

    public void ShowClickCloseDialog(string desc)
    {
        InternalShow(desc, false);
    }

    private bool InternalShow(string desc, bool autoClose)
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Dialog 所在对象未激活，无法显示弹窗");
            return false;
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

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.ShowDialog();
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

        return true;
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

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.HideDialog();
        }

        HideOtherUI(false);
        ResumeGameAfterFirstPickDialog();
    }

    private void PauseGameForFirstPickDialog()
    {
        if (pausedByFirstPickDialog) return;

        timeScaleBeforeDialog = Time.timeScale;
        Time.timeScale = 0f;
        pausedByFirstPickDialog = true;
    }

    private void ResumeGameAfterFirstPickDialog()
    {
        if (!pausedByFirstPickDialog) return;

        Time.timeScale = timeScaleBeforeDialog <= 0f ? 1f : timeScaleBeforeDialog;
        pausedByFirstPickDialog = false;
    }

    private string BuildSpiritIntro(ArchitecturalCrystal crystal)
    {
        string desc = string.IsNullOrEmpty(crystal.textDescription)
            ? $"发现了 {crystal.type}。它会带来 {crystal.expValue} 点结构经验。"
            : crystal.textDescription;

        return $"精灵：\n{desc}\n\n点击按钮后继续探索。";
    }

    private void HideOtherUI(bool hide)
    {
        if (uiToHide == null) return;

        foreach (GameObject ui in uiToHide)
        {
            if (ui != null)
            {
                ui.SetActive(!hide);
            }
        }
    }
}
