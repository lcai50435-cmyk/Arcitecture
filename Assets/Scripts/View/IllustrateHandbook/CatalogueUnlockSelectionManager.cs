using UnityEngine;

/// <summary>
/// 图鉴解锁选择管理器
/// 负责维护专用结构材料库存显示。
/// </summary>
public class CatalogueUnlockSelectionManager : MonoBehaviour
{
    public static CatalogueUnlockSelectionManager Instance;

    [Header("当前可用专用结构材料（运行时观察）")]
    public int availableUnlockCount = 0;

    public static CatalogueUnlockSelectionManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        CatalogueUnlockSelectionManager existing = FindObjectOfType<CatalogueUnlockSelectionManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject runtimeObject = new GameObject("CatalogueUnlockSelectionManager");
        return runtimeObject.AddComponent<CatalogueUnlockSelectionManager>();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        RuntimeProgressState.EnsureInstance().OnStateChanged += RefreshInventoryValue;
        RefreshInventoryValue();
    }

    private void OnDisable()
    {
        if (RuntimeProgressState.Instance != null)
        {
            RuntimeProgressState.Instance.OnStateChanged -= RefreshInventoryValue;
        }
    }

    public void AddUnlockCount(int count)
    {
        RuntimeProgressState.EnsureInstance().AddSpecialStructureInventory(count);
        RefreshInventoryValue();
    }

    public bool TryUnlockSlot(string slotId)
    {
        if (!TryResolveSlotContext(slotId, out CatalogueBuildingId buildingId, out int slotIndex))
        {
            return false;
        }

        bool success = RuntimeProgressState.EnsureInstance()
            .TryUnlockSlot(buildingId, slotIndex, out _, out _);
        RefreshInventoryValue();
        return success;
    }

    public bool IsSlotUnlocked(string slotId)
    {
        if (!TryResolveSlotContext(slotId, out CatalogueBuildingId buildingId, out int slotIndex))
        {
            return false;
        }

        return RuntimeProgressState.EnsureInstance().IsSlotUnlocked(buildingId, slotIndex);
    }

    public bool TryConsumeUnlockCount()
    {
        bool success = RuntimeProgressState.EnsureInstance().TryConsumeSpecialStructureInventory(1);
        RefreshInventoryValue();
        return success;
    }

    private void RefreshInventoryValue()
    {
        availableUnlockCount = RuntimeProgressState.EnsureInstance().AvailableSpecialStructureInventory;
    }

    private static bool TryResolveSlotContext(
        string slotId,
        out CatalogueBuildingId buildingId,
        out int slotIndex)
    {
        buildingId = CatalogueBuildingId.Building1;
        slotIndex = -1;

        if (string.IsNullOrEmpty(slotId))
        {
            return false;
        }

        foreach (BuildingDefinition definition in BuildingDefinitionLibrary.GetAll())
        {
            if (definition.slotDefinitions == null)
            {
                continue;
            }

            for (int i = 0; i < definition.slotDefinitions.Length; i++)
            {
                BuildingSlotDefinition slotDefinition = definition.slotDefinitions[i];
                if (slotDefinition == null || slotDefinition.slotId != slotId)
                {
                    continue;
                }

                buildingId = definition.buildingId;
                slotIndex = i;
                return true;
            }
        }

        return false;
    }
}
