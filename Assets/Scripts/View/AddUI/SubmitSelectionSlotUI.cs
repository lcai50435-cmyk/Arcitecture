using UnityEngine;
using UnityEngine.UI;

public class SubmitSelectionSlotUI : MonoBehaviour
{
    [Header("槽位索引（手动填写）")]
    public int slotIndex;

    [Header("按钮")]
    public Button button;

    [Header("图标图片（拖 Slot_X/Icon）")]
    public Image iconImage;

    private SubmitSelectionPanelUI owner;

    public void Init(SubmitSelectionPanelUI panelOwner)
    {
        owner = panelOwner;

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickSlot);
        }
    }

    public void Refresh(ArchitecturalCrystal item, bool hasValidItem)
    {
        if (hasValidItem)
        {
            if (iconImage != null)
            {
                iconImage.sprite = item.backIcon;
                iconImage.enabled = true;
                iconImage.color = Color.white;
            }

            if (button != null)
            {
                button.interactable = item.IsCommonStructure;
            }
        }
        else
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (button != null)
            {
                button.interactable = false;
            }
        }
    }


    private void OnClickSlot()
    {
        owner?.OnSlotClicked(slotIndex);
    }
}
