using UnityEngine;

public class CatalogueSubmitBridgeInteractHandler : MonoBehaviour, IInteractable
{
    public string InteractionTip => GameSceneBaseReturnBootstrapper.IsGameSceneActive()
        ? "返回基地"
        : "打开图鉴并上交";

    public void OnInteract()
    {
        if (GameSceneBaseReturnBootstrapper.IsGameSceneActive())
        {
            GameSceneBaseReturnBootstrapper.SubmitCatalogueAndReturnToBase();
            return;
        }

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

        int itemCount = backpack.GetOccupiedCount();
        if (CatalogueUnlockSelectionManager.Instance != null && itemCount > 0)
        {
            CatalogueUnlockSelectionManager.Instance.AddUnlockCount(itemCount);
        }

        player.SubmitAllCachedExp();
        UIManager.Instance?.OpenIllustratedHandbook();
    }
}
