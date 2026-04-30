using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个建筑图鉴条目的完成状态判断
/// 条件：
/// 1. 通用材料最多推进到 70% 解锁度
/// 2. 3 个专用槽位点亮后补足剩余 30%
/// 3. 点击未解锁图标确认完成
/// </summary>
public class CatalogueBuildingUnlockState : MonoBehaviour
{
    [Header("建筑编号")]
    public CatalogueBuildingId buildingId = CatalogueBuildingId.Building1;

    [Header("该建筑自己的进度条")]
    public Slider buildingSlider;

    [Header("该建筑下的3个点亮槽位")]
    public CatalogueUnlockSlotButton[] slotButtons;

    [Header("完成解锁后显示的物体（可选）")]
    public GameObject unlockedBuildingVisual;

    [Header("未完成时显示的物体（可选）")]
    public GameObject lockedBuildingVisual;

    [Header("点击未解锁图标完成建筑（可选）")]
    public Button lockedBuildingButton;

    [Header("建筑介绍数据（可选）")]
    public BuildingDetailData buildingDetailData;

    [Header("奖励弹窗（可选）")]
    public Dialog dialogUI;

    [Header("运行时状态观察")]
    public bool isSliderComplete;
    public bool areAllSlotsUnlocked;
    public bool isBuildingUnlocked;

    public CatalogueBuildingId BuildingId => buildingId;

    private Button boundLockedButton;

    private void Start()
    {
        BindBuildingButtons();
        RefreshState();
    }

    private void OnEnable()
    {
        RuntimeProgressState.EnsureInstance().OnStateChanged += RefreshState;
        BindBuildingButtons();
        RefreshState();
    }

    private void OnDisable()
    {
        if (RuntimeProgressState.Instance != null)
        {
            RuntimeProgressState.Instance.OnStateChanged -= RefreshState;
        }
    }

    private void OnDestroy()
    {
        UnbindBuildingButtons();
    }

    public void RefreshState()
    {
        ResolveBuildingIdIfNeeded();
        BindBuildingButtons();

        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();
        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        BuildingRuntimeStateData state = runtimeState.GetBuildingState(buildingId);

        if (buildingSlider != null)
        {
            buildingSlider.minValue = 0f;
            buildingSlider.maxValue = definition.requiredProgress;
            buildingSlider.value = state.progress;
            SliderFillGeometryUtility.ApplyExactFill(buildingSlider, true);
        }

        isSliderComplete = state.progress >= definition.requiredProgress;
        int slotCount = definition.slotDefinitions != null ? definition.slotDefinitions.Length : 0;
        areAllSlotsUnlocked = runtimeState.GetUnlockedSlotCount(buildingId) >= slotCount;
        isBuildingUnlocked = runtimeState.IsBuildingUnlocked(buildingId);

        if (unlockedBuildingVisual != null)
        {
            unlockedBuildingVisual.SetActive(isBuildingUnlocked);
        }

        if (lockedBuildingVisual != null)
        {
            lockedBuildingVisual.SetActive(!isBuildingUnlocked);
        }

        if (boundLockedButton != null)
        {
            boundLockedButton.interactable = runtimeState.CanUnlockBuilding(buildingId);
        }

        if (slotButtons != null)
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] != null)
                {
                    slotButtons[i].RefreshVisual();
                }
            }
        }

    }

    private void HandleBuildingButtonClicked()
    {
        ResolveBuildingIdIfNeeded();

        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();
        if (runtimeState.IsBuildingUnlocked(buildingId))
        {
            RefreshState();
            return;
        }

        if (!runtimeState.TryUnlockBuilding(buildingId, out _))
        {
            RefreshState();
            return;
        }

        RefreshState();
        ShowBuildingIntroduction();
    }

    private void BindBuildingButtons()
    {
        BindLockedButton();
    }

    private void BindLockedButton()
    {
        Button nextButton = lockedBuildingButton;
        if (nextButton == null && lockedBuildingVisual != null)
        {
            nextButton = EnsureButtonOnVisual(lockedBuildingVisual);
        }

        if (boundLockedButton == nextButton)
        {
            return;
        }

        UnbindLockedButton();
        boundLockedButton = nextButton;
        lockedBuildingButton = nextButton;

        if (boundLockedButton != null)
        {
            boundLockedButton.onClick.AddListener(HandleBuildingButtonClicked);
        }
    }

    private void UnbindBuildingButtons()
    {
        UnbindLockedButton();
    }

    private void UnbindLockedButton()
    {
        if (boundLockedButton != null)
        {
            boundLockedButton.onClick.RemoveListener(HandleBuildingButtonClicked);
            boundLockedButton = null;
        }
    }

    private static Button EnsureButtonOnVisual(GameObject visual)
    {
        if (visual == null)
        {
            return null;
        }

        Button button = visual.GetComponent<Button>();
        if (button == null)
        {
            button = visual.AddComponent<Button>();
        }

        Image image = visual.GetComponent<Image>();
        if (button.targetGraphic == null && image != null)
        {
            button.targetGraphic = image;
        }

        return button;
    }

    private void ShowBuildingIntroduction()
    {
        string content = BuildBuildingIntroductionContent();
        dialogUI = Dialog.EnsureTopmostRuntimeInstance();
        if (dialogUI == null)
        {
            Debug.Log(content);
            return;
        }

        dialogUI.ShowClickCloseDialog(content);
        dialogUI.EnsureTopmostRuntimePanelInputSurface();
    }

    private string BuildBuildingIntroductionContent()
    {
        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        BuildingDetailData detailData = ResolveBuildingDetailData();

        string title = definition.displayName;
        if (detailData != null && !string.IsNullOrWhiteSpace(detailData.buildingName))
        {
            title = detailData.buildingName;
        }
        else if (!string.IsNullOrWhiteSpace(definition.detailTitle))
        {
            title = definition.detailTitle;
        }

        string introduction = definition.detailDescription;
        if (detailData != null && !string.IsNullOrWhiteSpace(detailData.introduction1))
        {
            introduction = detailData.introduction1;
        }

        string supplement = string.Empty;
        if (detailData != null)
        {
            if (!string.IsNullOrWhiteSpace(detailData.finalIntroduction))
            {
                supplement = detailData.finalIntroduction;
            }
            else if (!string.IsNullOrWhiteSpace(detailData.introduction2))
            {
                supplement = detailData.introduction2;
            }
        }

        return string.IsNullOrWhiteSpace(supplement)
            ? $"{title}\n{introduction}"
            : $"{title}\n{introduction}\n\n{supplement}";
    }

    private BuildingDetailData ResolveBuildingDetailData()
    {
        if (buildingDetailData != null)
        {
            return buildingDetailData;
        }

        BuildingDetailOpenButton detailOpenButton = GetComponentInChildren<BuildingDetailOpenButton>(true);
        if (detailOpenButton != null && detailOpenButton.buildingDetailData != null)
        {
            buildingDetailData = detailOpenButton.buildingDetailData;
            return buildingDetailData;
        }

        buildingDetailData = GetComponentInChildren<BuildingDetailData>(true);
        return buildingDetailData;
    }

    private void ResolveBuildingIdIfNeeded()
    {
        BuildingProgressController controller = GetComponent<BuildingProgressController>();
        if (controller != null)
        {
            buildingId = controller.buildingId;
        }
    }
}
