using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DetailedInformationUI : MonoBehaviour
{
    [Header("图鉴主界面")]
    public GameObject illustratedHandbookPanel;

    [Header("详细信息总界面")]
    public GameObject detailedInformationPanel;

    [Header("第一页")]
    public GameObject backGround1;
    public Image page1Image;
    public Text page1NameText;
    public Text page1IntroductionText;
    public Button nextPageButton;
    public Button closeButton1;

    [Header("第二页")]
    public GameObject backGround2;
    public Image page2Image;
    public Text page2IntroductionText;
    public Text page2FinallyIntroductionText;
    public Button previousPageButton;
    public Button closeButton2;

    private void Start()
    {
        ApplyRuntimeFonts();
        BindButtons();

        ShowPage1Only();
    }

    private void OnDestroy()
    {
        if (nextPageButton != null)
            nextPageButton.onClick.RemoveListener(ShowPage2);

        if (previousPageButton != null)
            previousPageButton.onClick.RemoveListener(ShowPage1);

        if (closeButton1 != null)
            closeButton1.onClick.RemoveListener(CloseDetailOnlyReturnHandbook);

        if (closeButton2 != null)
            closeButton2.onClick.RemoveListener(CloseDetailOnlyReturnHandbook);
    }

    /// <summary>
    /// 显示建筑详细信息
    /// </summary>
    public void ShowDetail(BuildingDetailData data)
    {
        if (data == null) return;

        if (page1NameText != null)
            page1NameText.text = data.buildingName;

        if (page1Image != null)
        {
            page1Image.sprite = data.detailSprite1;
            page1Image.enabled = data.detailSprite1 != null;
        }

        if (page1IntroductionText != null)
            page1IntroductionText.text = data.introduction1;

        if (page2Image != null)
        {
            page2Image.sprite = data.detailSprite2;
            page2Image.enabled = data.detailSprite2 != null;
        }

        if (page2IntroductionText != null)
            page2IntroductionText.text = data.introduction2;

        if (page2FinallyIntroductionText != null)
            page2FinallyIntroductionText.text = data.finalIntroduction;

        PrepareDetailPanelForDisplay();

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.OpenModal(RuntimeModalType.DetailPage1, RuntimeModalOpenSource.None, true);
        }

        ShowPage1Only();
    }

    /// <summary>
    /// 显示第一页
    /// </summary>
    public void ShowPage1()
    {
        PrepareDetailPanelForDisplay();

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.OpenModal(RuntimeModalType.DetailPage1, RuntimeModalOpenSource.None, true);
        }

        ShowPage1Only();
    }

    /// <summary>
    /// 显示第二页
    /// </summary>
    public void ShowPage2()
    {
        PrepareDetailPanelForDisplay();

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.OpenModal(RuntimeModalType.DetailPage2, RuntimeModalOpenSource.None, true);
        }

        if (backGround1 != null)
            backGround1.SetActive(false);

        if (backGround2 != null)
            backGround2.SetActive(true);
    }

    /// <summary>
    /// 只显示第一页（本地页面状态）
    /// </summary>
    private void ShowPage1Only()
    {
        if (backGround1 != null)
            backGround1.SetActive(true);

        if (backGround2 != null)
            backGround2.SetActive(false);
    }

    /// <summary>
    /// 关闭整个图鉴系统
    /// </summary>
    public void CloseAllUI()
    {
        ShowPage1Only();
        UIManager.Instance?.CloseIllustratedHandbook();
    }

    /// <summary>
    /// 从详情页回到图鉴主页
    /// </summary>
    public void CloseDetailOnlyReturnHandbook()
    {
        if (detailedInformationPanel != null)
            detailedInformationPanel.SetActive(false);

        if (illustratedHandbookPanel != null)
            illustratedHandbookPanel.SetActive(true);

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.OpenModal(RuntimeModalType.Handbook, RuntimeModalOpenSource.None, true);
        }

        ShowPage1Only();
    }

    public bool IsDetailVisible()
    {
        GameObject detailRoot = detailedInformationPanel != null
            ? detailedInformationPanel
            : gameObject;

        if (detailRoot == null || !detailRoot.activeInHierarchy)
        {
            return false;
        }

        RectTransform rectTransform = detailRoot.transform as RectTransform;
        if (rectTransform != null &&
            (Mathf.Approximately(rectTransform.localScale.x, 0f) ||
             Mathf.Approximately(rectTransform.localScale.y, 0f)))
        {
            return false;
        }

        CanvasGroup canvasGroup = detailRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            return true;
        }

        return canvasGroup.alpha > 0.01f && canvasGroup.blocksRaycasts;
    }

    private void PrepareDetailPanelForDisplay()
    {
        ResolveRuntimeReferences();

        GameObject detailRoot = detailedInformationPanel != null
            ? detailedInformationPanel
            : gameObject;
        EnsureAncestorsVisible(detailRoot.transform);
        detailRoot.SetActive(true);
        detailRoot.transform.SetAsLastSibling();

        RectTransform detailRect = detailRoot.transform as RectTransform;
        if (detailRect != null &&
            Mathf.Approximately(detailRect.localScale.x, 0f) &&
            Mathf.Approximately(detailRect.localScale.y, 0f))
        {
            detailRect.localScale = Vector3.one;
        }

        CanvasGroup canvasGroup = detailRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = detailRoot.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        Canvas canvas = detailRoot.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = detailRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = RuntimeModalStyle.ModalSortingOrder + 2;

        if (detailRoot.GetComponent<GraphicRaycaster>() == null)
        {
            detailRoot.AddComponent<GraphicRaycaster>();
        }

        BindButtons();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.detailedInformation = detailRoot;
        }
    }

    private void ResolveRuntimeReferences()
    {
        GameObject detailRoot = detailedInformationPanel != null
            ? detailedInformationPanel
            : gameObject;
        if (detailedInformationPanel == null)
        {
            detailedInformationPanel = detailRoot;
        }

        if (illustratedHandbookPanel != null)
        {
            return;
        }

        Transform current = detailRoot.transform.parent;
        while (current != null)
        {
            if (current.name == IllustratedHandbookTabsController.IllustratedHandbookCanvasName)
            {
                illustratedHandbookPanel = current.gameObject;
                return;
            }

            if (current.name == IllustratedHandbookTabsController.RootObjectName)
            {
                Transform handbookPage = current.Find(IllustratedHandbookTabsController.IllustratedHandbookCanvasName);
                illustratedHandbookPanel = handbookPage != null ? handbookPage.gameObject : current.gameObject;
                return;
            }

            current = current.parent;
        }
    }

    private void EnsureAncestorsVisible(Transform detailRoot)
    {
        Transform current = detailRoot != null ? detailRoot.parent : null;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }

            RectTransform rectTransform = current as RectTransform;
            if (rectTransform != null &&
                Mathf.Approximately(rectTransform.localScale.x, 0f) &&
                Mathf.Approximately(rectTransform.localScale.y, 0f))
            {
                rectTransform.localScale = Vector3.one;
            }

            CanvasGroup canvasGroup = current.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            current = current.parent;
        }
    }

    private void BindButtons()
    {
        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveListener(ShowPage2);
            nextPageButton.onClick.AddListener(ShowPage2);
        }

        if (previousPageButton != null)
        {
            previousPageButton.onClick.RemoveListener(ShowPage1);
            previousPageButton.onClick.AddListener(ShowPage1);
        }

        ResolveCloseButtons();
        BindCloseButton(closeButton1);
        BindCloseButton(closeButton2);
    }

    private void ResolveCloseButtons()
    {
        Transform searchRoot = detailedInformationPanel != null
            ? detailedInformationPanel.transform
            : transform;

        if (closeButton1 == null)
        {
            closeButton1 = FindButton(searchRoot, "Close", "Setting", "关闭");
        }

        if (closeButton2 == null)
        {
            closeButton2 = FindButton(searchRoot, "Previous", "Back", "返回");
            if (closeButton2 == closeButton1)
            {
                closeButton2 = null;
            }
        }
    }

    private void BindCloseButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(CloseDetailOnlyReturnHandbook);
        button.onClick.AddListener(CloseDetailOnlyReturnHandbook);
        button.interactable = true;

        Graphic targetGraphic = button.targetGraphic;
        if (targetGraphic == null)
        {
            targetGraphic = button.GetComponent<Graphic>();
            button.targetGraphic = targetGraphic;
        }

        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = true;
        }
    }

    private static Button FindButton(Transform root, params string[] nameFragments)
    {
        if (root == null)
        {
            return null;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int fragmentIndex = 0; fragmentIndex < nameFragments.Length; fragmentIndex++)
        {
            string fragment = nameFragments[fragmentIndex];
            if (string.IsNullOrEmpty(fragment))
            {
                continue;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                Text label = button.GetComponentInChildren<Text>(true);
                TMP_Text tmpLabel = button.GetComponentInChildren<TMP_Text>(true);
                bool matchesName = button.name.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchesLabel = label != null &&
                                    label.text != null &&
                                    label.text.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchesTmpLabel = tmpLabel != null &&
                                       tmpLabel.text != null &&
                                       tmpLabel.text.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (matchesName || matchesLabel || matchesTmpLabel)
                {
                    return button;
                }
            }
        }

        return null;
    }

    private void ApplyRuntimeFonts()
    {
        RuntimeTextFontRepair.RepairLegacyText(page1NameText);
        RuntimeTextFontRepair.RepairLegacyText(page1IntroductionText);
        RuntimeTextFontRepair.RepairLegacyText(page2IntroductionText);
        RuntimeTextFontRepair.RepairLegacyText(page2FinallyIntroductionText);
    }
}
