using UnityEngine;
using UnityEngine.UI;

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
        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(ShowPage2);

        if (previousPageButton != null)
            previousPageButton.onClick.AddListener(ShowPage1);

        if (closeButton1 != null)
            closeButton1.onClick.AddListener(CloseDetailOnlyReturnHandbook);

        if (closeButton2 != null)
            closeButton2.onClick.AddListener(CloseDetailOnlyReturnHandbook);

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

        // 打开详细页第一页
        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.OpenDetailViewPage1();
        }

        ShowPage1Only();
    }

    /// <summary>
    /// 显示第一页
    /// </summary>
    public void ShowPage1()
    {
        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.OpenDetailViewPage1();
        }

        ShowPage1Only();
    }

    /// <summary>
    /// 显示第二页
    /// </summary>
    public void ShowPage2()
    {
        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.OpenDetailViewPage2();
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
        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.CloseAllBookUI();
        }

        ShowPage1Only();
        UIManager.Instance?.RestoreUI();
    }

    /// <summary>
    /// 从详细页回到图鉴主页
    /// </summary>
    public void CloseDetailOnlyReturnHandbook()
    {
        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.HideAllDetail();
            UIRootManager.Instance.ShowHandbook();
        }

        ShowPage1Only();
    }
}