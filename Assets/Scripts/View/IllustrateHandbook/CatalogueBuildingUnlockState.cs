using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个建筑图鉴条目的完成状态判断
/// 条件：
/// 1. 该建筑自己的进度条达到 100
/// 2. 点击未解锁图标确认完成
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
        BindLockedButton();
        RefreshState();
    }

    private void OnEnable()
    {
        RuntimeProgressState.EnsureInstance().OnStateChanged += RefreshState;
        BindLockedButton();
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
        UnbindLockedButton();
    }

    public void RefreshState()
    {
        ResolveBuildingIdIfNeeded();
        BindLockedButton();

        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();
        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        BuildingRuntimeStateData state = runtimeState.GetBuildingState(buildingId);

        if (buildingSlider != null)
        {
            buildingSlider.minValue = 0f;
            buildingSlider.maxValue = definition.requiredProgress;
            buildingSlider.value = state.progress;
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
            boundLockedButton.interactable = !isBuildingUnlocked && isSliderComplete;
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

    private void HandleLockedBuildingClicked()
    {
        ResolveBuildingIdIfNeeded();

        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();
        if (!runtimeState.TryUnlockBuilding(buildingId, out BuildingRewardDefinition completionReward))
        {
            RefreshState();
            return;
        }

        RefreshState();
        ShowCompletionReward(completionReward);
    }

    private void BindLockedButton()
    {
        Button nextButton = lockedBuildingButton;
        if (nextButton == null && lockedBuildingVisual != null)
        {
            nextButton = lockedBuildingVisual.GetComponent<Button>();
            if (nextButton == null)
            {
                nextButton = lockedBuildingVisual.AddComponent<Button>();
            }

            Image lockedImage = lockedBuildingVisual.GetComponent<Image>();
            if (nextButton.targetGraphic == null && lockedImage != null)
            {
                nextButton.targetGraphic = lockedImage;
            }
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
            boundLockedButton.onClick.AddListener(HandleLockedBuildingClicked);
        }
    }

    private void UnbindLockedButton()
    {
        if (boundLockedButton != null)
        {
            boundLockedButton.onClick.RemoveListener(HandleLockedBuildingClicked);
            boundLockedButton = null;
        }
    }

    private void ShowCompletionReward(BuildingRewardDefinition completionReward)
    {
        if (completionReward == null)
        {
            return;
        }

        if (dialogUI == null)
        {
            dialogUI = FindObjectOfType<Dialog>(true);
        }

        if (dialogUI != null)
        {
            dialogUI.ShowClickCloseDialog($"{completionReward.title}\n{completionReward.description}");
        }
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
