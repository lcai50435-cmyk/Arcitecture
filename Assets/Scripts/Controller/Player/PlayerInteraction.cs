using TMPro;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互提示UI")]
    public GameObject fImage;
    public GameObject boxPanel;
    public TextMeshProUGUI boxText;

    private IInteractable currentInteractable;

    void Start()
    {
        HideInteractUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryInteract();
        }
    }

    private void OnTriggerStay2D(Collider2D col)
    {
        if (col.TryGetComponent(out IInteractable interactable))
        {
            currentInteractable = interactable;

            if (fImage != null)
                fImage.SetActive(true);

            if (boxPanel != null)
                boxPanel.SetActive(true);

            if (boxText != null)
                boxText.text = currentInteractable.InteractionTip;
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.TryGetComponent(out IInteractable interactable))
        {
            if (interactable == currentInteractable)
            {
                currentInteractable = null;
                HideInteractUI();
            }
        }
    }

    private void TryInteract()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnInteract();
        }
    }

    public void HideInteractUI()
    {
        if (fImage != null) fImage.SetActive(false);
        if (boxPanel != null) boxPanel.SetActive(false);
    }

    public void ClearCurrentInteractable()
    {
        currentInteractable = null;
        HideInteractUI();
    }
}