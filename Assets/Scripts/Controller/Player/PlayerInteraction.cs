using TMPro;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互提示UI")]
<<<<<<< HEAD
    public GameObject fImage;       // F图片
    public GameObject boxPanel;     // 文字框背景
    public TextMeshProUGUI boxText; // 提示文字（如"拾取"、"打开"）
=======
    public GameObject fImage;
    public GameObject boxPanel;
    public TextMeshProUGUI boxText;
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58

    private IInteractable currentInteractable;

    void Start()
    {
<<<<<<< HEAD
        if (fImage != null) fImage.SetActive(false);
        if (boxPanel != null) boxPanel.SetActive(false);
=======
        HideInteractUI();
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryInteract();
        }
    }

<<<<<<< HEAD
    private void OnTriggerStay2D(Collider2D col)
=======
    private void OnTriggerEnter2D(Collider2D col)
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
    {
        if (col.TryGetComponent(out IInteractable interactable))
        {
            currentInteractable = interactable;

<<<<<<< HEAD
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

=======
            if (fImage != null)
                fImage.SetActive(true);

            if (boxPanel != null)
                boxPanel.SetActive(true);

            if (boxText != null)
                boxText.text = currentInteractable.InteractionTip;
        }
    }

>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.TryGetComponent(out IInteractable interactable))
        {
            if (interactable == currentInteractable)
            {
                currentInteractable = null;
<<<<<<< HEAD

                if (fImage != null) fImage.SetActive(false);
                if (boxPanel != null) boxPanel.SetActive(false);
=======
                HideInteractUI();
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
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