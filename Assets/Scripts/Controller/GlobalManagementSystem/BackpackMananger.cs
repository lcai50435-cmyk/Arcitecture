using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 专门存储背包物品数据的管理器。
/// 固定 6 格，禁止使用 RemoveAt 改变槽位结构。
/// </summary>
public class BackpackMananger : MonoBehaviour
{
    public static BackpackMananger Instance;

    [HideInInspector]
    public List<ArchitecturalCrystal?> backpackItems = new List<ArchitecturalCrystal?>();

    private const int MaxCapacity = 6;
    private readonly HashSet<ArchitecturalType> alreadyPickedTypes = new HashSet<ArchitecturalType>();

    public delegate void FirstPickTipEvent(ArchitecturalCrystal crystal);
    public event FirstPickTipEvent OnFirstTimePickItemType;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureCapacity();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 当前实际占用的背包格数。
    /// </summary>
    public int GetOccupiedCount()
    {
        int count = 0;
        for (int i = 0; i < backpackItems.Count; i++)
        {
            if (backpackItems[i].HasValue)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 拾取道具并放入背包的第一个空位。
    /// </summary>
    public bool PickItem(ArchitecturalCrystal crystal)
    {
        EnsureCapacity();

        int emptyIndex = -1;
        for (int i = 0; i < backpackItems.Count; i++)
        {
            if (!backpackItems[i].HasValue)
            {
                emptyIndex = i;
                break;
            }
        }

        if (emptyIndex == -1)
        {
            Debug.LogWarning("背包已满，无法拾取");
            return false;
        }

        bool isFirstPick = !alreadyPickedTypes.Contains(crystal.type);
        if (isFirstPick)
        {
            alreadyPickedTypes.Add(crystal.type);
            Debug.Log($"第一次拾取 {crystal.type} 结构道具");
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

        backpackItems[emptyIndex] = newItem;

        if (PlayerAttributeManager.Instance != null)
        {
            PlayerAttributeManager.Instance.AddBonus(
                newItem.bonusType,
                newItem.bonusValue,
                newItem.subBonusType,
                newItem.subBonusValue
            );
        }

        Debug.Log($"拾取 {newItem.type}，放入背包格子 {emptyIndex}");
        return true;
    }

    /// <summary>
    /// 清空指定槽位中的道具，但不移除槽位本身。
    /// </summary>
    public void RemoveItem(int index)
    {
        if (index < 0 || index >= backpackItems.Count)
        {
            return;
        }

        if (!backpackItems[index].HasValue)
        {
            return;
        }

        ArchitecturalCrystal item = backpackItems[index].Value;
        if (PlayerAttributeManager.Instance != null)
        {
            PlayerAttributeManager.Instance.RemoveBonus(
                item.bonusType,
                item.bonusValue,
                item.subBonusType,
                item.subBonusValue
            );
        }

        backpackItems[index] = null;
        Debug.Log($"清空第 {index} 个背包物品");
    }

    /// <summary>
    /// 获取指定槽位中的道具；空槽位返回 null。
    /// </summary>
    public ArchitecturalCrystal? GetItem(int index)
    {
        if (index >= 0 && index < backpackItems.Count)
        {
            return backpackItems[index];
        }

        return null;
    }

    /// <summary>
    /// 清空所有背包内容。
    /// </summary>
    public void ClearAllItems()
    {
        for (int i = 0; i < backpackItems.Count; i++)
        {
            RemoveItem(i);
        }

        Debug.Log("背包已清空");
    }

    private void EnsureCapacity()
    {
        while (backpackItems.Count < MaxCapacity)
        {
            backpackItems.Add(null);
        }
    }
}
