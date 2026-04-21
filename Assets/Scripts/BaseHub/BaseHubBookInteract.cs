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
        if (uiController == null)
            uiController = FindObjectOfType<BaseHubUIController>();

        uiController?.OpenIllustratedHandbook();
    }
}
