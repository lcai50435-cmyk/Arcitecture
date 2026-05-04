using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildingDetailOpenButton : MonoBehaviour
{
    [Header("当前建筑是否解锁")]
    public CatalogueBuildingUnlockState buildingUnlockState;

    [Header("当前建筑的数据")]
    public BuildingDetailData buildingDetailData;

    [Header("详细信息界面控制器")]
    public DetailedInformationUI detailedInformationUI;

    private Button button;

    private void Awake()
    {
        ResolveButton();
    }

    private void Start()
    {
        BindClickHandler();
        RefreshClickable();
    }

    private void OnEnable()
    {
        BindClickHandler();
        RefreshClickable();
    }

    private void Update()
    {
        RefreshClickable();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OpenDetail);
        }
    }

    private void RefreshClickable()
    {
        ResolveButton();
        ResolveReferences();
        if (button == null) return;

        button.interactable = IsBuildingUnlocked() && buildingDetailData != null && detailedInformationUI != null;
    }

    private void OpenDetail()
    {
        ResolveReferences();

        if (!IsBuildingUnlocked())
        {
            return;
        }

        if (detailedInformationUI == null)
        {
            Debug.LogError("DetailedInformationUI 未绑定");
            return;
        }

        BuildingDetailRuntimeResolver.HideOtherSceneAuthoredDetailCanvases(buildingUnlockState);
        detailedInformationUI.ShowDetail(buildingDetailData);
    }

    private void ResolveButton()
    {
        if (button != null)
        {
            return;
        }

        button = GetComponent<Button>();
        Image image = GetComponent<Image>();
        if (button != null && image != null)
        {
            button.targetGraphic = button.targetGraphic != null ? button.targetGraphic : image;
            image.raycastTarget = true;
        }
    }

    private void BindClickHandler()
    {
        ResolveButton();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(OpenDetail);
        button.onClick.AddListener(OpenDetail);
    }

    private void ResolveReferences()
    {
        buildingUnlockState = BuildingDetailRuntimeResolver.ResolveUnlockState(this, buildingUnlockState);
        buildingDetailData = BuildingDetailRuntimeResolver.ResolveDetailData(this, buildingUnlockState, buildingDetailData);
        detailedInformationUI = BuildingDetailRuntimeResolver.ResolveDetailUi(
            this,
            buildingUnlockState,
            detailedInformationUI,
            null);
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
