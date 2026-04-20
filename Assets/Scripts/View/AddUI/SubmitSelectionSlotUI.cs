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
        if (hasValidItem) // 替代原有的 if (item != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = item.backIcon;
                iconImage.enabled = true;
                iconImage.color = Color.white;
            }

            if (button != null)
            {
                // 特殊的物品判定：如果是解锁材料则按钮不可点击
                button.interactable = !item.isUnlockMaterial;
            }
        }
        else // 替代原有的 else (item == null)
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
        Debug.Log($"点击了窗口格子，slotIndex = {slotIndex}，物体名 = {gameObject.name}");
        owner?.OnSlotClicked(slotIndex);
    }
}