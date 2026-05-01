using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 建筑图片解锁后可点击，点击跳转到详细信息界面
/// </summary>
[RequireComponent(typeof(Button))]
public class UnlockedBuildingImageButton : MonoBehaviour
{
    [Header("当前建筑状态脚本")]
    public CatalogueBuildingUnlockState buildingUnlockState;

    [Header("图鉴主界面")]
    public GameObject illustratedHandbookPanel;

    [Header("详细信息界面")]
    public GameObject detailedInformationPanel;

    [Header("详细信息控制器")]
    public DetailedInformationUI detailedInformationUI;

    [Header("建筑详情数据")]
    public BuildingDetailData buildingDetailData;

    [Header("是否解锁后才允许点击")]
    public bool onlyClickableWhenUnlocked = true;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnClickImage);
        }

        RefreshClickableState();
    }

    private void Update()
    {
        RefreshClickableState();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClickImage);
        }
    }

    private void RefreshClickableState()
    {
        ResolveReferences();
        if (button == null)
        {
            return;
        }

        if (!onlyClickableWhenUnlocked)
        {
            button.interactable = true;
            return;
        }

        if (buildingUnlockState == null)
        {
            button.interactable = false;
            return;
        }

        button.interactable = IsBuildingUnlocked();
    }

    private void OnClickImage()
    {
        ResolveReferences();
        if (onlyClickableWhenUnlocked)
        {
            if (buildingUnlockState == null || !IsBuildingUnlocked())
            {
                return;
            }
        }

        Debug.Log("点击已解锁建筑图片，跳转到详细信息界面");

        if (illustratedHandbookPanel != null)
        {
            illustratedHandbookPanel.SetActive(false);
        }

        if (detailedInformationUI != null && buildingDetailData != null)
        {
            BuildingDetailRuntimeResolver.HideOtherSceneAuthoredDetailCanvases(buildingUnlockState);
            detailedInformationUI.ShowDetail(buildingDetailData);
            return;
        }

        if (detailedInformationPanel != null)
        {
            detailedInformationPanel.SetActive(true);
        }
    }

    private void ResolveReferences()
    {
        buildingUnlockState = BuildingDetailRuntimeResolver.ResolveUnlockState(this, buildingUnlockState);
        buildingDetailData = BuildingDetailRuntimeResolver.ResolveDetailData(this, buildingUnlockState, buildingDetailData);
        detailedInformationUI = BuildingDetailRuntimeResolver.ResolveDetailUi(
            this,
            buildingUnlockState,
            detailedInformationUI,
            detailedInformationPanel);
        detailedInformationPanel = BuildingDetailRuntimeResolver.ResolveDetailPanel(
            detailedInformationUI,
            detailedInformationPanel);
    }

    private bool IsBuildingUnlocked()
    {
        if (buildingUnlockState == null)
        {
            return false;
        }

        RuntimeProgressState runtimeState = RuntimeProgressState.Instance;
        return runtimeState != null
            ? runtimeState.IsBuildingUnlocked(buildingUnlockState.BuildingId)
            : buildingUnlockState.isBuildingUnlocked;
    }
}
