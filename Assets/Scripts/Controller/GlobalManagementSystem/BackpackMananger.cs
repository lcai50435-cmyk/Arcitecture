using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackpackMananger : MonoBehaviour
{
    public static BackpackMananger Instance;

    [HideInInspector]
    public List<ArchitecturalCrystal?> backpackItems = new List<ArchitecturalCrystal?>();

    private const int MaxCapacity = 6;
    public const int MaxCommonMaterialCount = 3;
    public const int MaxSpecialStructureMaterialCount = 3;
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

    public int GetCommonMaterialCount()
    {
        EnsureCapacity();

        int count = 0;
        for (int i = 0; i < backpackItems.Count; i++)
        {
            ArchitecturalCrystal? item = backpackItems[i];
            if (item.HasValue && item.Value.IsGenericCommonMaterial)
            {
                count++;
            }
        }

        return count;
    }

    public int GetSpecialStructureMaterialCount()
    {
        EnsureCapacity();

        int count = 0;
        for (int i = 0; i < backpackItems.Count; i++)
        {
            ArchitecturalCrystal? item = backpackItems[i];
            if (item.HasValue && item.Value.IsSpecialStructure)
            {
                count++;
            }
        }

        return count;
    }

    public bool PickItem(ArchitecturalCrystal crystal)
    {
        EnsureCapacity();

        if (!CanStoreCommonMaterial(crystal))
        {
            Debug.LogWarning($"通用材料已达上限 {MaxCommonMaterialCount}，无法继续添加");
            return false;
        }

        if (!CanStoreSpecialStructureMaterial(crystal))
        {
            Debug.LogWarning($"专用结构已达关卡上限 {MaxSpecialStructureMaterialCount}，无法继续添加");
            return false;
        }

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

        if (!crystal.IsCommonStructure && !crystal.IsSpecialStructure && !crystal.IsRepairMaterial)
        {
            return false;
        }

        if (!CanStoreCommonMaterial(crystal) || !CanStoreSpecialStructureMaterial(crystal))
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

        if (!CanStoreCommonMaterial(crystal))
        {
            return false;
        }

        if (!CanStoreSpecialStructureMaterial(crystal))
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

    public bool HasRepairMaterial(CatalogueBuildingId buildingId)
    {
        EnsureCapacity();

        for (int i = 0; i < backpackItems.Count; i++)
        {
            ArchitecturalCrystal? item = backpackItems[i];
            if (item.HasValue && item.Value.IsRepairMaterial && item.Value.repairBuildingId == buildingId)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryConsumeRepairMaterial(CatalogueBuildingId buildingId)
    {
        EnsureCapacity();

        for (int i = 0; i < backpackItems.Count; i++)
        {
            ArchitecturalCrystal? item = backpackItems[i];
            if (!item.HasValue || !item.Value.IsRepairMaterial || item.Value.repairBuildingId != buildingId)
            {
                continue;
            }

            backpackItems[i] = null;
            RefreshPlayerTemporaryAttributes();
            OnInventoryChanged?.Invoke();
            Debug.Log($"消耗 {item.Value.DisplayName}，背包格子 {i} 已清空");
            return true;
        }

        return false;
    }

    public bool TryConsumeSpecialStructureMaterial(int index)
    {
        EnsureCapacity();

        if (index < 0 || index >= backpackItems.Count)
        {
            return false;
        }

        ArchitecturalCrystal? item = backpackItems[index];
        if (!item.HasValue || !item.Value.IsSpecialStructure)
        {
            return false;
        }

        backpackItems[index] = null;
        RefreshPlayerTemporaryAttributes();
        OnInventoryChanged?.Invoke();
        Debug.Log($"消耗 {item.Value.DisplayName}，背包格子 {index} 已清空");
        return true;
    }

    public bool TryConsumeFirstSpecialStructureMaterial(out int consumedIndex)
    {
        EnsureCapacity();
        consumedIndex = -1;

        for (int i = 0; i < backpackItems.Count; i++)
        {
            if (TryConsumeSpecialStructureMaterial(i))
            {
                consumedIndex = i;
                return true;
            }
        }

        return false;
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

    private bool CanStoreCommonMaterial(ArchitecturalCrystal crystal)
    {
        return !crystal.IsGenericCommonMaterial || GetCommonMaterialCount() < MaxCommonMaterialCount;
    }

    private bool CanStoreSpecialStructureMaterial(ArchitecturalCrystal crystal)
    {
        return !crystal.IsSpecialStructure || GetSpecialStructureMaterialCount() < MaxSpecialStructureMaterialCount;
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
            OnItemPicked?.Invoke(crystal);
            Debug.Log($"拾取 {crystal.DisplayName}，恢复墨笔耐久 {crystal.inkRestoreValue}");
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
            EnsureFirstPickTipListener();
            OnFirstTimePickItemType?.Invoke(storedItem);
        }

        OnItemPicked?.Invoke(storedItem);
        OnInventoryChanged?.Invoke();

        Debug.Log($"拾取 {storedItem.DisplayName}，放入背包格子 {slotIndex}");
        return true;
    }

    private void EnsureFirstPickTipListener()
    {
        if (!GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        Dialog.EnsureGameplayRuntimeInstance();
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
            crystal.inkRestoreValue,
            crystal.buildProgressPercent,
            crystal.repairBuildingId);
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
