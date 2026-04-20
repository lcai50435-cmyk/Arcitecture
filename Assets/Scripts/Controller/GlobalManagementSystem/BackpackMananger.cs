using System.Collections.Generic;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

/// <summary>
/// 管理存储建筑道具数据的背包
/// 固定6格，禁止使用 RemoveAt 方法
/// </summary>
public class BackpackMananger : MonoBehaviour
{
    public static BackpackMananger Instance;

    [HideInInspector]
    // 关键修改：改为可空结构体类型
    public List<ArchitecturalCrystal?> backpackItems = new List<ArchitecturalCrystal?>();

    private int maxCapacity = 6;
    private HashSet<ArchitecturalType> alreadyPickedTypes = new HashSet<ArchitecturalType>(); // 是否第一次拾取该道具

    public delegate void FirstPickTipEvent(ArchitecturalCrystal crystal);
    public event FirstPickTipEvent OnFirstTimePickItemType;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化固定6格（初始值为 null）
            while (backpackItems.Count < maxCapacity)
            {
                backpackItems.Add(null); // 可空结构体支持 null 赋值
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 当前实际占用数量
    /// </summary>
    public int GetOccupiedCount()
    {
        int count = 0;
        for (int i = 0; i < backpackItems.Count; i++)
        {
            // 关键修改：判断可空类型是否有值
            if (backpackItems[i].HasValue)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 拾取道具并放入背包第一个空位置
    /// </summary>
    public bool PickItem(ArchitecturalCrystal crystal)
    {
        int emptyIndex = -1;

        for (int i = 0; i < backpackItems.Count; i++)
        {
            // 关键修改：判断是否为 null（空位置）
            if (backpackItems[i] == null)
            {
                emptyIndex = i;
                break;
            }
        }

        if (emptyIndex == -1)
        {
            Debug.LogWarning("背包已满无法拾取");
            return false;
        }

        bool isFirstPick = !alreadyPickedTypes.Contains(crystal.type);

        if (isFirstPick)
        {
            alreadyPickedTypes.Add(crystal.type);
            Debug.Log($"第一次拾取{crystal.type}道具");
            OnFirstTimePickItemType?.Invoke(crystal);
        }

        ArchitecturalCrystal newItem = new ArchitecturalCrystal(
            crystal.type,
            crystal.expValue,
            crystal.icon,
            crystal.backIcon,
            crystal.textDescription,
            crystal.bonusType,
            crystal.bonusValue,
            crystal.subBonusType,
            crystal.subBonusValue,
            crystal.isUnlockMaterial
        );

        backpackItems[emptyIndex] = newItem; // 可空类型接收结构体值

        // 拾取道具时立即添加对应的属性加成
        if (PlayerAttributeManager.Instance != null)
        {
            PlayerAttributeManager.Instance.AddBonus(
                newItem.bonusType,
                newItem.bonusValue,
                newItem.subBonusType,
                newItem.subBonusValue
            );
        }

        Debug.Log($"拾取{newItem.type}放入背包位置：{emptyIndex}");
        return true;
    }

    /// <summary>
    /// 删除指定索引位置的道具（置为空，不是移除）
    /// </summary>
    public void RemoveItem(int index)
    {
        if (index >= 0 && index < backpackItems.Count)
        {
            // 关键修改：先判断是否有值，再移除属性加成
            if (backpackItems[index].HasValue && PlayerAttributeManager.Instance != null)
            {
                var item = backpackItems[index].Value; // 取可空类型的实际值
                PlayerAttributeManager.Instance.RemoveBonus(
                    item.bonusType,
                    item.bonusValue,
                    item.subBonusType,
                    item.subBonusValue);
            }

            backpackItems[index] = null; // 置为 null 标记空位置
            Debug.Log($"移除了{index}位置的道具");
        }
    }

    /// <summary>
    /// 获取指定索引位置的道具
    /// </summary>
    /// <returns>有值则返回结构体，无值返回 null</returns>
    public ArchitecturalCrystal? GetItem(int index)
    {
        if (index >= 0 && index < backpackItems.Count)
        {
            return backpackItems[index]; // 直接返回可空类型
        }
        return null;
    }

    /// <summary>
    /// 清空所有道具
    /// </summary>
    public void ClearAllItems()
    {
        for (int i = 0; i < backpackItems.Count; i++)
        {
            // 清空时也要移除属性加成
            if (backpackItems[i].HasValue && PlayerAttributeManager.Instance != null)
            {
                var item = backpackItems[i].Value;
                PlayerAttributeManager.Instance.RemoveBonus(
                    item.bonusType,
                    item.bonusValue,
                    item.subBonusType,
                    item.subBonusValue);
            }
            backpackItems[i] = null;
        }
        Debug.Log("背包已清空");
    }
}