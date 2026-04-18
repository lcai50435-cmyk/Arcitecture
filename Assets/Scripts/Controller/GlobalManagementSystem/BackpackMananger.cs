using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 专门存储背包物品数据的核心类
/// 固定6格，不再使用 RemoveAt 左移
/// </summary>
public class BackpackMananger : MonoBehaviour
{
    public static BackpackMananger Instance;

    [HideInInspector]
    public List<ArchitecturalCrystal> backpackItems = new List<ArchitecturalCrystal>();

    private int maxCapacity = 6;
    private HashSet<ArchitecturalType> alreadyPickedTypes = new HashSet<ArchitecturalType>(); // 是否第一次捡到物品

    public delegate void FirstPickTipEvent(ArchitecturalCrystal crystal);
    public event FirstPickTipEvent OnFirstTimePickItemType;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化固定6格
            while (backpackItems.Count < maxCapacity)
            {
                backpackItems.Add(null);
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
            if (backpackItems[i] != null)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 拾取物品加入背包（放入第一个空格）
    /// </summary>
    public bool PickItem(ArchitecturalCrystal crystal)
    {
        int emptyIndex = -1;

        for (int i = 0; i < backpackItems.Count; i++)
        {
            if (backpackItems[i] == null)
            {
                emptyIndex = i;
                break;
            }
        }

        if (emptyIndex == -1)
        {
            Debug.LogWarning("背包已满，无法拾取！");
            return false;
        }

        bool isFirstPick = !alreadyPickedTypes.Contains(crystal.type);

        if (isFirstPick)
        {
            alreadyPickedTypes.Add(crystal.type);
            Debug.Log("玩家第一次捡起该物品");
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
            crystal.isUnlockMaterial
        );

        backpackItems[emptyIndex] = newItem;

        // 拾取道具时，根据格子索引添加属性加成
        if (PlayerAttributeManager.Instance != null)
        {
            PlayerAttributeManager.Instance.AddBonus(
                newItem.bonusType,
                newItem.bonusValue
            );
        }

        Debug.Log($"拾取{newItem.type}，放入背包格子：{emptyIndex}");
        return true;
    }

    /// <summary>
    /// 删除指定索引的物品（改为置空，不左移）
    /// </summary>
    public void RemoveItem(int index)
    {
        if (index >= 0 && index < backpackItems.Count)
        {
            // 移除道具时扣除相应属性加成
            if (PlayerAttributeManager.Instance != null)
            {
                PlayerAttributeManager.Instance.RemoveBonus(backpackItems[index].bonusType, backpackItems[index].bonusValue);
            }

            backpackItems[index] = null;
            Debug.Log($"清空第{index}个格子物品");
        }
    }

    /// <summary>
    /// 获取指定索引的物品
    /// </summary>
    public ArchitecturalCrystal GetItem(int index)
    {
        if (index >= 0 && index < backpackItems.Count)
        {
            return backpackItems[index];
        }
        return null;
    }

    /// <summary>
    /// 清空所有物品
    /// </summary>
    public void ClearAllItems()
    {
        for (int i = 0; i < backpackItems.Count; i++)
        {
            backpackItems[i] = null;
        }
        Debug.Log("背包已清空");
    }
}