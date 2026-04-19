using UnityEngine;

public class UIRootManager : MonoBehaviour
{
    public static UIRootManager Instance;

    [Header("图鉴主页")]
    public CanvasGroup handbookUI;

    [Header("详细信息页")]
    public CanvasGroup detailUIPage1;
    public CanvasGroup detailUIPage2;

    [Header("提交窗口 - 三个建筑分别一个")]
    public CanvasGroup submitSelectionUI1;
    public CanvasGroup submitSelectionUI2;
    public CanvasGroup submitSelectionUI3;

    [Header("Dialog弹窗")]
    public CanvasGroup dialogUI;

    [Header("场景交互提示UI")]
    public CanvasGroup interactTipUI;

    [Header("背包UI（可选）")]
    public CanvasGroup backpackUI;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        HideHandbook();
        HideAllDetail();
        HideAllSubmitSelection();
        HideDialog();

        ShowInteractTip();

        if (backpackUI != null)
        {
            ShowBackpack();
        }
    }

    private void SetUI(CanvasGroup cg, bool active, string name)
    {
        if (cg == null)
        {
            Debug.LogWarning($"{name} 没绑定 CanvasGroup");
            return;
        }

        cg.alpha = active ? 1f : 0f;
        cg.interactable = active;
        cg.blocksRaycasts = active;

        Debug.Log($"{name} -> active={active}, blocksRaycasts={cg.blocksRaycasts}");
    }

    // ========= 图鉴主页 =========
    public void ShowHandbook() => SetUI(handbookUI, true, "HandbookUI");
    public void HideHandbook() => SetUI(handbookUI, false, "HandbookUI");

    // ========= 详细页 =========
    public void ShowDetailPage1()
    {
        SetUI(detailUIPage1, true, "DetailUIPage1");
        SetUI(detailUIPage2, false, "DetailUIPage2");
    }

    public void ShowDetailPage2()
    {
        SetUI(detailUIPage1, false, "DetailUIPage1");
        SetUI(detailUIPage2, true, "DetailUIPage2");
    }

    public void HideAllDetail()
    {
        SetUI(detailUIPage1, false, "DetailUIPage1");
        SetUI(detailUIPage2, false, "DetailUIPage2");
    }

    // ========= 提交窗口 =========
    public void ShowSubmitSelection(int buildingIndex)
    {
        HideAllSubmitSelection();

        switch (buildingIndex)
        {
            case 0:
                SetUI(submitSelectionUI1, true, "SubmitSelectionUI1");
                break;
            case 1:
                SetUI(submitSelectionUI2, true, "SubmitSelectionUI2");
                break;
            case 2:
                SetUI(submitSelectionUI3, true, "SubmitSelectionUI3");
                break;
            default:
                Debug.LogWarning($"未知的提交窗口索引: {buildingIndex}");
                break;
        }
    }

    public void HideSubmitSelection(int buildingIndex)
    {
        switch (buildingIndex)
        {
            case 0:
                SetUI(submitSelectionUI1, false, "SubmitSelectionUI1");
                break;
            case 1:
                SetUI(submitSelectionUI2, false, "SubmitSelectionUI2");
                break;
            case 2:
                SetUI(submitSelectionUI3, false, "SubmitSelectionUI3");
                break;
            default:
                Debug.LogWarning($"未知的提交窗口索引: {buildingIndex}");
                break;
        }
    }

    public void HideAllSubmitSelection()
    {
        SetUI(submitSelectionUI1, false, "SubmitSelectionUI1");
        SetUI(submitSelectionUI2, false, "SubmitSelectionUI2");
        SetUI(submitSelectionUI3, false, "SubmitSelectionUI3");
    }

    // ========= Dialog =========
    public void ShowDialog() => SetUI(dialogUI, true, "DialogUI");
    public void HideDialog() => SetUI(dialogUI, false, "DialogUI");

    // ========= 交互提示 =========
    public void ShowInteractTip() => SetUI(interactTipUI, true, "InteractTipUI");
    public void HideInteractTip() => SetUI(interactTipUI, false, "InteractTipUI");

    // ========= 背包 =========
    public void ShowBackpack() => SetUI(backpackUI, true, "BackpackUI");
    public void HideBackpack() => SetUI(backpackUI, false, "BackpackUI");

    // ========= 常用组合 =========
    public void OpenHandbookView()
    {
        ShowHandbook();
        HideAllDetail();
        HideAllSubmitSelection();
        HideDialog();
        HideInteractTip();
    }

    public void OpenDetailViewPage1()
    {
        HideHandbook();
        ShowDetailPage1();
        HideAllSubmitSelection();
        HideDialog();
        HideInteractTip();
    }

    public void OpenDetailViewPage2()
    {
        HideHandbook();
        ShowDetailPage2();
        HideAllSubmitSelection();
        HideDialog();
        HideInteractTip();
    }

    public void CloseAllBookUI()
    {
        HideHandbook();
        HideAllDetail();
        HideAllSubmitSelection();
        HideDialog();
        ShowInteractTip();
    }

    public bool IsAnyGameplayBlockingUIOpen()
    {
        return
            IsCanvasGroupOpen(handbookUI) ||
            IsCanvasGroupOpen(detailUIPage1) ||
            IsCanvasGroupOpen(detailUIPage2) ||
            IsCanvasGroupOpen(submitSelectionUI1) ||
            IsCanvasGroupOpen(submitSelectionUI2) ||
            IsCanvasGroupOpen(submitSelectionUI3) ||
            IsCanvasGroupOpen(dialogUI);
    }

    private bool IsCanvasGroupOpen(CanvasGroup cg)
    {
        if (cg == null) return false;

        return cg.alpha > 0.01f && cg.blocksRaycasts;
    }


}