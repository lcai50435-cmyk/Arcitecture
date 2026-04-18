using UnityEngine;

// 建筑结构物品类型
public enum ArchitecturalType
{
    Gold,
    White,
    Green
}

public enum AttributeBonusType
{
    MaxHealth, // 血量上限
    MoveSpeed, // 移动速度
    AttackPower, // 攻击力
    Defense // 防御力
}

/// <summary>
/// 建筑结构物品相关信息
/// </summary>
public class ArchitecturalCrystal
{
    public ArchitecturalType type;   // 建筑类型
    public int expValue;             // 建筑构建度
    public Sprite icon;              // 场景图标
    public Sprite backIcon;          // 背包图标
    public string textDescription;   // 描述文本

    // 属性相关加成
    public AttributeBonusType bonusType;
    public float bonusValue;

    // 是否为专用点亮道具
    public bool isUnlockMaterial;

    public ArchitecturalCrystal(
        ArchitecturalType type,
        int expValue,
        Sprite icon,
        Sprite backIcon,
        string textDescription, 
        AttributeBonusType bonusType,
        float bonusValue,
        bool isUnlockMaterial = false)
    {
        this.type = type;
        this.expValue = expValue;
        this.icon = icon;
        this.backIcon = backIcon;
        this.textDescription = textDescription;
        this.bonusType = bonusType;
        this.bonusValue = bonusValue;
        this.isUnlockMaterial = isUnlockMaterial;
    }
}