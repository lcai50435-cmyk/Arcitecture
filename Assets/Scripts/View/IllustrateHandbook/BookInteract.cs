using UnityEngine;

public class BookInteract : MonoBehaviour, IInteractable
{
    [Header("图鉴面板")]
    public GameObject illustratedHandbook;

    public string InteractionTip => GameSceneBaseReturnBootstrapper.IsGameSceneActive()
        ? "返回基地"
        : "打开图鉴";

    public void OnInteract()
    {
        if (GameSceneBaseReturnBootstrapper.IsGameSceneActive())
        {
            GameSceneBaseReturnBootstrapper.SubmitCatalogueAndReturnToBase();
            return;
        }

        if (illustratedHandbook != null)
        {
            UIManager.Instance?.OpenIllustratedHandbook();
        }
        else
        {
            Debug.LogError("图鉴面板未赋值");
        }
    }
}
