using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CatalogueUnlockSlotButton : MonoBehaviour
{
    [Header("槽位唯一ID")]
    public string slotId;

    [Header("运行时槽位索引")]
    public int slotIndex = -1;

    [Header("要控制颜色的图片（拖父物体 Progress_X 的 Image）")]
    public Image targetImage;

    [Header("说明数据")]
    public UnlockSlotDescriptionData descriptionData;

    [Header("弹窗引用")]
    public Dialog dialogUI;

    private Button button;

    public bool IsUnlocked
    {
        get
        {
            if (!TryResolveContext(out CatalogueBuildingUnlockState buildingState, out int resolvedSlotIndex))
            {
                return false;
            }

            return RuntimeProgressState.EnsureInstance().IsSlotUnlocked(buildingState.BuildingId, resolvedSlotIndex);
        }
    }

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

        if (dialogUI == null)
        {
            dialogUI = FindObjectOfType<Dialog>();
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
        if (!TryResolveContext(out CatalogueBuildingUnlockState buildingState, out int resolvedSlotIndex))
        {
            return;
        }

        if (RuntimeProgressState.EnsureInstance().IsSlotUnlocked(buildingState.BuildingId, resolvedSlotIndex))
        {
            ShowDescription();
            return;
        }

        bool success = RuntimeProgressState.EnsureInstance().TryUnlockSlot(
            buildingState.BuildingId,
            resolvedSlotIndex,
            out BuildingRewardDefinition slotReward,
            out BuildingRewardDefinition completionReward);

        if (!success)
        {
            Debug.Log("没有专用结构材料，无法点亮该小图标");
            return;
        }

        RefreshVisual();
        ShowRewardDialog(slotReward, completionReward);
        buildingState.RefreshState();
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
        else if (TryResolveContext(out CatalogueBuildingUnlockState buildingState, out int resolvedSlotIndex))
        {
            BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingState.BuildingId);
            if (definition.slotDefinitions != null &&
                resolvedSlotIndex >= 0 &&
                resolvedSlotIndex < definition.slotDefinitions.Length)
            {
                BuildingSlotDefinition slotDefinition = definition.slotDefinitions[resolvedSlotIndex];
                content = !string.IsNullOrEmpty(slotDefinition.description)
                    ? slotDefinition.description
                    : slotDefinition.slotName;
            }
        }

        dialogUI.ShowClickCloseDialog(content);
    }

    public void RefreshVisual()
    {
        if (targetImage != null)
        {
            targetImage.color = IsUnlocked
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }

    private bool TryResolveContext(out CatalogueBuildingUnlockState buildingState, out int resolvedSlotIndex)
    {
        buildingState = GetComponentInParent<CatalogueBuildingUnlockState>();
        resolvedSlotIndex = slotIndex;

        if (buildingState == null)
        {
            return false;
        }

        if (resolvedSlotIndex < 0 && buildingState.slotButtons != null)
        {
            for (int i = 0; i < buildingState.slotButtons.Length; i++)
            {
                if (buildingState.slotButtons[i] == this)
                {
                    resolvedSlotIndex = i;
                    slotIndex = i;
                    break;
                }
            }
        }

        return resolvedSlotIndex >= 0;
    }

    private void ShowRewardDialog(BuildingRewardDefinition slotReward, BuildingRewardDefinition completionReward)
    {
        if (dialogUI == null)
        {
            dialogUI = FindObjectOfType<Dialog>();
        }

        if (dialogUI == null)
        {
            return;
        }

        string content = slotReward != null
            ? $"{slotReward.title}\n{slotReward.description}"
            : "槽位已点亮。";

        if (completionReward != null)
        {
            content += $"\n\n{completionReward.title}\n{completionReward.description}";
        }

        dialogUI.ShowClickCloseDialog(content);
    }
}
