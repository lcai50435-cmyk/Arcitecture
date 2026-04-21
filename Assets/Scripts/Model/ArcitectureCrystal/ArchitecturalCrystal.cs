using UnityEngine;

// 建筑结构物品类型
public enum ArchitecturalType
{
    MortiseAndTenonJoint,
    GroundMass,
    BeamFrame,
    TampedEarth,
    Tile,
    Brackets,
    Gold,
    White,
    Green
}

public enum AttributeBonusType
{
    CurrentHealth,
    MoveSpeed,
    AttackPower,
    Defense,
    Durability,
    MaxHealth
}

/// <summary>
/// 建筑结构物品数据信息。
/// </summary>
public struct ArchitecturalCrystal
{
    public ArchitecturalType type;
    public int expValue;
    public Sprite icon;
    public Sprite backIcon;
    public string textDescription;

    public AttributeBonusType bonusType;
    public float bonusValue;

    public AttributeBonusType subBonusType;
    public float subBonusValue;

    public bool isUnlockMaterial;

    public ArchitecturalCrystal(
        ArchitecturalType type,
        int expValue,
        Sprite icon,
        Sprite backIcon,
        string textDescription,
        AttributeBonusType bonusType,
        float bonusValue,
        AttributeBonusType subBonusType,
        float subBonusValue,
        bool isUnlockMaterial = false)
    {
        this.type = type;
        this.expValue = expValue;
        this.icon = icon;
        this.backIcon = backIcon;
        this.textDescription = textDescription;
        this.bonusType = bonusType;
        this.bonusValue = bonusValue;
        this.subBonusType = subBonusType;
        this.subBonusValue = subBonusValue;
        this.isUnlockMaterial = isUnlockMaterial;
    }
}
