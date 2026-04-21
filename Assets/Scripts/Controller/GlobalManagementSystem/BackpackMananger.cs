using System.Collections.Generic;
using UnityEngine;

public class BackpackMananger : MonoBehaviour
{
    public static BackpackMananger Instance;

    [HideInInspector]
    public List<ArchitecturalCrystal?> backpackItems = new List<ArchitecturalCrystal?>();

    private const int MaxCapacity = 6;
    private readonly HashSet<ArchitecturalType> alreadyPickedCommonTypes = new HashSet<ArchitecturalType>();

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

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        EnsureCapacity();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

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

    public bool PickItem(ArchitecturalCrystal crystal)
    {
        EnsureCapacity();

        if (crystal.IsInkSupply)
        {
            ApplyInkSupply(crystal);
            Debug.Log($"拾取 {crystal.DisplayName}，恢复墨笔耐久 {crystal.inkRestoreValue}");
            return true;
        }

        int emptyIndex = FindEmptyIndex();
        if (emptyIndex == -1)
        {
            Debug.LogWarning("背包已满，无法拾取");
            return false;
        }

        bool shouldShowFirstPick = crystal.IsCommonStructure && !alreadyPickedCommonTypes.Contains(crystal.type);
        if (shouldShowFirstPick)
        {
            alreadyPickedCommonTypes.Add(crystal.type);
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
            crystal.isUnlockMaterial,
            crystal.resourceCategory,
            crystal.inkRestoreValue);

        backpackItems[emptyIndex] = newItem;

        if (shouldShowFirstPick)
        {
            OnFirstTimePickItemType?.Invoke(newItem);
        }

        Debug.Log($"拾取 {newItem.DisplayName}，放入背包格子 {emptyIndex}");
        return true;
    }

    public void RemoveItem(int index)
    {
        if (index < 0 || index >= backpackItems.Count) return;
        if (!backpackItems[index].HasValue) return;

        backpackItems[index] = null;
        Debug.Log($"清空第 {index} 个背包物品");
    }

    public ArchitecturalCrystal? GetItem(int index)
    {
        if (index >= 0 && index < backpackItems.Count)
        {
            return backpackItems[index];
        }

        return null;
    }

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

    private int FindEmptyIndex()
    {
        for (int i = 0; i < backpackItems.Count; i++)
        {
            if (!backpackItems[i].HasValue)
            {
                return i;
            }
        }

        return -1;
    }

    private void ApplyInkSupply(ArchitecturalCrystal crystal)
    {
        PlayerAttack playerAttack = FindObjectOfType<PlayerAttack>();
        if (playerAttack == null)
        {
            Debug.LogWarning("拾取墨水补给时未找到 PlayerAttack，未执行恢复");
            return;
        }

        playerAttack.AddInk(crystal.inkRestoreValue);
    }
}
