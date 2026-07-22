using UnityEngine;

public class SpiritInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private BaseHubUIController uiController;

    public string InteractionTip => "查看属性与墨水";

    public void Configure(BaseHubUIController controller)
    {
        uiController = controller;
    }

    public void OnInteract()
    {
        if (uiController == null)
            uiController = FindObjectOfType<BaseHubUIController>();

        uiController?.OpenSpiritPanel(RuntimeModalOpenSource.Interact);
    }
}
