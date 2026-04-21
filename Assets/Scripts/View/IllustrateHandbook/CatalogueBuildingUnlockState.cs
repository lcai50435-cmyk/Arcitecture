using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个建筑图鉴条目的完成状态判断
/// 条件：
/// 1. 该建筑自己的进度条达到 100
/// 2. 该建筑下 3 个槽位全部点亮
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

    [Header("运行时状态观察")]
    public bool isSliderComplete;
    public bool areAllSlotsUnlocked;
    public bool isBuildingUnlocked;

    public CatalogueBuildingId BuildingId => buildingId;

    private void Start()
    {
        RefreshState();
    }

    private void OnEnable()
    {
        RuntimeProgressState.EnsureInstance().OnStateChanged += RefreshState;
        RefreshState();
    }

    private void OnDisable()
    {
        if (RuntimeProgressState.Instance != null)
        {
            RuntimeProgressState.Instance.OnStateChanged -= RefreshState;
        }
    }

    public void RefreshState()
    {
        ResolveBuildingIdIfNeeded();

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
        areAllSlotsUnlocked = runtimeState.GetUnlockedSlotCount(buildingId) >= definition.slotDefinitions.Length;
        isBuildingUnlocked = runtimeState.IsBuildingUnlocked(buildingId);

        if (unlockedBuildingVisual != null)
        {
            unlockedBuildingVisual.SetActive(isBuildingUnlocked);
        }

        if (lockedBuildingVisual != null)
        {
            lockedBuildingVisual.SetActive(!isBuildingUnlocked);
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

        Debug.Log(
            $"{gameObject.name} 建筑状态：Slider完成={isSliderComplete}，槽位完成={areAllSlotsUnlocked}，最终完成={isBuildingUnlocked}");
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
