using UnityEngine;

/// <summary>
/// 图鉴解锁选择管理器
/// 负责兼容旧图鉴解锁入口，专用结构以背包格为准。
/// </summary>
public class CatalogueUnlockSelectionManager : MonoBehaviour
{
    public static CatalogueUnlockSelectionManager Instance;

    [Header("当前可用专用结构（运行时观察）")]
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
        BackpackMananger backpack = ResolveRuntimeBackpackManager();
        for (int i = 0; i < count && backpack != null; i++)
        {
            backpack.PickItem(ArchitecturalCrystalFactory.CreateSpecialStructureMaterial());
        }

        RefreshInventoryValue();
    }

    public bool TryUnlockSlot(string slotId)
    {
        if (!TryResolveSlotContext(slotId, out CatalogueBuildingId buildingId, out int slotIndex))
        {
            return false;
        }

        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();
        if (runtimeState.IsSlotUnlocked(buildingId, slotIndex))
        {
            RefreshInventoryValue();
            return false;
        }

        BackpackMananger backpack = ResolveRuntimeBackpackManager();
        if (backpack == null || !backpack.TryConsumeFirstSpecialStructureMaterial(out _))
        {
            RefreshInventoryValue();
            return false;
        }

        bool success = runtimeState.TryUnlockSlot(buildingId, slotIndex, out _, out _);
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
        BackpackMananger backpack = ResolveRuntimeBackpackManager();
        bool success = backpack != null && backpack.TryConsumeFirstSpecialStructureMaterial(out _);
        RefreshInventoryValue();
        return success;
    }

    private void RefreshInventoryValue()
    {
        BackpackMananger backpack = ResolveRuntimeBackpackManager();
        availableUnlockCount = backpack != null ? backpack.GetSpecialStructureMaterialCount() : 0;
    }

    private static BackpackMananger ResolveRuntimeBackpackManager()
    {
        BackpackMananger backpack = BackpackMananger.Instance;
        if (backpack != null)
        {
            return backpack;
        }

        GameObject manager = new GameObject("RuntimeBackpackManager");
        return manager.AddComponent<BackpackMananger>();
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
