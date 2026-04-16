using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 控制建筑大图颜色：
/// 初始为较深灰色，随着 Slider 和小图标点亮逐渐变亮，最终恢复正常显示
/// </summary>
public class ImageColorController : MonoBehaviour
{
    [Header("建筑大图")]
    public Image targetImage;

    [Header("对应的进度条")]
    public Slider buildingSlider;

    [Header("该建筑下的3个小图标按钮")]
    public CatalogueUnlockSlotButton[] slotButtons;

    [Header("Slider权重")]
    [Range(0f, 1f)]
    public float sliderWeight = 0.7f;

    [Header("小图标权重")]
    [Range(0f, 1f)]
    public float slotWeight = 0.3f;

    [Header("初始灰度（比小图标更灰）")]
    [Range(0f, 1f)]
    public float baseGray = 0.25f;

    private void Update()
    {
        UpdateColor();
    }

    private void UpdateColor()
    {
        if (targetImage == null || buildingSlider == null)
        {
            return;
        }

        // 1. Slider 进度，0~1
        float sliderProgress = 0f;
        if (buildingSlider.maxValue > 0f)
        {
            sliderProgress = buildingSlider.value / buildingSlider.maxValue;
        }
        sliderProgress = Mathf.Clamp01(sliderProgress);

        // 2. 小图标点亮进度，0~1
        float slotProgress = 0f;
        if (slotButtons != null && slotButtons.Length > 0)
        {
            int unlockedCount = 0;

            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] != null && slotButtons[i].IsUnlocked)
                {
                    unlockedCount++;
                }
            }

            slotProgress = (float)unlockedCount / slotButtons.Length;
        }

        // 3. 组合进度
        float combinedProgress = sliderProgress * sliderWeight + slotProgress * slotWeight;
        combinedProgress = Mathf.Clamp01(combinedProgress);

        // 4. 从 baseGray 渐变到 1（白色）
        float brightness = Mathf.Lerp(baseGray, 1f, combinedProgress);

        targetImage.color = new Color(brightness, brightness, brightness, 1f);
    }
}