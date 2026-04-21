using TMPro;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互提示UI")]
    public GameObject fImage;
    public GameObject boxPanel;
    public TextMeshProUGUI boxText;

    [Header("最大交互距离")]
    public float interactDistance = 1.2f;

    private IInteractable currentInteractable;
    private Collider2D currentInteractableCollider;

    void Start()
    {
        HideInteractUI();
    }

    void Update()
    {
        UpdateCurrentInteractable();

        if (Input.GetKeyDown(KeyCode.F))
        {
            TryInteract();
        }
    }

    private void UpdateCurrentInteractable()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactDistance);

        IInteractable nearestInteractable = null;
        Collider2D nearestCollider = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;
            if (hit.gameObject == gameObject) continue;

            if (hit.TryGetComponent(out IInteractable interactable))
            {
                Vector2 closestPoint = hit.ClosestPoint(transform.position);
                float distance = Vector2.Distance(transform.position, closestPoint);

                if (distance <= interactDistance && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestInteractable = interactable;
                    nearestCollider = hit;
                }
            }
        }

        currentInteractable = nearestInteractable;
        currentInteractableCollider = nearestCollider;

        if (currentInteractable != null)
        {
            ShowInteractUI(currentInteractable.InteractionTip);
        }
        else
        {
            HideInteractUI();
        }
    }

    private void TryInteract()
    {
        if (currentInteractable == null || currentInteractableCollider == null)
        {
            ClearCurrentInteractable();
            return;
        }

        Vector2 closestPoint = currentInteractableCollider.ClosestPoint(transform.position);
        float distance = Vector2.Distance(transform.position, closestPoint);

        if (distance > interactDistance)
        {
            ClearCurrentInteractable();
            return;
        }

        currentInteractable.OnInteract();
    }

    private void ShowInteractUI(string tip)
    {
        if (fImage != null)
            fImage.SetActive(true);

        if (boxPanel != null)
            boxPanel.SetActive(true);

        if (boxText != null)
            boxText.text = tip;
    }

    public void HideInteractUI()
    {
        if (fImage != null) fImage.SetActive(false);
        if (boxPanel != null) boxPanel.SetActive(false);
    }

    public void ClearCurrentInteractable()
    {
        currentInteractable = null;
        currentInteractableCollider = null;
        HideInteractUI();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}
