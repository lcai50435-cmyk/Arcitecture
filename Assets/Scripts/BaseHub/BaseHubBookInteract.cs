using UnityEngine;

public class BaseHubBookInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private BaseHubUIController uiController;

    public string InteractionTip => "打开图鉴";

    public void Configure(BaseHubUIController controller)
    {
        uiController = controller;
    }

    public void OnInteract()
    {
        if (IllustratedUISceneLoader.Open(
                RuntimeModalOpenSource.Interact,
                IllustratedHandbookPage.IllustratedHandbook))
        {
            return;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenIllustratedHandbook(RuntimeModalOpenSource.Interact);
            return;
        }

        if (uiController == null)
            uiController = FindObjectOfType<BaseHubUIController>();

        uiController?.OpenIllustratedHandbook(RuntimeModalOpenSource.Interact);
    }
}
