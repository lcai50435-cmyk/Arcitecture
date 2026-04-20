using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ValueTrans : MonoBehaviour
{
    public Slider slider;

    public void SetMaxValue(float maxValue)
    {
        slider.maxValue = maxValue;
        slider.value = maxValue;
    }

    public void SetValue(float currentValue)
    {
        slider.value = currentValue;
    }
}
