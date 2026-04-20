using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 图鉴总进度UI
/// 三个进度条按顺序增长：先满第一条，再加第二条，再加第三条
/// </summary>
public class HandbookTotalProgressUI : MonoBehaviour
{
    [Header("数据来源")]
    public CatalogueAddExp catalogue;

    [Header("三个总进度条（按顺序拖入）")]
    public Slider progressSlider1;
    public Slider progressSlider2;
    public Slider progressSlider3;

    [Header("每个进度条最大值")]
    public int maxValuePerBar = 100;

    private void Start()
    {
        if (catalogue == null)
        {
            catalogue = FindObjectOfType<CatalogueAddExp>();
        }

        if (catalogue == null)
        {
            Debug.LogError("未找到 CatalogueAddExp，无法刷新图鉴总进度！");
            return;
        }

        InitSlider(progressSlider1);
        InitSlider(progressSlider2);
        InitSlider(progressSlider3);

        catalogue.OnProgressChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (catalogue != null)
        {
            catalogue.OnProgressChanged -= RefreshUI;
        }
    }

    private void InitSlider(Slider slider)
    {
        if (slider == null) return;

        slider.minValue = 0;
        slider.maxValue = maxValuePerBar;
        slider.interactable = false;
    }

    /// <summary>
    /// 刷新三个总进度条
    /// </summary>
    public void RefreshUI()
    {
        if (catalogue == null) return;

        int totalProgress = catalogue.GetTotalProgress();

        int bar1 = Mathf.Clamp(totalProgress, 0, maxValuePerBar);
        int bar2 = Mathf.Clamp(totalProgress - maxValuePerBar, 0, maxValuePerBar);
        int bar3 = Mathf.Clamp(totalProgress - maxValuePerBar * 2, 0, maxValuePerBar);

        if (progressSlider1 != null)
        {
            progressSlider1.value = bar1;
        }

        if (progressSlider2 != null)
        {
            progressSlider2.value = bar2;
        }

        if (progressSlider3 != null)
        {
            progressSlider3.value = bar3;
        }

        Debug.Log($"总进度：{totalProgress} | 第一条:{bar1}/100 第二条:{bar2}/100 第三条:{bar3}/100");
    }
}