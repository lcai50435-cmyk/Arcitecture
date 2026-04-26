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
            yield return null;
        }

        slider.value = targetValue;
        valueAnimation = null;
    }
}
