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

        RefreshFromRuntimeState();
    }

    private void OnEnable()
    {
        RuntimeProgressState.EnsureInstance().OnStateChanged += RefreshFromRuntimeState;
        RefreshFromRuntimeState();
    }

    private void OnDisable()
    {
        if (RuntimeProgressState.Instance != null)
        {
            RuntimeProgressState.Instance.OnStateChanged -= RefreshFromRuntimeState;
        }
    }

    public void AddProgress(float value)
    {
        if (value <= 0f) return;

        if (RuntimeProgressState.EnsureInstance().AddBuildingProgress(buildingId, Mathf.RoundToInt(value), out _))
        {
            RefreshFromRuntimeState();
        }
    }

    public float GetCurrentProgress()
    {
        return progressSlider == null ? 0f : progressSlider.value;
    }

    public bool IsFull()
    {
        if (progressSlider == null) return false;
        return progressSlider.value >= maxProgress;
    }

    public void RefreshFromRuntimeState()
    {
        if (progressSlider == null)
        {
            return;
        }

        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        maxProgress = definition.requiredProgress;
        progressSlider.minValue = 0f;
        progressSlider.maxValue = maxProgress;
        progressSlider.value = RuntimeProgressState.EnsureInstance().GetBuildingProgress(buildingId);

        if (buildingUnlockState != null)
        {
            buildingUnlockState.RefreshState();
        }
    }
}
