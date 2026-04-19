using TMPro;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互提示UI")]
    public GameObject fImage;
    public GameObject boxPanel;
    public TextMeshProUGUI boxText;

    [Header("最大交互距离")]
    public float interactDistance = 2.2f;

    private IInteractable currentInteractable;
    private Transform currentInteractableTransform;

    void Start()
    {
        HideInteractUI();
    }

    void Update()
    {
        // 每帧检查当前交互对象是否还在有效距离内
        ValidateCurrentInteractable();

        if (Input.GetKeyDown(KeyCode.F))
        {
            TryInteract();
        }
    }

    private void OnTriggerStay2D(Collider2D col)
    {
        if (!col.TryGetComponent(out IInteractable interactable))
            return;

        float distance = Vector2.Distance(transform.position, col.transform.position);

        // 超出真实交互距离，不记录
        if (distance > interactDistance)
            return;

        currentInteractable = interactable;
        currentInteractableTransform = col.transform;

        ShowInteractUI(currentInteractable.InteractionTip);
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (!col.TryGetComponent(out IInteractable interactable))
            return;

        if (interactable == currentInteractable)
        {
            ClearCurrentInteractable();
        }
    }

    private void TryInteract()
    {
        // 再做一次交互前校验
        if (!IsCurrentInteractableValid())
        {
            ClearCurrentInteractable();
            return;
        }

        if (currentInteractable != null)
        {
            currentInteractable.OnInteract();
        }
    }

    private void ValidateCurrentInteractable()
    {
        if (!IsCurrentInteractableValid())
        {
            ClearCurrentInteractable();
        }
    }

    private bool IsCurrentInteractableValid()
    {
        if (currentInteractable == null || currentInteractableTransform == null)
            return false;

        float distance = Vector2.Distance(transform.position, currentInteractableTransform.position);
        return distance <= interactDistance;
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
        currentInteractableTransform = null;
        HideInteractUI();
    }
}