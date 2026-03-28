using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackpackUI : MonoBehaviour
{
    [Header("背包格子")]
    public GameObject[] backPackGrid;  // 设置背包格子位置

    private PlayerGetArchitectural playerGetArc;  

    void Awake()
    {
        playerGetArc = FindObjectOfType<PlayerGetArchitectural>();

        // UI订阅事件
        playerGetArc.OnBackpackChanged += RefreshUI;
    }

    /// <summary> 纯UI渲染 </summary>
    void RefreshUI()
    {
        var items = playerGetArc.GetBackpackItems();

        for (int i = 0; i < backPackGrid.Length; i++)
        {
            // 直接从 背包格子 拿到 Sprite 组件
            SpriteRenderer spriteRenderer = backPackGrid[i].GetComponent<SpriteRenderer>();

            if (i < items.Count)
            {
                spriteRenderer.sprite = items[i].backIcon;  // 换图片
                spriteRenderer.enabled = true;          // 显示
            }
            else
            {
                spriteRenderer.sprite = null;           // 清空图片
                spriteRenderer.enabled = false;         // 隐藏
            }
        }
    }
}
