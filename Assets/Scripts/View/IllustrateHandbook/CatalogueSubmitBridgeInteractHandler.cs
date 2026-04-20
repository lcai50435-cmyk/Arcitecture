using UnityEngine;

/// <summary>
/// 图鉴上交桥接器
/// 不改原有上交流程，只在上交前把背包数量转换成“可点亮次数”
/// </summary>
public class CatalogueSubmitBridgeInteractHandler : MonoBehaviour, IInteractable
{
    public string InteractionTip => "打开图鉴并上交";

    public void OnInteract()
    {
        BackpackMananger backpack = BackpackMananger.Instance;
        PlayerGetArchitectural player = FindObjectOfType<PlayerGetArchitectural>();

        if (backpack == null)
        {
            Debug.LogError("未找到 BackpackMananger");
            return;
        }

        if (player == null)
        {
            Debug.LogError("未找到 PlayerGetArchitectural");
            return;
        }

        int itemCount = backpack.backpackItems.Count;

        // 先把这次上交的物品数量转成“可点亮次数”
        if (CatalogueUnlockSelectionManager.Instance != null && itemCount > 0)
        {
            CatalogueUnlockSelectionManager.Instance.AddUnlockCount(itemCount);
        }

        // 继续走你原来的经验上交流程
        player.SubmitAllCachedExp();

        // 打开图鉴
        UIManager.Instance?.OpenIllustratedHandbook();
    }
}