using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 建筑结构物品交互处理器
/// </summary>
public class CrystalInteractHandler : MonoBehaviour, IInteractable
{
    [Header("物品配置")]
    public ArchitecturalType type; // 建筑结构类型
    public int expValue; // 建筑结构构建度
    public Sprite icon; // 建筑结构场景图片
    public Sprite backIcon; // 建筑结构背包中图片
    [TextArea] public string textDescription; // 建筑结构介绍

    public void OnInteract()
    {
        // 创建数据对象
        var data = new ArchitecturalCrystal(type, expValue, icon, backIcon, textDescription);
        // 调用背包系统，将数据加入背包
        var player = FindObjectOfType<PlayerGetArchitectural>();

        if (player != null)
        {
            player.PickCrystal(data);
        }
        else
        {
            Debug.LogError("未找到PlayerGetArchitectural组件，请挂载到玩家身上！");
            return;
        }
        
        // 销毁物体
        Destroy(gameObject);
    }

    public string InteractionTip => $"{type} 构建度 + {expValue}";
}
