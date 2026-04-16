using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CatalogueUnlockSlotButton : MonoBehaviour
{
    [Header("槽位唯一ID")]
    public string slotId;

    [Header("要控制颜色的图片（拖父物体 Progress_X 的 Image）")]
    public Image targetImage;

    [Header("说明数据")]
    public UnlockSlotDescriptionData descriptionData;

    [Header("弹窗引用")]
    public Dialog dialogUI;

    private Button button;
    private bool isUnlocked = false;

    public bool IsUnlocked => isUnlocked;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnClickSlot);
        }

        RefreshVisual();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClickSlot);
        }
    }

    private void OnClickSlot()
    {
        if (isUnlocked)
        {
            ShowDescription();
            return;
        }

        if (CatalogueUnlockSelectionManager.Instance == null)
        {
            Debug.LogError("没有 CatalogueUnlockSelectionManager");
            return;
        }

        if (CatalogueUnlockSelectionManager.Instance.TryUnlockSlot(slotId))
        {
            isUnlocked = true;
            RefreshVisual();

            CatalogueBuildingUnlockState buildingState = GetComponentInParent<CatalogueBuildingUnlockState>();
            if (buildingState != null)
            {
                buildingState.RefreshState();
            }
        }
    }

    private void ShowDescription()
    {
        if (dialogUI == null)
        {
            Debug.LogWarning("Dialog 未绑定");
            return;
        }

        string content = "暂无介绍";

        if (descriptionData != null)
        {
            if (!string.IsNullOrEmpty(descriptionData.description))
            {
                content = descriptionData.description;
            }
            else if (!string.IsNullOrEmpty(descriptionData.slotName))
            {
                content = descriptionData.slotName;
            }
        }

        
        dialogUI.ShowClickCloseDialog(content);
    }

    public void RefreshVisual()
    {
        if (CatalogueUnlockSelectionManager.Instance != null)
        {
            isUnlocked = CatalogueUnlockSelectionManager.Instance.IsSlotUnlocked(slotId);
        }

        if (targetImage != null)
        {
            if (isUnlocked)
            {
                targetImage.color = new Color(1f, 1f, 1f, 1f);
            }
            else
            {
                targetImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }
    }
}