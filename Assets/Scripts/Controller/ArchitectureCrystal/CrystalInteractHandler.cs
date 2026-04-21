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
}
