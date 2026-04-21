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

    private void Awake()
    {
        ResolveRuntimeReferences();
    }

    void Start()
    {
        ResolveRuntimeReferences();
        HideInteractUI();
    }

    void Update()
    {
        if (IsGameplayUiBlockingInteraction())
        {
            ClearCurrentInteractable();
            return;
        }

        UpdateCurrentInteractable();

        if (Input.GetKeyDown(GameSettingsStore.GetKeyBinding(GameInputAction.Interact)))
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
        ResolveRuntimeReferences();

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

    private static bool IsGameplayUiBlockingInteraction()
    {
        if (UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen())
        {
            return true;
        }

        return UIManager.Instance != null && UIManager.Instance.IsHandbookOpen;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }

    private void ResolveRuntimeReferences()
    {
        if (boxText == null && boxPanel != null)
        {
            boxText = boxPanel.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }
}
