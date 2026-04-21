using System.Collections.Generic;
using UnityEngine;

public class BackpackMananger : MonoBehaviour
{
    public static BackpackMananger Instance;

    [HideInInspector]
    public List<ArchitecturalCrystal?> backpackItems = new List<ArchitecturalCrystal?>();

    private const int MaxCapacity = 6;
    private readonly HashSet<ArchitecturalType> alreadyPickedCommonTypes = new HashSet<ArchitecturalType>();
    private readonly HashSet<int> reservedSlots = new HashSet<int>();
    private int nextRuntimePickupOrder = 1;

    public delegate void FirstPickTipEvent(ArchitecturalCrystal crystal);
    public event FirstPickTipEvent OnFirstTimePickItemType;
    public event System.Action<ArchitecturalCrystal> OnItemPicked;
    public event System.Action OnInventoryChanged;

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

        if (TryHandleImmediatePickup(crystal, out bool immediatePickupSucceeded))
        {
            return immediatePickupSucceeded;
        }

        int emptyIndex = FindEmptyIndex();
        if (emptyIndex == -1)
        {
            Debug.LogWarning("背包已满，无法拾取");
            return false;
        }

        return StoreBackpackItem(crystal, emptyIndex);
    }

    public bool TryReserveSlotForPickup(ArchitecturalCrystal crystal, out int slotIndex)
    {
        EnsureCapacity();
        slotIndex = -1;

        if (!crystal.IsCommonStructure)
        {
            return false;
        }

        slotIndex = FindEmptyIndex();
        if (slotIndex == -1)
        {
            return false;
        }

        reservedSlots.Add(slotIndex);
        return true;
    }

    public void CancelReservedSlot(int slotIndex)
    {
        if (slotIndex < 0)
        {
            return;
        }

        reservedSlots.Remove(slotIndex);
    }

    public bool CommitReservedPickup(ArchitecturalCrystal crystal, int slotIndex)
    {
        EnsureCapacity();

        if (!reservedSlots.Remove(slotIndex))
        {
            return false;
        }

        if (slotIndex < 0 || slotIndex >= backpackItems.Count || backpackItems[slotIndex].HasValue)
        {
            return false;
        }

        return StoreBackpackItem(crystal, slotIndex);
    }

    public void RemoveItem(int index)
    {
        if (index < 0 || index >= backpackItems.Count) return;
        if (!backpackItems[index].HasValue) return;

        backpackItems[index] = null;
        RefreshPlayerTemporaryAttributes();
        OnInventoryChanged?.Invoke();
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
        bool removedAnyItem = false;
        for (int i = 0; i < backpackItems.Count; i++)
        {
            if (!backpackItems[i].HasValue)
            {
                continue;
            }

            backpackItems[i] = null;
            removedAnyItem = true;
        }

        if (removedAnyItem)
        {
            RefreshPlayerTemporaryAttributes();
            OnInventoryChanged?.Invoke();
        }

        reservedSlots.Clear();

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
            if (!backpackItems[i].HasValue && !reservedSlots.Contains(i))
            {
                return i;
            }
        }

        return -1;
    }

    private bool TryHandleImmediatePickup(ArchitecturalCrystal crystal, out bool success)
    {
        if (crystal.IsInkSupply)
        {
            ApplyInkSupply(crystal);
            Debug.Log($"拾取 {crystal.DisplayName}，恢复墨笔耐久 {crystal.inkRestoreValue}");
            success = true;
            return true;
        }

        if (crystal.IsSpecialStructure)
        {
            RuntimeProgressState.EnsureInstance().AddSpecialStructureInventory(1);
            OnInventoryChanged?.Invoke();
            Debug.Log($"拾取 {crystal.DisplayName}，已加入专用材料库存");
            success = true;
            return true;
        }

        success = false;
        return false;
    }

    private bool StoreBackpackItem(ArchitecturalCrystal crystal, int slotIndex)
    {
        bool shouldShowFirstPick = crystal.IsCommonStructure && !alreadyPickedCommonTypes.Contains(crystal.type);
        if (shouldShowFirstPick)
        {
            alreadyPickedCommonTypes.Add(crystal.type);
        }

        ArchitecturalCrystal storedItem = CreateStoredBackpackItem(crystal);
        backpackItems[slotIndex] = storedItem;
        RefreshPlayerTemporaryAttributes();

        if (shouldShowFirstPick)
        {
            OnFirstTimePickItemType?.Invoke(storedItem);
        }

        OnItemPicked?.Invoke(storedItem);
        OnInventoryChanged?.Invoke();

        Debug.Log($"拾取 {storedItem.DisplayName}，放入背包格子 {slotIndex}");
        return true;
    }

    private ArchitecturalCrystal CreateStoredBackpackItem(ArchitecturalCrystal crystal)
    {
        ArchitecturalCrystal storedItem = new ArchitecturalCrystal(
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
        storedItem.runtimePickupOrder = nextRuntimePickupOrder++;
        return storedItem;
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

    private void RefreshPlayerTemporaryAttributes()
    {
        PlayerAttributeManager attributeManager = PlayerAttributeManager.Instance != null
            ? PlayerAttributeManager.Instance
            : FindObjectOfType<PlayerAttributeManager>();

        if (attributeManager != null)
        {
            attributeManager.ApplyAllBonus();
        }
    }
}
