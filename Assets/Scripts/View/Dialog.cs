using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Dialog : MonoBehaviour
{
    private const string GameplayPauseReason = "RuntimeDialog";

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
    private bool isClosingDialog;
    private bool requestedGameplayPause;

    private void Start()
    {
        backpackManager = BackpackMananger.Instance;
        RuntimeTextFontRepair.RepairLegacyText(descriptionText);

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
        InternalShow(desc, false);
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
        isClosingDialog = false;
        PauseGameForFirstPickDialog();

        if (UIRootManager.Instance != null)
        {
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(true);
            }

            UIRootManager.Instance.OpenModal(RuntimeModalType.Dialog, RuntimeModalOpenSource.None);
        }
        else if (dialogPanel != null)
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

        return true;
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        CloseDialog();
    }

    private void OnClickCloseDialog()
    {
        if (!waitingForClickClose) return;
        CloseDialog();
    }

    public void CloseDialog()
    {
        if (isClosingDialog)
        {
            return;
        }

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        waitingForClickClose = false;
        isClosingDialog = true;

        if (UIRootManager.Instance != null && UIRootManager.Instance.ActiveModalType == RuntimeModalType.Dialog)
        {
            UIRootManager.Instance.CloseModalFlow(CompleteCloseDialog);
            return;
        }

        CompleteCloseDialog();
    }

    public void ForceHideImmediately()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        waitingForClickClose = false;
        isClosingDialog = false;

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

    private void CompleteCloseDialog()
    {
        if (clickCloseButton != null)
        {
            clickCloseButton.gameObject.SetActive(false);
        }

        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }

        HideOtherUI(false);
        ResumeGameAfterFirstPickDialog();
        isClosingDialog = false;
    }

    private void PauseGameForFirstPickDialog()
    {
        if (requestedGameplayPause || !GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        RuntimeGameplayPauseController.RequestPause(GameplayPauseReason);
        requestedGameplayPause = true;
    }

    private void ResumeGameAfterFirstPickDialog()
    {
        if (!requestedGameplayPause)
        {
            return;
        }

        RuntimeGameplayPauseController.ReleasePause(GameplayPauseReason);
        requestedGameplayPause = false;
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
