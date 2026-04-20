using UnityEngine;

/// <summary>
/// 建筑结构物品交互处理器
/// </summary>
public class CrystalInteractHandler : MonoBehaviour, IInteractable
{
    [Header("是否为专用点亮道具")]
    public bool isUnlockMaterial = false;

    [Header("物品配置")]
    public ArchitecturalType type;
    public int expValue;
    public Sprite icon;
    public Sprite backIcon;
    public AttributeBonusType bonusType;
    public float bonusValue;
    [TextArea] public string textDescription;

    // private bool pickSuccess;

    public void OnInteract()
    {
        var player = FindObjectOfType<PlayerGetArchitectural>();
        if (player == null) return;

        // 封装物品数据
        var data = new ArchitecturalCrystal(
            type,
            expValue,
            icon,
            backIcon,
            textDescription,
            bonusType,
            bonusValue,
            isUnlockMaterial
        );   

        // 先判断是否拾取成功
        bool pickSuccess = player.PickCrystal(data);

        // 只有成功捡起来，才删除物品
        if (pickSuccess)
        {
            Destroy(gameObject);
        }
        else
        {
            // 可以加提示：背包已满
            Debug.Log("背包满了，物品保留在地图上");
        }

    }

    public string InteractionTip
    {
        get
        {
            if (isUnlockMaterial)
            {
                return "专用点亮道具";
            }

            return $"拾起宝藏";
        }
    }
}