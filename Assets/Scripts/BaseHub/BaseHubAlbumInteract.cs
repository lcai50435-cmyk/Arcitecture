using UnityEngine;

public class BaseHubAlbumInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private BaseHubUIController uiController;

    public string InteractionTip => "查看留念相册";

    public void Configure(BaseHubUIController controller)
    {
        uiController = controller;
    }

    public void OnInteract()
    {
        if (uiController == null)
        {
            uiController = FindObjectOfType<BaseHubUIController>();
        }

        uiController?.OpenAlbumPanel(RuntimeModalOpenSource.Interact);
    }
}
