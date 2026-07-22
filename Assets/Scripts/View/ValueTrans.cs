using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ValueTrans : MonoBehaviour
{
    private const float DefaultAnimationDuration = 0.18f;

    public Slider slider;
    public float animationDuration = DefaultAnimationDuration;

    private Coroutine valueAnimation;

    public void SetMaxValue(float maxValue)
    {
        if (slider == null)
        {
            return;
        }

        slider.maxValue = Mathf.Max(slider.minValue, maxValue);
        slider.value = Mathf.Clamp(slider.value, slider.minValue, slider.maxValue);
        ApplyFillClip();
    }

    public void SetValue(float currentValue)
    {
        if (slider == null)
        {
            return;
        }

        float targetValue = Mathf.Clamp(currentValue, slider.minValue, slider.maxValue);
        if (!gameObject.activeInHierarchy || animationDuration <= 0f)
        {
            SetValueImmediate(targetValue);
            return;
        }

        if (Mathf.Approximately(slider.value, targetValue))
        {
            SetValueImmediate(targetValue);
            return;
        }

        if (valueAnimation != null)
        {
            StopCoroutine(valueAnimation);
        }

        valueAnimation = StartCoroutine(AnimateValue(targetValue));
    }

    private void SetValueImmediate(float targetValue)
    {
        if (valueAnimation != null)
        {
            StopCoroutine(valueAnimation);
            valueAnimation = null;
        }

        slider.value = targetValue;
        ApplyFillClip();
    }

    private IEnumerator AnimateValue(float targetValue)
    {
        float startValue = slider.value;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, animationDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            slider.value = Mathf.Lerp(startValue, targetValue, easedProgress);
            ApplyFillClip();
            yield return null;
        }

        slider.value = targetValue;
        ApplyFillClip();
        valueAnimation = null;
    }

    private void ApplyFillClip()
    {
        SliderFillGeometryUtility.ApplyExactFill(slider);
    }
}

public static class SliderFillGeometryUtility
{
    public static void ApplyExactFill(Slider slider, bool normalizeFillContainer = false)
    {
        if (slider == null || slider.fillRect == null)
        {
            return;
        }

        if (normalizeFillContainer)
        {
            NormalizeFillContainer(slider);
        }

        float normalizedValue = ResolveNormalizedValue(slider);
        Vector2 anchorMin;
        Vector2 anchorMax;

        switch (slider.direction)
        {
            case Slider.Direction.RightToLeft:
                anchorMin = new Vector2(1f - normalizedValue, 0f);
                anchorMax = Vector2.one;
                break;
            case Slider.Direction.BottomToTop:
                anchorMin = Vector2.zero;
                anchorMax = new Vector2(1f, normalizedValue);
                break;
            case Slider.Direction.TopToBottom:
                anchorMin = new Vector2(0f, 1f - normalizedValue);
                anchorMax = Vector2.one;
                break;
            default:
                anchorMin = Vector2.zero;
                anchorMax = new Vector2(normalizedValue, 1f);
                break;
        }

        RectTransform fillRect = slider.fillRect;
        fillRect.anchorMin = anchorMin;
        fillRect.anchorMax = anchorMax;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.localScale = Vector3.one;
    }

    private static float ResolveNormalizedValue(Slider slider)
    {
        float range = slider.maxValue - slider.minValue;
        return Mathf.Approximately(range, 0f)
            ? 0f
            : Mathf.Clamp01((slider.value - slider.minValue) / range);
    }

    private static void NormalizeFillContainer(Slider slider)
    {
        RectTransform fillContainer = slider.fillRect.parent as RectTransform;
        if (fillContainer == null || fillContainer == slider.transform)
        {
            return;
        }

        bool horizontal = slider.direction == Slider.Direction.LeftToRight ||
            slider.direction == Slider.Direction.RightToLeft;
        Vector2 anchorMin = fillContainer.anchorMin;
        Vector2 anchorMax = fillContainer.anchorMax;
        Vector2 offsetMin = fillContainer.offsetMin;
        Vector2 offsetMax = fillContainer.offsetMax;

        if (horizontal)
        {
            anchorMin.x = 0f;
            anchorMax.x = 1f;
            offsetMin.x = 0f;
            offsetMax.x = 0f;
        }
        else
        {
            anchorMin.y = 0f;
            anchorMax.y = 1f;
            offsetMin.y = 0f;
            offsetMax.y = 0f;
        }

        fillContainer.anchorMin = anchorMin;
        fillContainer.anchorMax = anchorMax;
        fillContainer.offsetMin = offsetMin;
        fillContainer.offsetMax = offsetMax;
        fillContainer.localScale = Vector3.one;
    }
}
