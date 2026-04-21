using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SubmitSelectionSlotUI : MonoBehaviour, IPointerClickHandler
{
    private static readonly Color SelectionBorderColor = new Color(0.98f, 0.85f, 0.48f, 0.95f);
    private static readonly Color SelectionTintColor = new Color(1f, 0.97f, 0.87f, 1f);

    [Header("槽位索引（手动填写）")]
    public int slotIndex;

    [Header("按钮")]
    public Button button;

    [Header("槽位底图（可选）")]
    public Image backgroundImage;

    [Header("图标图片（拖 Slot_X/Icon）")]
    public Image iconImage;

    private SubmitSelectionPanelUI owner;
    private Outline selectionOutline;
    private Color defaultBackgroundColor = Color.white;
    private bool backgroundColorInitialized;

    public void Init(SubmitSelectionPanelUI panelOwner)
    {
        owner = panelOwner;

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (backgroundImage == null)
        {
            backgroundImage = button != null
                ? button.targetGraphic as Image
                : GetComponent<Image>();
        }

        EnsureSelectionVisual();
    }

    public void Refresh(ArchitecturalCrystal item, bool hasValidItem, bool isSelected)
    {
        if (hasValidItem)
        {
            if (iconImage != null)
            {
                iconImage.sprite = item.backIcon != null
                    ? item.backIcon
                    : (item.icon != null ? item.icon : RuntimeCrystalDropFactory.ResolveSprite(item));
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

        ApplySelectionVisual(hasValidItem && isSelected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (button != null && !button.interactable)
        {
            return;
        }

        owner?.OnSlotPressed(slotIndex, Mathf.Max(1, eventData.clickCount));
    }

    private void EnsureSelectionVisual()
    {
        if (backgroundImage == null)
        {
            return;
        }

        if (!backgroundColorInitialized)
        {
            defaultBackgroundColor = backgroundImage.color;
            backgroundColorInitialized = true;
        }

        selectionOutline = backgroundImage.GetComponent<Outline>();
        if (selectionOutline == null)
        {
            selectionOutline = backgroundImage.gameObject.AddComponent<Outline>();
        }

        selectionOutline.effectColor = SelectionBorderColor;
        selectionOutline.effectDistance = new Vector2(4f, 4f);
        selectionOutline.useGraphicAlpha = true;
        selectionOutline.enabled = false;
    }

    private void ApplySelectionVisual(bool isSelected)
    {
        EnsureSelectionVisual();

        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected
                ? Color.Lerp(defaultBackgroundColor, SelectionTintColor, 0.28f)
                : defaultBackgroundColor;
        }

        if (selectionOutline != null)
        {
            selectionOutline.enabled = isSelected;
        }
    }
}
