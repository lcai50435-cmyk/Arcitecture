using UnityEngine;
using UnityEngine.UI;

public class BuildingProgressController : MonoBehaviour
{
    [Header("建筑编号")]
    public CatalogueBuildingId buildingId;

    [Header("对应进度条")]
    public Slider progressSlider;

    [Header("最大进度")]
    public float maxProgress = 100f;

    [Header("对应建筑解锁状态")]
    public CatalogueBuildingUnlockState buildingUnlockState;

    private void Awake()
    {
        if (buildingUnlockState == null)
        {
            buildingUnlockState = GetComponent<CatalogueBuildingUnlockState>();
        }
    }

    public void AddProgress(float value)
    {
        if (progressSlider == null) return;
        if (value <= 0f) return;

        progressSlider.value = Mathf.Clamp(progressSlider.value + value, 0f, maxProgress);

        Debug.Log($"{buildingId} 增加进度 {value}，当前：{progressSlider.value}/{maxProgress}");

        // 关键：进度条变化后，立刻刷新建筑解锁状态
        if (buildingUnlockState != null)
        {
            buildingUnlockState.RefreshState();
        }
    }

    public float GetCurrentProgress()
    {
        if (progressSlider == null) return 0f;
        return progressSlider.value;
    }

    public bool IsFull()
    {
        if (progressSlider == null) return false;
        return progressSlider.value >= maxProgress;
    }
}