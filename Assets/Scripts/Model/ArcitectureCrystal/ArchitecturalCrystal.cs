using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 建筑结构物品类型
public enum ArchitecturalType
{
    Gold,   // 金色 30
    White,  // 白色 10
    Green   // 绿色 20
}

/// <summary>
/// 建筑结构物品相关信息
/// </summary>
public class ArchitecturalCrystal
{
    public ArchitecturalType type; // 建筑类型
    public int expValue;  // 建筑构建度
    public Sprite icon; // 物品图标
    public Sprite backIcon; // 物品背包图标
    public string textDescription;  // 建筑结构描述

    /// <summary>
    /// 快速创建建筑结构物品
    /// </summary>
    /// <param name="type"></param>
    /// <param name="expValue"></param>
    /// <param name="icon"></param>
    /// <param name="backIcon"></param>
    /// <param name="textDescription"></param>
    public ArchitecturalCrystal(ArchitecturalType type, int expValue, Sprite icon, 
        Sprite backIcon, string textDescription)
    {
        this.type = type;
        this.expValue = expValue;
        this.icon = icon;
        this.backIcon = backIcon;
        this.textDescription = textDescription;
    }
}
