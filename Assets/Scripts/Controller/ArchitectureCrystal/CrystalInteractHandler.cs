using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// Building structure item interaction handler
/// </summary>
public class CrystalInteractHandler : MonoBehaviour, IInteractable
{
    private const float PickupAmbushProbability = 0.3f;
    private const int LegacyDefaultCommonExpValue = 30;

    [Header("是否为专用结构")]
    public bool isUnlockMaterial = false;

    [Header("资源分类")]
    public ArchitecturalResourceCategory resourceCategory = ArchitecturalResourceCategory.CommonStructure;

    [Header("修复材料目标建筑")]
    public CatalogueBuildingId repairBuildingId = CatalogueBuildingId.Building1;

    [Header("晶体配置")]
    public ArchitecturalType type;
    public int expValue;
    [Range(0, ArchitecturalCrystalFactory.MaximumBuildProgressPercent)]
    public int buildProgressPercent;
    public Sprite icon;
    public Sprite backIcon;
    public AttributeBonusType bonusType;
    public float bonusValue;
    public AttributeBonusType subBonusType;
    public float subBonusValue;
    [Header("墨水补给恢复量")]
    public int inkRestoreValue;
    [TextArea] public string textDescription;

    [Header("跨场景保留拾取状态")]
    public bool persistCollectedAcrossSceneLoads = true;

    [Header("掉落包装")]
    public bool startClosedAsLootBag = false;
    public Sprite closedLootBagSprite;
    public Sprite revealedLootSprite;

    private string runtimeCollectionId;
    private bool collectionIdResolved;
    private bool hasRuntimeCrystalData;
    private ArchitecturalCrystal runtimeCrystalData;

    private void Awake()
    {
        if (!persistCollectedAcrossSceneLoads)
        {
            return;
        }

        if (TryGetRuntimeCollectionId(out string crystalId) &&
            RuntimeCollectedCrystalRegistry.EnsureInstance().IsCollected(crystalId))
        {
            Destroy(gameObject);
        }
    }

    public void OnInteract()
    {
        Debug.Log($"[CrystalInteract] OnInteract 被调用，type={type}, textDescription={textDescription}");
        
        PlayerGetArchitectural player = FindObjectOfType<PlayerGetArchitectural>();
        if (player == null)
        {
            Debug.LogWarning("[CrystalInteract] 未找到 PlayerGetArchitectural");
            return;
        }

        if (ShouldTriggerPickupAmbush())
        {
            RegisterCollectedState();
            Destroy(gameObject);
            return;
        }

        ArchitecturalCrystal data = BuildRuntimeCrystalData();
        Debug.Log($"[CrystalInteract] BuildRuntimeCrystalData 返回: type={data.type}, DisplayName={data.DisplayName}, IsCommonStructure={data.IsCommonStructure}");

        if (startClosedAsLootBag &&
            RuntimeBackpackPickupAnimator.TryAnimateLootBagPickup(
                data,
                transform.position,
                closedLootBagSprite != null ? closedLootBagSprite : icon))
        {
            RegisterCollectedState();
            Destroy(gameObject);
            return;
        }

        bool pickSuccess = player.PickCrystal(data);

        if (pickSuccess)
        {
            RegisterCollectedState();
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("背包已满，晶体保留在地图上");
        }
    }

    public string InteractionTip
    {
        get
        {
            if (startClosedAsLootBag)
            {
                return "收取锦囊";
            }

            ArchitecturalResourceCategory category = resourceCategory;
            if (category != ArchitecturalResourceCategory.InkSupply && isUnlockMaterial)
            {
                category = ArchitecturalResourceCategory.SpecialStructure;
            }

            if (category == ArchitecturalResourceCategory.SpecialStructure)
            {
                return "拾取材料";
            }

            if (category == ArchitecturalResourceCategory.RepairMaterial)
            {
                return "拾取修复材料";
            }

            if (category == ArchitecturalResourceCategory.InkSupply)
            {
                return "拾取补给";
            }

            return "拾取晶体";
        }
    }

    private ArchitecturalCrystal BuildRuntimeCrystalData()
    {
        if (hasRuntimeCrystalData)
        {
            return runtimeCrystalData;
        }

        ArchitecturalResourceCategory category = ResolveCategory();
        ArchitecturalCrystal crystal;

        if (category == ArchitecturalResourceCategory.SpecialStructure)
        {
            crystal = ArchitecturalCrystalFactory.CreateSpecialStructureMaterial(icon, backIcon);
            OverrideCrystalPresentation(ref crystal);
            runtimeCrystalData = crystal;
            hasRuntimeCrystalData = true;
            return runtimeCrystalData;
        }

        if (category == ArchitecturalResourceCategory.RepairMaterial)
        {
            crystal = ArchitecturalCrystalFactory.CreateRepairMaterial(repairBuildingId, icon, backIcon);
            OverrideCrystalPresentation(ref crystal);
            runtimeCrystalData = crystal;
            hasRuntimeCrystalData = true;
            return runtimeCrystalData;
        }

        if (category == ArchitecturalResourceCategory.InkSupply)
        {
            bool largeBottle = type == ArchitecturalType.LargeInkBottle || inkRestoreValue >= 50;
            crystal = ArchitecturalCrystalFactory.CreateInkSupply(largeBottle, icon, backIcon);
            OverrideCrystalPresentation(ref crystal);
            runtimeCrystalData = crystal;
            hasRuntimeCrystalData = true;
            return runtimeCrystalData;
        }

        crystal = ArchitecturalCrystalFactory.CreateCommonStructure(
            type,
            icon,
            backIcon,
            ResolveCommonStructureBuildProgressPercent());
        OverrideCrystalPresentation(ref crystal);
        runtimeCrystalData = crystal;
        hasRuntimeCrystalData = true;
        return runtimeCrystalData;
    }

    private void OverrideCrystalPresentation(ref ArchitecturalCrystal crystal)
    {
        if (icon != null)
        {
            crystal.icon = icon;
        }

        if (backIcon != null)
        {
            crystal.backIcon = backIcon;
        }
        else if (icon != null && crystal.backIcon == null)
        {
            crystal.backIcon = icon;
        }

        if (!string.IsNullOrEmpty(textDescription))
        {
            crystal.textDescription = textDescription;
        }
    }

    private int ResolveCommonStructureBuildProgressPercent()
    {
        if (buildProgressPercent > 0)
        {
            return ArchitecturalCrystalFactory.ClampBuildProgressPercent(buildProgressPercent);
        }

        if (expValue > 0 && expValue != LegacyDefaultCommonExpValue)
        {
            return ArchitecturalCrystalFactory.ClampBuildProgressPercent(expValue);
        }

        return 0;
    }

    private bool ShouldTriggerPickupAmbush()
    {
        if (UnityEngine.Random.value > PickupAmbushProbability)
        {
            return false;
        }

        bool spawned = RunStageDirector.TryTriggerPickupAmbush(transform.position);
        if (spawned)
        {
            Debug.Log($"拾取 {type} 触发伏击怪");
        }

        return spawned;
    }

    private ArchitecturalResourceCategory ResolveCategory()
    {
        ArchitecturalResourceCategory category = resourceCategory;
        if (category != ArchitecturalResourceCategory.InkSupply && isUnlockMaterial)
        {
            category = ArchitecturalResourceCategory.SpecialStructure;
        }

        return category;
    }

    private void RegisterCollectedState()
    {
        if (!persistCollectedAcrossSceneLoads)
        {
            return;
        }

        if (TryGetRuntimeCollectionId(out string crystalId))
        {
            RuntimeCollectedCrystalRegistry.EnsureInstance().MarkCollected(crystalId);
        }
    }

    private bool TryGetRuntimeCollectionId(out string crystalId)
    {
        if (!collectionIdResolved)
        {
            collectionIdResolved = true;
            runtimeCollectionId = BuildRuntimeCollectionId();
        }

        crystalId = runtimeCollectionId;
        return !string.IsNullOrEmpty(crystalId);
    }

    private string BuildRuntimeCollectionId()
    {
        if (!persistCollectedAcrossSceneLoads || !gameObject.scene.IsValid())
        {
            return string.Empty;
        }

        string sceneIdentifier = string.IsNullOrEmpty(gameObject.scene.path) ? gameObject.scene.name : gameObject.scene.path;
        StringBuilder hierarchyBuilder = new StringBuilder();

        Transform current = transform;
        while (current != null)
        {
            hierarchyBuilder.Insert(0, $"/{current.name}[{current.GetSiblingIndex()}]");
            current = current.parent;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append(sceneIdentifier);
        builder.Append(hierarchyBuilder);
        builder.Append('|').Append(type);
        builder.Append('|').Append(transform.position.x.ToString("0.###", CultureInfo.InvariantCulture));
        builder.Append(',').Append(transform.position.y.ToString("0.###", CultureInfo.InvariantCulture));
        builder.Append(',').Append(transform.position.z.ToString("0.###", CultureInfo.InvariantCulture));
        return builder.ToString();
    }
}
