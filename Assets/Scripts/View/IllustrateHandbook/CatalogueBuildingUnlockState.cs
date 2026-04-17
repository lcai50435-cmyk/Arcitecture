using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个建筑图鉴条目的完成状态判断
/// 条件：
/// 1. 该建筑自己的Slider达到100
/// 2. 该建筑下3个槽位全部点亮
/// </summary>
public class CatalogueBuildingUnlockState : MonoBehaviour
{
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

    private void Start()
    {
        RefreshState();
    }

    public void RefreshState()
    {
        isSliderComplete = CheckSliderComplete();
        areAllSlotsUnlocked = CheckAllSlotsUnlocked();
        isBuildingUnlocked = isSliderComplete && areAllSlotsUnlocked;

        if (unlockedBuildingVisual != null)
        {
            unlockedBuildingVisual.SetActive(isBuildingUnlocked);
        }

        if (lockedBuildingVisual != null)
        {
            lockedBuildingVisual.SetActive(!isBuildingUnlocked);
        }

        Debug.Log($"{gameObject.name} 建筑状态：Slider完成={isSliderComplete}，槽位完成={areAllSlotsUnlocked}，最终完成={isBuildingUnlocked}");
    }

    private bool CheckSliderComplete()
    {
        if (buildingSlider == null)
        {
            return false;
        }

        return buildingSlider.value >= 100f;
    }

    private bool CheckAllSlotsUnlocked()
    {
        if (slotButtons == null || slotButtons.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == null || !slotButtons[i].IsUnlocked)
            {
                return false;
            }
        }

        return true;
    }
}