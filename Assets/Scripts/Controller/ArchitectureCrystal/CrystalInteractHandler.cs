using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// 建筑结构物品交互处理器
/// </summary>
public class CrystalInteractHandler : MonoBehaviour, IInteractable
{
    [Header("是否为专用材料")]
    public bool isUnlockMaterial = false;

    [Header("资源分类")]
    public ArchitecturalResourceCategory resourceCategory = ArchitecturalResourceCategory.CommonStructure;

    [Header("晶体配置")]
    public ArchitecturalType type;
    public int expValue;
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

    private string runtimeCollectionId;
    private bool collectionIdResolved;

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
        PlayerGetArchitectural player = FindObjectOfType<PlayerGetArchitectural>();
        if (player == null)
        {
            return;
        }

        ArchitecturalCrystal data = new ArchitecturalCrystal(
            type,
            expValue,
            icon,
            backIcon,
            textDescription,
            bonusType,
            bonusValue,
            subBonusType,
            subBonusValue,
            isUnlockMaterial,
            resourceCategory,
            inkRestoreValue
        );

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
            ArchitecturalResourceCategory category = resourceCategory;
            if (category != ArchitecturalResourceCategory.InkSupply && isUnlockMaterial)
            {
                category = ArchitecturalResourceCategory.SpecialStructure;
            }

            if (category == ArchitecturalResourceCategory.SpecialStructure)
            {
                return "拾取材料";
            }

            if (category == ArchitecturalResourceCategory.InkSupply)
            {
                return "拾取补给";
            }

            return "拾取晶体";
        }
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
