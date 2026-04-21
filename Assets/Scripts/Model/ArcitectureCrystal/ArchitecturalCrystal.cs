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
    Green,
    SmallInkBottle,
    LargeInkBottle
}

public enum ArchitecturalResourceCategory
{
    CommonStructure,
    SpecialStructure,
    InkSupply
}

public enum AttributeBonusType
{
    CurrentHealth,
    MoveSpeed,
    AttackPower,
    Defense,
    Durability,
    MaxHealth,
    None
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
    public ArchitecturalResourceCategory resourceCategory;
    public int inkRestoreValue;

    public ArchitecturalResourceCategory Category
    {
        get
        {
            if (resourceCategory == ArchitecturalResourceCategory.InkSupply)
            {
                return ArchitecturalResourceCategory.InkSupply;
            }

            if (isUnlockMaterial || resourceCategory == ArchitecturalResourceCategory.SpecialStructure)
            {
                return ArchitecturalResourceCategory.SpecialStructure;
            }

            return ArchitecturalResourceCategory.CommonStructure;
        }
    }

    public bool IsCommonStructure => Category == ArchitecturalResourceCategory.CommonStructure;
    public bool IsSpecialStructure => Category == ArchitecturalResourceCategory.SpecialStructure;
    public bool IsInkSupply => Category == ArchitecturalResourceCategory.InkSupply;

    public string DisplayName
    {
        get
        {
            if (IsSpecialStructure)
            {
                return "专用结构材料";
            }

            return ArchitecturalCrystalFactory.GetDisplayName(type);
        }
    }

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
        : this(
            type,
            expValue,
            icon,
            backIcon,
            textDescription,
            bonusType,
            bonusValue,
            subBonusType,
            subBonusValue,
            isUnlockMaterial,
            isUnlockMaterial ? ArchitecturalResourceCategory.SpecialStructure : ArchitecturalResourceCategory.CommonStructure,
            0)
    {
    }

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
        bool isUnlockMaterial,
        ArchitecturalResourceCategory resourceCategory,
        int inkRestoreValue)
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
        this.resourceCategory = resourceCategory;
        this.inkRestoreValue = inkRestoreValue;
    }
}

public static class ArchitecturalCrystalFactory
{
    public static ArchitecturalCrystal CreateCommonStructure(
        ArchitecturalType type,
        Sprite icon = null,
        Sprite backIcon = null)
    {
        return new ArchitecturalCrystal(
            type,
            30,
            icon,
            backIcon != null ? backIcon : icon,
            GetDefaultDescription(type),
            AttributeBonusType.None,
            0f,
            AttributeBonusType.None,
            0f,
            false,
            ArchitecturalResourceCategory.CommonStructure,
            0);
    }

    public static ArchitecturalCrystal CreateSpecialStructureMaterial(
        Sprite icon = null,
        Sprite backIcon = null)
    {
        return new ArchitecturalCrystal(
            ArchitecturalType.MortiseAndTenonJoint,
            0,
            icon,
            backIcon != null ? backIcon : icon,
            "专用结构材料，可用于点亮建筑录槽位。",
            AttributeBonusType.None,
            0f,
            AttributeBonusType.None,
            0f,
            true,
            ArchitecturalResourceCategory.SpecialStructure,
            0);
    }

    public static ArchitecturalCrystal CreateInkSupply(
        bool largeBottle,
        Sprite icon = null,
        Sprite backIcon = null)
    {
        return new ArchitecturalCrystal(
            largeBottle ? ArchitecturalType.LargeInkBottle : ArchitecturalType.SmallInkBottle,
            0,
            icon,
            backIcon != null ? backIcon : icon,
            largeBottle ? "大墨瓶，可恢复 50 点墨笔耐久。" : "小墨瓶，可恢复 20 点墨笔耐久。",
            AttributeBonusType.None,
            0f,
            AttributeBonusType.None,
            0f,
            false,
            ArchitecturalResourceCategory.InkSupply,
            largeBottle ? 50 : 20);
    }

    public static string GetDisplayName(ArchitecturalType type)
    {
        switch (type)
        {
            case ArchitecturalType.MortiseAndTenonJoint:
                return "榫卯";
            case ArchitecturalType.GroundMass:
                return "台基";
            case ArchitecturalType.BeamFrame:
                return "梁架";
            case ArchitecturalType.TampedEarth:
                return "夯土";
            case ArchitecturalType.Tile:
                return "瓦";
            case ArchitecturalType.Brackets:
                return "斗拱";
            case ArchitecturalType.SmallInkBottle:
                return "小墨瓶";
            case ArchitecturalType.LargeInkBottle:
                return "大墨瓶";
            case ArchitecturalType.Gold:
                return "金";
            case ArchitecturalType.White:
                return "白";
            case ArchitecturalType.Green:
                return "绿";
            default:
                return type.ToString();
        }
    }

    public static string GetDefaultDescription(ArchitecturalType type)
    {
        switch (type)
        {
            case ArchitecturalType.Brackets:
                return "斗拱可增加弹体数量，让一次攻击覆盖更大区域。";
            case ArchitecturalType.MortiseAndTenonJoint:
                return "榫卯可提升命中次数，让墨迹继续向前穿透。";
            case ArchitecturalType.Tile:
                return "瓦可增大墨迹体积，提升命中覆盖面。";
            case ArchitecturalType.TampedEarth:
                return "夯土会附带减速，让敌人行动迟缓。";
            case ArchitecturalType.GroundMass:
                return "台基让墨迹附带击退，帮你拉开安全距离。";
            case ArchitecturalType.BeamFrame:
                return "梁架会同步提升弹道速度与射程。";
            default:
                return $"拾取 {GetDisplayName(type)} 后会立即生效。";
        }
    }
}
