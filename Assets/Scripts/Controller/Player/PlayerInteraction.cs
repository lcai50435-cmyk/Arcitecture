using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInteraction : MonoBehaviour
{
    private const string InteractPromptId = "player_interact";

    [Header("交互提示UI")]
    public GameObject fImage;
    public GameObject boxPanel;
    public TextMeshProUGUI boxText;

    [Header("最大交互距离")]
    public float interactDistance = 1.2f;

    private IInteractable currentInteractable;
    private Collider2D currentInteractableCollider;
    private bool suppressInteractUi;
    private PlayerMove playerMove;

    private void Awake()
    {
        playerMove = GetComponent<PlayerMove>();
        ResolveRuntimeReferences();
    }

    void Start()
    {
        ResolveRuntimeReferences();
        HideInteractUI();
    }

    private void OnDisable()
    {
        HideInteractUI();
    }

    void Update()
    {
        if (suppressInteractUi)
        {
            HideInteractUI();
            return;
        }

        if (IsGameplayUiBlockingInteraction())
        {
            ClearCurrentInteractable();
            return;
        }

        if (ShouldHoldFloatingPromptUntilPlayerIsControllable())
        {
            ClearCurrentInteractable();
            return;
        }

        UpdateCurrentInteractable();

        if (Input.GetKeyDown(GameSettingsStore.GetKeyBinding(GameInputAction.Interact)))
        {
            if (UIRootManager.Instance != null && UIRootManager.Instance.ShouldSuppressInteractionInput())
            {
                return;
            }

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
        if (suppressInteractUi || ShouldHoldFloatingPromptUntilPlayerIsControllable())
        {
            return;
        }

        ResolveRuntimeReferences();

        if (UseFloatingPromptStyle())
        {
            HideLegacyPromptVisual();
            RuntimeFollowPromptHud.ShowOrUpdate(
                InteractPromptId,
                transform,
                RuntimeFollowPromptHud.FormatCompactKey(GameSettingsStore.GetKeyBinding(GameInputAction.Interact)),
                tip,
                0);
            return;
        }

        if (fImage != null)
        {
            fImage.SetActive(true);
        }

        if (boxPanel != null)
        {
            boxPanel.SetActive(true);
        }

        if (boxText != null)
        {
            boxText.text = tip;
        }
    }

    public void HideInteractUI()
    {
        RuntimeFollowPromptHud.Hide(InteractPromptId);
        HideLegacyPromptVisual();
    }

    public void ClearCurrentInteractable()
    {
        currentInteractable = null;
        currentInteractableCollider = null;
        HideInteractUI();
    }

    public void SetInteractUiSuppressed(bool suppressed)
    {
        suppressInteractUi = suppressed;
        if (suppressed)
        {
            HideInteractUI();
        }
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

        if (UseFloatingPromptStyle())
        {
            HideLegacyPromptVisual();
        }
    }

    private void HideLegacyPromptVisual()
    {
        if (fImage != null)
        {
            fImage.SetActive(false);
        }

        if (boxPanel != null)
        {
            boxPanel.SetActive(false);
        }
    }

    private static bool UseFloatingPromptStyle()
    {
        return UseFloatingPromptStyleForScene(SceneManager.GetActiveScene().name);
    }

    public static bool UseFloatingPromptStyleForScene(string sceneName)
    {
        return GameplayStageCatalog.IsGameplayScene(sceneName) ||
               string.Equals(sceneName, "NewBase", System.StringComparison.Ordinal) ||
               string.Equals(sceneName, "BaseScene", System.StringComparison.Ordinal);
    }

    private bool ShouldHoldFloatingPromptUntilPlayerIsControllable()
    {
        if (!UseFloatingPromptStyle())
        {
            return false;
        }

        if (GameplayStageIntroDirector.IsIntroActive)
        {
            return true;
        }

        playerMove ??= GetComponent<PlayerMove>();
        return playerMove != null && (!playerMove.enabled || !playerMove.canMove);
    }
}
