using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 建筑晶体类型
public enum ArchitecturalType
{
    BeamFrame,   // 梁架
    Brackets,   // 托架
    GroundMass, // 基质
    Mortise, // 榫卯
    TampedEarth, // 夯土
    Tile // 瓦片
}

// 属性加成类型枚举
public enum AttributeBonusType
{
    MaxHealth,   // 血量上限
    MoveSpeed,   // 移动速度
    AttackPower, // 攻击力
    Defense      // 防御力
}

/// <summary>
/// 建筑晶体道具基础信息
/// </summary>
public class ArchitecturalCrystal
{
    public ArchitecturalType type; // 晶体类型
    public int expValue;  // 晶体经验值
    public Sprite icon; // 道具图标
    public Sprite backIcon; // 道具背景图标
    public string textDescription;  // 建筑晶体描述

    // 属性加成相关
    public AttributeBonusType bonusType; // 该道具提供的属性加成类型
    public float bonusValue;             // 该道具提供的属性加成数值

    /// <summary>
    /// 构造函数
    /// </summary>
    public ArchitecturalCrystal(ArchitecturalType type, int expValue, Sprite icon,
        Sprite backIcon, string textDescription, AttributeBonusType bonusType, float bonusValue)
    {
        this.type = type;
        this.expValue = expValue;
        this.icon = icon;
        this.backIcon = backIcon;
        this.textDescription = textDescription;

        // 初始化属性加成
        this.bonusType = bonusType;
        this.bonusValue = bonusValue;
    }
}