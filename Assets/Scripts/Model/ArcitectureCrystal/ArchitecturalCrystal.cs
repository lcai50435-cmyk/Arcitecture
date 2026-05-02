using System.Collections.Generic;
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
    InkSupply,
    RepairMaterial
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
    // 兼容旧场景与 prefab 序列化，普通结构的构建度已改由 buildProgressPercent 驱动。
    public int expValue;
    public int buildProgressPercent;
    public Sprite icon;
    public Sprite backIcon;
    public string textDescription;

    public AttributeBonusType bonusType;
    public float bonusValue;

    public AttributeBonusType subBonusType;
    public float subBonusValue;

    public bool isUnlockMaterial;
    public ArchitecturalResourceCategory resourceCategory;
    public CatalogueBuildingId repairBuildingId;
    public int inkRestoreValue;
    public int runtimePickupOrder;

    public ArchitecturalResourceCategory Category
    {
        get
        {
            if (resourceCategory == ArchitecturalResourceCategory.InkSupply)
            {
                return ArchitecturalResourceCategory.InkSupply;
            }

            if (resourceCategory == ArchitecturalResourceCategory.RepairMaterial)
            {
                return ArchitecturalResourceCategory.RepairMaterial;
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
    public bool IsRepairMaterial => Category == ArchitecturalResourceCategory.RepairMaterial;
    public bool IsGenericCommonMaterial => IsCommonStructure && IsGenericCommonMaterialType(type);

    public string DisplayName
    {
        get
        {
            if (IsRepairMaterial)
            {
                return $"{BuildingDefinitionLibrary.Get(repairBuildingId).displayName}修复材料";
            }

            if (IsSpecialStructure)
            {
                return "专用结构";
            }

            if (IsGenericCommonMaterial)
            {
                return "通用材料";
            }

            return ArchitecturalCrystalFactory.GetDisplayName(type);
        }
    }

    public static bool IsGenericCommonMaterialType(ArchitecturalType type)
    {
        return type == ArchitecturalType.Green ||
               type == ArchitecturalType.Gold ||
               type == ArchitecturalType.White;
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
        bool isUnlockMaterial = false,
        int buildProgressPercent = 0)
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
            0,
            buildProgressPercent)
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
        int inkRestoreValue,
        int buildProgressPercent = 0,
        CatalogueBuildingId repairBuildingId = CatalogueBuildingId.Building1)
        : this()
    {
        this.type = type;
        this.expValue = expValue;
        this.buildProgressPercent = buildProgressPercent;
        this.icon = icon;
        this.backIcon = backIcon;
        this.textDescription = textDescription;
        this.bonusType = bonusType;
        this.bonusValue = bonusValue;
        this.subBonusType = subBonusType;
        this.subBonusValue = subBonusValue;
        this.isUnlockMaterial = isUnlockMaterial;
        this.resourceCategory = resourceCategory;
        this.repairBuildingId = repairBuildingId;
        this.inkRestoreValue = inkRestoreValue;
        this.runtimePickupOrder = 0;
    }
}

public readonly struct CommonStructureCrystalDefinition
{
    public readonly AttributeBonusType bonusType;
    public readonly float bonusValue;
    public readonly AttributeBonusType subBonusType;
    public readonly float subBonusValue;
    public readonly string description;

    public CommonStructureCrystalDefinition(
        AttributeBonusType bonusType,
        float bonusValue,
        AttributeBonusType subBonusType,
        float subBonusValue,
        string description)
    {
        this.bonusType = bonusType;
        this.bonusValue = bonusValue;
        this.subBonusType = subBonusType;
        this.subBonusValue = subBonusValue;
        this.description = description;
    }
}

public readonly struct ArchitecturalCrystalVisualSet
{
    public readonly Sprite icon;
    public readonly Sprite backIcon;

    public ArchitecturalCrystalVisualSet(Sprite icon, Sprite backIcon)
    {
        this.icon = icon;
        this.backIcon = backIcon;
    }
}

public static class ArchitecturalCrystalFactory
{
    public const int MinimumBuildProgressPercent = 10;
    public const int MaximumBuildProgressPercent = 30;

    private static Sprite runtimeSpecialStructureSprite;
    private static readonly Dictionary<ArchitecturalType, CommonStructureCrystalDefinition> commonStructureDefinitions =
        new Dictionary<ArchitecturalType, CommonStructureCrystalDefinition>
        {
            {
                ArchitecturalType.MortiseAndTenonJoint,
                new CommonStructureCrystalDefinition(
                    AttributeBonusType.None,
                    0f,
                    AttributeBonusType.None,
                    0f,
                    "这是榫卯结构，通过凹凸咬合连接木材，无需钉子。它让建筑既稳固又灵活，是中国古建筑最重要的技艺之一。")
            },
            {
                ArchitecturalType.GroundMass,
                new CommonStructureCrystalDefinition(
                    AttributeBonusType.None,
                    0f,
                    AttributeBonusType.None,
                    0f,
                    "石基位于建筑底部，可以防潮、防腐，让建筑更加耐久。")
            },
            {
                ArchitecturalType.BeamFrame,
                new CommonStructureCrystalDefinition(
                    AttributeBonusType.None,
                    0f,
                    AttributeBonusType.None,
                    0f,
                    "梁架是建筑的骨架，由梁与柱共同构成，支撑起整个屋顶结构。")
            },
            {
                ArchitecturalType.TampedEarth,
                new CommonStructureCrystalDefinition(
                    AttributeBonusType.None,
                    0f,
                    AttributeBonusType.None,
                    0f,
                    "夯土是将土层反复压实形成地基的方法，简单却非常坚固，广泛用于古代建筑。")
            },
            {
                ArchitecturalType.Tile,
                new CommonStructureCrystalDefinition(
                    AttributeBonusType.None,
                    0f,
                    AttributeBonusType.None,
                    0f,
                    "瓦片覆盖在屋顶上，用来防水和保护内部结构，是最常见的屋面材料。")
            },
            {
                ArchitecturalType.Brackets,
                new CommonStructureCrystalDefinition(
                    AttributeBonusType.None,
                    0f,
                    AttributeBonusType.None,
                    0f,
                    "斗拱位于柱与屋顶之间，用来承托重量并分散压力。这种结构还能缓冲震动，使建筑更加稳固。")
            }
        };

    public static ArchitecturalCrystal CreateCommonStructure(
        ArchitecturalType type,
        Sprite icon = null,
        Sprite backIcon = null,
        int buildProgressPercent = 0)
    {
        CommonStructureCrystalDefinition definition = GetCommonStructureDefinition(type);
        ArchitecturalCrystalVisualSet visuals = ArchitecturalCrystalVisualResolver.Resolve(
            type,
            ArchitecturalResourceCategory.CommonStructure,
            icon,
            backIcon);
        int resolvedBuildProgressPercent = ResolveBuildProgressPercent(buildProgressPercent);
        return new ArchitecturalCrystal(
            type,
            0,
            visuals.icon,
            visuals.backIcon,
            definition.description,
            definition.bonusType,
            definition.bonusValue,
            definition.subBonusType,
            definition.subBonusValue,
            false,
            ArchitecturalResourceCategory.CommonStructure,
            0,
            resolvedBuildProgressPercent);
    }

    public static ArchitecturalCrystal CreateSpecialStructureMaterial(
        Sprite icon = null,
        Sprite backIcon = null)
    {
        ArchitecturalCrystalVisualSet visuals = ArchitecturalCrystalVisualResolver.Resolve(
            ArchitecturalType.MortiseAndTenonJoint,
            ArchitecturalResourceCategory.SpecialStructure,
            icon,
            backIcon);
        Sprite specialIcon = visuals.icon != null ? visuals.icon : GetOrCreateSpecialStructureSprite();
        Sprite specialBackIcon = visuals.backIcon != null ? visuals.backIcon : specialIcon;

        return new ArchitecturalCrystal(
            ArchitecturalType.MortiseAndTenonJoint,
            0,
            specialIcon,
            specialBackIcon,
            "专用结构，可用于点亮建筑录槽位。",
            AttributeBonusType.None,
            0f,
            AttributeBonusType.None,
            0f,
            true,
            ArchitecturalResourceCategory.SpecialStructure,
            0);
    }

    public static ArchitecturalCrystal CreateGenericCommonMaterial(
        Sprite icon = null,
        Sprite backIcon = null)
    {
        ArchitecturalCrystal crystal = CreateCommonStructure(
            ArchitecturalType.Green,
            icon,
            backIcon,
            MaximumBuildProgressPercent);
        crystal.textDescription = "通用材料，可带回基地提交到建筑录。";
        return crystal;
    }

    public static ArchitecturalCrystal CreateRepairMaterial(
        CatalogueBuildingId buildingId,
        Sprite icon = null,
        Sprite backIcon = null)
    {
        ArchitecturalCrystalVisualSet visuals = ArchitecturalCrystalVisualResolver.Resolve(
            ArchitecturalType.MortiseAndTenonJoint,
            ArchitecturalResourceCategory.RepairMaterial,
            icon,
            backIcon);
        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        Sprite repairIcon = visuals.icon != null ? visuals.icon : GetOrCreateSpecialStructureSprite();
        Sprite repairBackIcon = visuals.backIcon != null ? visuals.backIcon : repairIcon;

        return new ArchitecturalCrystal(
            ArchitecturalType.MortiseAndTenonJoint,
            0,
            repairIcon,
            repairBackIcon,
            $"{definition.displayName}修复材料，可带回对应关卡修复残破建筑。",
            AttributeBonusType.None,
            0f,
            AttributeBonusType.None,
            0f,
            false,
            ArchitecturalResourceCategory.RepairMaterial,
            0,
            0,
            buildingId);
    }

    private static Sprite GetOrCreateSpecialStructureSprite()
    {
        if (runtimeSpecialStructureSprite != null)
        {
            return runtimeSpecialStructureSprite;
        }

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color outline = new Color32(103, 63, 20, 255);
        Color shadow = new Color32(154, 105, 35, 255);
        Color fill = new Color32(238, 191, 90, 255);
        Color highlight = new Color32(255, 231, 156, 255);

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = 10.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center.x);
                float dy = Mathf.Abs(y - center.y);
                float manhattan = dx + dy * 1.18f;

                Color color = Color.clear;
                if (manhattan <= radius + 1.5f)
                {
                    color = outline;
                }

                if (manhattan <= radius)
                {
                    color = shadow;
                }

                if (manhattan <= radius - 1.8f)
                {
                    color = fill;
                }

                if (manhattan <= radius - 4.2f && y >= center.y - 5f)
                {
                    color = highlight;
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        runtimeSpecialStructureSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f);
        runtimeSpecialStructureSprite.name = "RuntimeSpecialStructureSprite";
        return runtimeSpecialStructureSprite;
    }

    public static ArchitecturalCrystal CreateInkSupply(
        bool largeBottle,
        Sprite icon = null,
        Sprite backIcon = null)
    {
        ArchitecturalType type = largeBottle ? ArchitecturalType.LargeInkBottle : ArchitecturalType.SmallInkBottle;
        ArchitecturalCrystalVisualSet visuals = ArchitecturalCrystalVisualResolver.Resolve(
            type,
            ArchitecturalResourceCategory.InkSupply,
            icon,
            backIcon);

        return new ArchitecturalCrystal(
            type,
            0,
            visuals.icon,
            visuals.backIcon,
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
                return "石基";
            case ArchitecturalType.BeamFrame:
                return "梁架";
            case ArchitecturalType.TampedEarth:
                return "夯土";
            case ArchitecturalType.Tile:
                return "瓦片";
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
                return "斗拱会追加攻击波次，最多连续发出 3 波。";
            case ArchitecturalType.MortiseAndTenonJoint:
                return "榫卯会让墨迹按扇形发射，最多形成 6 发齐射。";
            case ArchitecturalType.Tile:
                return "瓦片会增大墨迹体积，并降低攻击墨水消耗。";
            case ArchitecturalType.TampedEarth:
                return "夯土会同步提升墨迹射程与飞行速度。";
            case ArchitecturalType.GroundMass:
                return "石基会同步提升墨迹体积与伤害。";
            case ArchitecturalType.BeamFrame:
                return "梁架会提升攻击速度，最低攻击间隔为 0.4 秒。";
            default:
                return $"拾取 {GetDisplayName(type)} 后会立即生效。";
        }
    }

    public static CommonStructureCrystalDefinition GetCommonStructureDefinition(ArchitecturalType type)
    {
        if (commonStructureDefinitions.TryGetValue(type, out CommonStructureCrystalDefinition definition))
        {
            return definition;
        }

        return new CommonStructureCrystalDefinition(
            AttributeBonusType.None,
            0f,
            AttributeBonusType.None,
            0f,
            GetDefaultDescription(type));
    }

    public static int ClampBuildProgressPercent(int buildProgressPercent)
    {
        return Mathf.Clamp(buildProgressPercent, MinimumBuildProgressPercent, MaximumBuildProgressPercent);
    }

    private static int ResolveBuildProgressPercent(int buildProgressPercent)
    {
        if (buildProgressPercent > 0)
        {
            return ClampBuildProgressPercent(buildProgressPercent);
        }

        return Random.Range(MinimumBuildProgressPercent, MaximumBuildProgressPercent + 1);
    }
}

public static class ArchitecturalCrystalVisualResolver
{
    private readonly struct VisualConfig
    {
        public readonly string iconPath;
        public readonly string backIconPath;
        public readonly Color32 primaryColor;
        public readonly Color32 secondaryColor;

        public VisualConfig(string iconPath, string backIconPath, Color32 primaryColor, Color32 secondaryColor)
        {
            this.iconPath = iconPath;
            this.backIconPath = backIconPath;
            this.primaryColor = primaryColor;
            this.secondaryColor = secondaryColor;
        }
    }

    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private static readonly Dictionary<ArchitecturalType, VisualConfig> commonVisualConfigs =
        new Dictionary<ArchitecturalType, VisualConfig>
        {
            {
                ArchitecturalType.MortiseAndTenonJoint,
                new VisualConfig(
                    "Assets/File/Prop/Prop/MortiseandTenon.png",
                    "Assets/File/Prop/Prop/MortiseandTenonBackground.png",
                    new Color32(219, 177, 122, 255),
                    new Color32(108, 74, 45, 255))
            },
            {
                ArchitecturalType.GroundMass,
                new VisualConfig(
                    null,
                    "Assets/File/Prop/Prop/StonBaseBackground.png",
                    new Color32(170, 177, 187, 255),
                    new Color32(76, 85, 97, 255))
            },
            {
                ArchitecturalType.BeamFrame,
                new VisualConfig(
                    "Assets/File/Prop/Prop/BeamFramework.png",
                    "Assets/File/Prop/Prop/BeamFrameworkBackground.png",
                    new Color32(199, 143, 94, 255),
                    new Color32(88, 56, 34, 255))
            },
            {
                ArchitecturalType.TampedEarth,
                new VisualConfig(
                    "Assets/File/UIResources/RammedEarthUI.png",
                    "Assets/File/Prop/Prop/HangTuBackground.png",
                    new Color32(199, 112, 76, 255),
                    new Color32(102, 53, 40, 255))
            },
            {
                ArchitecturalType.Tile,
                new VisualConfig(
                    "Assets/File/Prop/Prop/RoofTile.png",
                    "Assets/File/Prop/Prop/RoofTileBackground.png",
                    new Color32(116, 139, 173, 255),
                    new Color32(56, 75, 104, 255))
            },
            {
                ArchitecturalType.Brackets,
                new VisualConfig(
                    null,
                    "Assets/File/Prop/Prop/DouGongBackground.png",
                    new Color32(183, 95, 77, 255),
                    new Color32(106, 49, 40, 255))
            }
        };

    private static readonly VisualConfig smallInkVisualConfig = new VisualConfig(
        null,
        null,
        new Color32(71, 154, 216, 255),
        new Color32(30, 76, 125, 255));
    private static readonly VisualConfig specialStructureVisualConfig = new VisualConfig(
        "Assets/File/Prop/Prop/ItemBag_2.png",
        "Assets/File/Prop/Prop/LightBall.png",
        new Color32(243, 199, 96, 255),
        new Color32(125, 83, 26, 255));
    private static readonly VisualConfig repairMaterialVisualConfig = new VisualConfig(
        "Assets/File/Prop/Prop/ItemBag_2.png",
        "Assets/File/Prop/Prop/LightBall.png",
        new Color32(105, 210, 170, 255),
        new Color32(31, 105, 86, 255));
    private static readonly VisualConfig largeInkVisualConfig = new VisualConfig(
        null,
        null,
        new Color32(89, 190, 236, 255),
        new Color32(31, 98, 142, 255));

    public static ArchitecturalCrystalVisualSet Resolve(
        ArchitecturalType type,
        ArchitecturalResourceCategory category,
        Sprite icon = null,
        Sprite backIcon = null)
    {
        Sprite resolvedIcon = icon;
        Sprite resolvedBackIcon = backIcon;

        if (category == ArchitecturalResourceCategory.CommonStructure)
        {
            VisualConfig config = GetCommonVisualConfig(type);
            resolvedIcon ??= ResolveSprite(
                type,
                category,
                config.iconPath,
                config.primaryColor,
                config.secondaryColor,
                false);
            resolvedBackIcon ??= ResolveSprite(
                type,
                category,
                config.backIconPath,
                config.primaryColor,
                config.secondaryColor,
                true);
        }
        else if (category == ArchitecturalResourceCategory.InkSupply)
        {
            VisualConfig config = type == ArchitecturalType.LargeInkBottle
                ? largeInkVisualConfig
                : smallInkVisualConfig;
            resolvedIcon ??= ResolveSprite(type, category, config.iconPath, config.primaryColor, config.secondaryColor, false);
            resolvedBackIcon ??= ResolveSprite(type, category, config.backIconPath, config.primaryColor, config.secondaryColor, true);
        }
        else if (category == ArchitecturalResourceCategory.SpecialStructure)
        {
            resolvedIcon ??= ResolveSprite(
                type,
                category,
                specialStructureVisualConfig.iconPath,
                specialStructureVisualConfig.primaryColor,
                specialStructureVisualConfig.secondaryColor,
                false);
            resolvedBackIcon ??= ResolveSprite(
                type,
                category,
                specialStructureVisualConfig.backIconPath,
                specialStructureVisualConfig.primaryColor,
                specialStructureVisualConfig.secondaryColor,
                true);
        }
        else if (category == ArchitecturalResourceCategory.RepairMaterial)
        {
            resolvedIcon ??= ResolveSprite(
                type,
                category,
                repairMaterialVisualConfig.iconPath,
                repairMaterialVisualConfig.primaryColor,
                repairMaterialVisualConfig.secondaryColor,
                false);
            resolvedBackIcon ??= ResolveSprite(
                type,
                category,
                repairMaterialVisualConfig.backIconPath,
                repairMaterialVisualConfig.primaryColor,
                repairMaterialVisualConfig.secondaryColor,
                true);
        }

        resolvedIcon ??= resolvedBackIcon;
        resolvedBackIcon ??= resolvedIcon;
        return new ArchitecturalCrystalVisualSet(resolvedIcon, resolvedBackIcon);
    }

    private static VisualConfig GetCommonVisualConfig(ArchitecturalType type)
    {
        if (commonVisualConfigs.TryGetValue(type, out VisualConfig config))
        {
            return config;
        }

        return new VisualConfig(
            null,
            null,
            new Color32(215, 215, 215, 255),
            new Color32(97, 97, 97, 255));
    }

    private static Sprite ResolveSprite(
        ArchitecturalType type,
        ArchitecturalResourceCategory category,
        string assetPath,
        Color32 primaryColor,
        Color32 secondaryColor,
        bool isBackgroundVariant)
    {
        string cacheKey = $"{category}_{type}_{(isBackgroundVariant ? "Back" : "Icon")}";
        if (spriteCache.TryGetValue(cacheKey, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite resolvedSprite = LoadProjectSprite(assetPath);
        if (resolvedSprite == null)
        {
            resolvedSprite = CreateFallbackSprite(cacheKey, primaryColor, secondaryColor, isBackgroundVariant);
        }

        spriteCache[cacheKey] = resolvedSprite;
        return resolvedSprite;
    }

    private static Sprite LoadProjectSprite(string assetPath)
    {
        return RuntimeProjectSpriteLoader.LoadSprite(assetPath, true);
    }

    private static Sprite CreateFallbackSprite(
        string cacheKey,
        Color32 primaryColor,
        Color32 secondaryColor,
        bool isBackgroundVariant)
    {
        const int size = 48;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float outerRadius = isBackgroundVariant ? 20.5f : 17.5f;
        float innerRadius = isBackgroundVariant ? 16.5f : 13.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center.x);
                float dy = Mathf.Abs(y - center.y);
                float diamondDistance = dx + dy * 1.08f;
                Color color = Color.clear;

                if (diamondDistance <= outerRadius)
                {
                    color = secondaryColor;
                }

                if (diamondDistance <= innerRadius)
                {
                    color = primaryColor;
                }

                if (!isBackgroundVariant && Mathf.Abs(dx - dy) <= 1.6f && diamondDistance <= innerRadius - 1.8f)
                {
                    color = Color.Lerp(primaryColor, Color.white, 0.26f);
                }

                if (isBackgroundVariant && diamondDistance <= innerRadius - 4f)
                {
                    color = Color.Lerp(primaryColor, Color.white, 0.14f);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        texture.name = $"RuntimeCrystalVisual_{cacheKey}";
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = texture.name;
        return sprite;
    }
}
