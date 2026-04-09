using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 第一次捡起物品介绍3UI触发
/// </summary>
public class CrystalDescriptionUI : MonoBehaviour
{
    public TextMeshProUGUI descriptionText;

    private void Start()
    {
        // 监听第一次拾取事件
        BackpackMananger.Instance.OnFirstPick += ShowDescription;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 展示UI文本
    /// </summary>
    /// <param name="desc">介绍文本</param>
    void ShowDescription(string desc)
    {
        descriptionText.text = desc;
        gameObject.SetActive(true);
        Invoke(nameof(Hide), 4f); // 4秒后隐藏UI
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }
}
