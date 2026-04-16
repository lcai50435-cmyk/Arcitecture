using TMPro;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互提示UI")]
    public GameObject fImage;       // F图片
    public GameObject boxPanel;     // 文字框背景
    public TextMeshProUGUI boxText; // 提示文字（如"拾取"、"打开"）

    private IInteractable currentInteractable;

    void Start()
    {
        if (fImage != null) fImage.SetActive(false);
        if (boxPanel != null) boxPanel.SetActive(false);
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

            // 显示F图片
            if (fImage != null)
                fImage.SetActive(true);

            // 从物品获取提示文字（每个物品可以不同）
            if (boxPanel != null)
                boxPanel.SetActive(true);

            if (boxText != null)
            {
                // 使用物品自己的 InteractionTip
                boxText.text = currentInteractable.InteractionTip;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.TryGetComponent(out IInteractable interactable))
        {
            if (interactable == currentInteractable)
            {
                currentInteractable = null;

                if (fImage != null) fImage.SetActive(false);
                if (boxPanel != null) boxPanel.SetActive(false);
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
}