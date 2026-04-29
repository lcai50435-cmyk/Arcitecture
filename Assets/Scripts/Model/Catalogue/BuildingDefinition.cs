using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BuildingRewardDefinition
{
    public string rewardId;
    public string title;
    [TextArea(2, 6)] public string description;
    public bool unlocksWeapon;
    public WeaponType unlockedWeaponType = WeaponType.DirectInk;
    public AttributeBonusType bonusType = AttributeBonusType.None;
    public float bonusValue;
    public AttributeBonusType subBonusType = AttributeBonusType.None;
    public float subBonusValue;
}

[Serializable]
public class BuildingSlotDefinition
{
    public string slotId;
    public string slotName;
    [TextArea(2, 6)] public string description;
    public string iconAssetPath;
    public BuildingRewardDefinition reward;
}

[Serializable]
public class BuildingDefinition
{
    public CatalogueBuildingId buildingId;
    public string displayName;
    public int requiredProgress = 100;
    public string detailTitle;
    [TextArea(3, 8)] public string detailDescription;
    public BuildingSlotDefinition[] slotDefinitions;
    public BuildingRewardDefinition completionReward;
}

public static class BuildingDefinitionLibrary
{
    private static readonly Dictionary<CatalogueBuildingId, BuildingDefinition> definitions =
        new Dictionary<CatalogueBuildingId, BuildingDefinition>
        {
            {
                CatalogueBuildingId.Building1,
                new BuildingDefinition
                {
                    buildingId = CatalogueBuildingId.Building1,
                    displayName = "福建土楼",
                    detailTitle = "福建土楼",
                    detailDescription = "福建土楼以夯土围合成巨型聚落，兼具居住、防御与宗族凝聚功能，是中国南方山地聚居智慧的代表。",
                    slotDefinitions = new[]
                    {
                        CreateSlot(
                            "fujian_tulou_slot_1",
                            "围合防御",
                            "圆楼与方楼形成整体防御体系，让建筑能在动荡环境中维持稳定秩序。",
                            "Assets/File/UIResources/RammedEarthUI.png",
                            CreateReward("fujian_tulou_small_1", "土楼小奖励", "生命上限提升 10。", AttributeBonusType.MaxHealth, 10f)),
                        CreateSlot(
                            "fujian_tulou_slot_2",
                            "聚族而居",
                            "土楼内部以公共空间连接各房支系，强调协作、共居与守望相助。",
                            "Assets/File/UIResources/ThickWallUI.png",
                            CreateReward("fujian_tulou_small_2", "土楼小奖励", "防御提升 2。", AttributeBonusType.Defense, 2f)),
                        CreateSlot(
                            "fujian_tulou_slot_3",
                            "夯土营造",
                            "厚重夯土墙兼具隔热、承重与耐久能力，是土楼长期使用的基础。",
                            "Assets/File/UIResources/TimberworkUI.png",
                            CreateReward("fujian_tulou_small_3", "土楼小奖励", "墨笔耐久上限提升 10。", AttributeBonusType.Durability, 10f))
                    },
                    completionReward = CreateWeaponUnlockReward(
                        "fujian_tulou_big",
                        "土楼大奖励",
                        "解锁福建土楼完整条目，并永久解锁爆墨基型。",
                        WeaponType.BurstInk)
                }
            },
            {
                CatalogueBuildingId.Building2,
                new BuildingDefinition
                {
                    buildingId = CatalogueBuildingId.Building2,
                    displayName = "赵州桥",
                    detailTitle = "赵州桥",
                    detailDescription = "赵州桥以敞肩石拱减轻桥体自重、分散受力，是世界桥梁史上极具代表性的早期敞肩拱桥。",
                    slotDefinitions = new[]
                    {
                        CreateSlot(
                            "zhaozhou_bridge_slot_1",
                            "敞肩券洞",
                            "主拱两侧设置小拱，既减轻桥身重量，也帮助洪水快速通过。",
                            "Assets/File/UIResources/SingleSpan.png",
                            CreateReward("zhaozhou_bridge_small_1", "赵州桥小奖励", "攻击提升 4。", AttributeBonusType.AttackPower, 4f)),
                        CreateSlot(
                            "zhaozhou_bridge_slot_2",
                            "弧线受力",
                            "石桥将竖向压力沿弧线传导到桥台，体现精确的结构受力设计。",
                            "Assets/File/UIResources/SmallArch.png",
                            CreateReward("zhaozhou_bridge_small_2", "赵州桥小奖励", "移动速度提升 0.25。", AttributeBonusType.MoveSpeed, 0.25f)),
                        CreateSlot(
                            "zhaozhou_bridge_slot_3",
                            "千年跨河",
                            "桥体在交通与自然冲击间长期服役，体现古代工匠对材料与结构的把控。",
                            "Assets/File/UIResources/VoussoirConstruction.png",
                            CreateReward("zhaozhou_bridge_small_3", "赵州桥小奖励", "墨笔耐久上限提升 10。", AttributeBonusType.Durability, 10f))
                    },
                    completionReward = CreateWeaponUnlockReward(
                        "zhaozhou_bridge_big",
                        "赵州桥大奖励",
                        "解锁赵州桥完整条目，并永久解锁贯墨基型。",
                        WeaponType.PierceInk)
                }
            },
            {
                CatalogueBuildingId.Building3,
                new BuildingDefinition
                {
                    buildingId = CatalogueBuildingId.Building3,
                    displayName = "安徽水乡民居",
                    detailTitle = "安徽水乡民居",
                    detailDescription = "安徽水乡民居以白墙黛瓦、临水街巷和天井采光著称，在自然水系与日常生活间形成高度协调的空间秩序。",
                    slotDefinitions = new[]
                    {
                        CreateSlot(
                            "anhui_water_town_slot_1",
                            "临水布局",
                            "依水而建的街巷与民居组织，让生活、运输与防洪形成稳定平衡。",
                            "Assets/File/UIResources/ShuiXiang.png",
                            CreateReward("anhui_water_town_small_1", "水乡小奖励", "防御提升 2。", AttributeBonusType.Defense, 2f)),
                        CreateSlot(
                            "anhui_water_town_slot_2",
                            "白墙黛瓦",
                            "屋面与墙体形成鲜明对比，同时兼顾排水、防潮与识别性。",
                            "Assets/File/Prop/Prop/RoofTile.png",
                            CreateReward("anhui_water_town_small_2", "水乡小奖励", "墨笔耐久上限提升 10。", AttributeBonusType.Durability, 10f)),
                        CreateSlot(
                            "anhui_water_town_slot_3",
                            "天井采光",
                            "通过天井组织通风、采光与雨水回收，是民居空间智慧的核心节点。",
                            "Assets/File/TileMap/FirstPass/AnhuiWaterTowns_1.png",
                            CreateReward("anhui_water_town_small_3", "水乡小奖励", "生命上限提升 10。", AttributeBonusType.MaxHealth, 10f))
                    },
                    completionReward = CreateWeaponUnlockReward(
                        "anhui_water_town_big",
                        "水乡大奖励",
                        "解锁安徽水乡民居完整条目，并永久解锁流墨基型。",
                        WeaponType.FlowInk)
                }
            }
        };

    public static BuildingDefinition Get(CatalogueBuildingId buildingId)
    {
        return definitions[buildingId];
    }

    public static IEnumerable<BuildingDefinition> GetAll()
    {
        return definitions.Values;
    }

    private static BuildingSlotDefinition CreateSlot(
        string slotId,
        string slotName,
        string description,
        string iconAssetPath,
        BuildingRewardDefinition reward)
    {
        return new BuildingSlotDefinition
        {
            slotId = slotId,
            slotName = slotName,
            description = description,
            iconAssetPath = iconAssetPath,
            reward = reward
        };
    }

    private static BuildingRewardDefinition CreateReward(
        string rewardId,
        string title,
        string description,
        AttributeBonusType bonusType,
        float bonusValue,
        AttributeBonusType subBonusType = AttributeBonusType.None,
        float subBonusValue = 0f)
    {
        return new BuildingRewardDefinition
        {
            rewardId = rewardId,
            title = title,
            description = description,
            bonusType = bonusType,
            bonusValue = bonusValue,
            subBonusType = subBonusType,
            subBonusValue = subBonusValue
        };
    }

    private static BuildingRewardDefinition CreateWeaponUnlockReward(
        string rewardId,
        string title,
        string description,
        WeaponType weaponType)
    {
        return new BuildingRewardDefinition
        {
            rewardId = rewardId,
            title = title,
            description = description,
            unlocksWeapon = true,
            unlockedWeaponType = weaponType
        };
    }
}
