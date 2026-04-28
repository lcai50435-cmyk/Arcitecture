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
        if (slider == null || slider.fillRect == null)
        {
            return;
        }

        float range = slider.maxValue - slider.minValue;
        float normalizedValue = Mathf.Approximately(range, 0f)
            ? 0f
            : Mathf.Clamp01((slider.value - slider.minValue) / range);

        Vector2 anchorMin = slider.fillRect.anchorMin;
        Vector2 anchorMax = slider.fillRect.anchorMax;

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

        slider.fillRect.anchorMin = anchorMin;
        slider.fillRect.anchorMax = anchorMax;
    }
}
