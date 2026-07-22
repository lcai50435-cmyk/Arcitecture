using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overall catalogue progress UI.
/// The three progress bars fill in order: first the first bar, then the second, then the third.
/// </summary>
public class UniversalCollectionProgress : MonoBehaviour
{
    [Header("三个总进度条（按顺序拖入）")]
    public Slider progressSlider1;
    public Slider progressSlider2;
    public Slider progressSlider3;

    [Header("每个进度条最大值")]
    public int maxValuePerBar = 100;

    private void Start()
    {
        InitSlider(progressSlider1);
        InitSlider(progressSlider2);
        InitSlider(progressSlider3);

        RuntimeProgressState.EnsureInstance().OnStateChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (RuntimeProgressState.Instance != null)
        {
            RuntimeProgressState.Instance.OnStateChanged -= RefreshUI;
        }
    }

    private void InitSlider(Slider slider)
    {
        if (slider == null) return;

        slider.minValue = 0;
        slider.maxValue = maxValuePerBar;
        slider.interactable = false;
        SliderFillGeometryUtility.ApplyExactFill(slider, true);
    }

    public void RefreshUI()
    {
        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();
        int totalProgress = runtimeState.GetTotalProgress();

        int bar1 = Mathf.Clamp(totalProgress, 0, maxValuePerBar);
        int bar2 = Mathf.Clamp(totalProgress - maxValuePerBar, 0, maxValuePerBar);
        int bar3 = Mathf.Clamp(totalProgress - maxValuePerBar * 2, 0, maxValuePerBar);

        if (progressSlider1 != null)
        {
            progressSlider1.value = bar1;
            SliderFillGeometryUtility.ApplyExactFill(progressSlider1, true);
        }

        if (progressSlider2 != null)
        {
            progressSlider2.value = bar2;
            SliderFillGeometryUtility.ApplyExactFill(progressSlider2, true);
        }

        if (progressSlider3 != null)
        {
            progressSlider3.value = bar3;
            SliderFillGeometryUtility.ApplyExactFill(progressSlider3, true);
        }

    }
}
