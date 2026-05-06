using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CatalogueUnlockSlotButton : MonoBehaviour, IDropHandler
{
    private const float DoubleClickWindow = 0.32f;
    private static readonly Color LockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    private static readonly Color ArmedColor = new Color(0.82f, 0.78f, 0.62f, 1f);
    private static readonly Color ArmedOutlineColor = new Color(0.98f, 0.86f, 0.48f, 0.95f);

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
    private Outline armedOutline;
    private bool pendingUnlockArmed;
    private float lastLockedClickTime = -10f;

    public bool IsUnlocked
    {
        get
        {
            if (!TryResolveRuntimeSlotContext(out _, out _, out CatalogueBuildingId buildingId, out int resolvedSlotIndex))
            {
                return false;
            }

            return RuntimeProgressState.EnsureInstance().IsSlotUnlocked(buildingId, resolvedSlotIndex);
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

        ResolveDialogReference();

        RefreshVisual();
    }

    private void Update()
    {
        if (!pendingUnlockArmed)
        {
            return;
        }

        if (Time.unscaledTime - lastLockedClickTime <= DoubleClickWindow)
        {
            return;
        }

        pendingUnlockArmed = false;
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
        if (!TryResolveRuntimeSlotContext(
                out CatalogueBuildingUnlockState buildingState,
                out _,
                out CatalogueBuildingId buildingId,
                out int resolvedSlotIndex))
        {
            return;
        }

        if (RuntimeProgressState.EnsureInstance().IsSlotUnlocked(buildingId, resolvedSlotIndex))
        {
            pendingUnlockArmed = false;
            ShowDescription(buildingId, resolvedSlotIndex);
            return;
        }

        pendingUnlockArmed = false;
        RefreshVisual();
        ShowUnlockRequirementPrompt(buildingId, resolvedSlotIndex);
        buildingState?.RefreshState();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!TryResolveRuntimeSlotContext(
                out CatalogueBuildingUnlockState buildingState,
                out _,
                out CatalogueBuildingId buildingId,
                out int resolvedSlotIndex))
        {
            return;
        }

        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();
        if (runtimeState.IsSlotUnlocked(buildingId, resolvedSlotIndex))
        {
            pendingUnlockArmed = false;
            RefreshVisual();
            return;
        }

        if (!BackpackSlot.TryGetDraggedSpecialStructureSlot(eventData?.pointerDrag, out int sourceSlotIndex))
        {
            ShowUnlockRequirementPrompt(buildingId, resolvedSlotIndex);
            return;
        }

        BackpackMananger backpack = ResolveRuntimeBackpackManager();
        if (backpack == null || !backpack.TryConsumeSpecialStructureMaterial(sourceSlotIndex))
        {
            ShowUnlockRequirementPrompt(buildingId, resolvedSlotIndex);
            return;
        }

        bool success = runtimeState.TryUnlockSlot(
            buildingId,
            resolvedSlotIndex,
            out BuildingRewardDefinition slotReward,
            out BuildingRewardDefinition completionReward);
        if (success && runtimeState.CanUnlockBuilding(buildingId))
        {
            runtimeState.TryUnlockBuilding(buildingId, out completionReward);
        }

        pendingUnlockArmed = false;
        buildingState?.RefreshState();
        RefreshVisual();
        RefreshBackpackViews();

        if (!success)
        {
            ShowUnlockRequirementPrompt(buildingId, resolvedSlotIndex);
            return;
        }

        ShowRewardDialog(slotReward, completionReward);
    }

    private void ShowDescription(CatalogueBuildingId buildingId, int resolvedSlotIndex)
    {
        if (!ResolveDialogReference())
        {
            Debug.LogWarning("Dialog 未绑定");
            return;
        }

        string content = BuildDescriptionContent(buildingId, resolvedSlotIndex);
        dialogUI.ShowClickCloseDialog(content);
    }

    public void RefreshVisual()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.enabled = true;
            button.interactable = true;
        }

        if (targetImage != null)
        {
            targetImage.raycastTarget = true;
            targetImage.color = IsUnlocked
                ? new Color(1f, 1f, 1f, 1f)
                : (pendingUnlockArmed ? ArmedColor : LockedColor);
        }

        RefreshArmedOutline();
    }

    private bool TryResolveRuntimeSlotContext(
        out CatalogueBuildingUnlockState buildingState,
        out CatalogueBuildingUnlockState parentBuildingState,
        out CatalogueBuildingId buildingId,
        out int resolvedSlotIndex)
    {
        buildingState = GetComponentInParent<CatalogueBuildingUnlockState>();
        parentBuildingState = buildingState;
        buildingId = CatalogueBuildingId.Building1;
        resolvedSlotIndex = slotIndex;

        if (buildingState == null)
        {
            return TryResolveBySlotId(out buildingId, out resolvedSlotIndex);
        }

        buildingId = buildingState.BuildingId;

        if (buildingState.slotButtons != null)
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

        // At runtime, prefer the slot order from the parent building config while supporting prefabs that still keep old slotIds.
        if (IsResolvedSlotIndexValid(buildingId, resolvedSlotIndex))
        {
            return true;
        }

        bool resolvedBySlotId = TryResolveBySlotId(out buildingId, out resolvedSlotIndex);
        if (resolvedBySlotId)
        {
            slotIndex = resolvedSlotIndex;
        }

        return resolvedBySlotId;
    }

    private static bool IsResolvedSlotIndexValid(CatalogueBuildingId buildingId, int resolvedSlotIndex)
    {
        if (resolvedSlotIndex < 0)
        {
            return false;
        }

        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        if (definition.slotDefinitions == null || resolvedSlotIndex >= definition.slotDefinitions.Length)
        {
            return false;
        }

        return definition.slotDefinitions[resolvedSlotIndex] != null;
    }

    private bool TryResolveBySlotId(out CatalogueBuildingId buildingId, out int resolvedSlotIndex)
    {
        buildingId = CatalogueBuildingId.Building1;
        resolvedSlotIndex = -1;

        if (string.IsNullOrEmpty(slotId))
        {
            return false;
        }

        foreach (BuildingDefinition definition in BuildingDefinitionLibrary.GetAll())
        {
            if (definition.slotDefinitions == null)
            {
                continue;
            }

            for (int i = 0; i < definition.slotDefinitions.Length; i++)
            {
                BuildingSlotDefinition slotDefinition = definition.slotDefinitions[i];
                if (slotDefinition == null || slotDefinition.slotId != slotId)
                {
                    continue;
                }

                buildingId = definition.buildingId;
                resolvedSlotIndex = i;
                return true;
            }
        }

        return false;
    }

    private string BuildDescriptionContent(CatalogueBuildingId buildingId, int resolvedSlotIndex)
    {
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

        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        if (definition.slotDefinitions != null &&
            resolvedSlotIndex >= 0 &&
            resolvedSlotIndex < definition.slotDefinitions.Length)
        {
            BuildingSlotDefinition slotDefinition = definition.slotDefinitions[resolvedSlotIndex];
            if (slotDefinition != null)
            {
                if (string.IsNullOrEmpty(content) || content == "暂无介绍")
                {
                    content = !string.IsNullOrEmpty(slotDefinition.description)
                        ? slotDefinition.description
                        : slotDefinition.slotName;
                }

                if (slotDefinition.reward != null && !string.IsNullOrEmpty(slotDefinition.reward.description))
                {
                    content += $"\n\n永久效果：{slotDefinition.reward.description}";
                }
            }
        }

        return content;
    }

    private void ShowRewardDialog(BuildingRewardDefinition slotReward, BuildingRewardDefinition completionReward)
    {
        if (!ResolveDialogReference())
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

    private void ShowUnlockRequirementPrompt(CatalogueBuildingId buildingId, int resolvedSlotIndex)
    {
        BackpackMananger backpack = ResolveRuntimeBackpackManager();
        int remainingInventory = backpack != null ? backpack.GetSpecialStructureMaterialCount() : 0;
        string slotName = ResolveSlotName(buildingId, resolvedSlotIndex);
        string content = $"点亮 {slotName} 需要拖动 1 个专用结构到该槽位。\n当前背包专用结构：{remainingInventory}";

        if (!ResolveDialogReference())
        {
            Debug.Log(content);
            return;
        }

        dialogUI.ShowAutoDialogForce(content);
    }

    private static BackpackMananger ResolveRuntimeBackpackManager()
    {
        return BackpackMananger.Instance != null
            ? BackpackMananger.Instance
            : FindObjectOfType<BackpackMananger>(true);
    }

    private static void RefreshBackpackViews()
    {
        BackpackUI backpackUI = FindObjectOfType<BackpackUI>(true);
        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }

        GameplayStatusHudRuntime.RefreshStructureProgressText();
    }

    private static string ResolveSlotName(CatalogueBuildingId buildingId, int resolvedSlotIndex)
    {
        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        if (definition.slotDefinitions == null ||
            resolvedSlotIndex < 0 ||
            resolvedSlotIndex >= definition.slotDefinitions.Length ||
            definition.slotDefinitions[resolvedSlotIndex] == null)
        {
            return "该槽位";
        }

        string slotName = definition.slotDefinitions[resolvedSlotIndex].slotName;
        return string.IsNullOrWhiteSpace(slotName) ? "该槽位" : slotName;
    }

    private bool ResolveDialogReference()
    {
        dialogUI = Dialog.EnsureTopmostRuntimeInstance();
        return dialogUI != null;
    }

    private void RefreshArmedOutline()
    {
        Image outlineTarget = targetImage != null
            ? targetImage
            : (button != null ? button.targetGraphic as Image : null);
        if (outlineTarget == null)
        {
            return;
        }

        armedOutline = outlineTarget.GetComponent<Outline>();
        if (armedOutline == null)
        {
            armedOutline = outlineTarget.gameObject.AddComponent<Outline>();
        }

        armedOutline.effectColor = ArmedOutlineColor;
        armedOutline.effectDistance = new Vector2(4f, 4f);
        armedOutline.useGraphicAlpha = true;
        armedOutline.enabled = pendingUnlockArmed && !IsUnlocked;
    }
}
