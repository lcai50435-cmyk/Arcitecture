using UnityEngine;

public class PlayerGetArchitectural : MonoBehaviour
{
    private BackpackMananger backpack;
    private BackpackUI backpackUI;

    private void Start()
    {
        ResolveRuntimeDependencies();
    }

    public bool PickCrystal(ArchitecturalCrystal crystal)
    {
        if (!ResolveRuntimeDependencies() || backpack == null)
        {
            return false;
        }

        if (!backpack.PickItem(crystal))
        {
            return false;
        }

        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }

        return true;
    }

    public void SubmitAllCachedExp()
    {
        if (!ResolveRuntimeDependencies() || backpack == null)
        {
            return;
        }

        if (backpack.GetOccupiedCount() == 0)
        {
            Debug.Log("Backpack is empty");
            return;
        }

        CatalogueAutoSubmitResult result = CatalogueSubmissionService.SubmitAllAuto(backpack);
        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }

        Debug.Log(
            $"自动提交完成：专用结构 {result.specialStructureCount}，补给 {result.inkSupplyCount}，剩余普通结构 {result.remainingCommonStructureCount}");
    }

    public void SubmitSingleItem(int index)
    {
        Debug.LogWarning("普通结构提交需要指定目标建筑，请使用 SubmitSingleItemToBuilding。");
    }

    public bool ConsumeOneUnlockMaterial()
    {
        return RuntimeProgressState.EnsureInstance().TryConsumeSpecialStructureInventory(1);
    }

    public void SubmitSingleItemToBuilding(int index, CatalogueBuildingId buildingId)
    {
        if (!ResolveRuntimeDependencies() || backpack == null)
        {
            return;
        }

        bool submitted = CatalogueSubmissionService.SubmitSingleCommonStructure(
            backpack,
            index,
            buildingId,
            out BuildingRewardDefinition completionReward);

        if (!submitted)
        {
            return;
        }

        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }

        if (completionReward != null)
        {
            ShowRewardDialog(completionReward);
        }
    }

    private bool ResolveRuntimeDependencies()
    {
        if (backpack == null)
        {
            backpack = BackpackMananger.Instance;
        }

        if (backpack == null)
        {
            GameObject manager = new GameObject("RuntimeBackpackManager");
            backpack = manager.AddComponent<BackpackMananger>();
            Debug.Log("Created runtime BackpackMananger for PlayerGetArchitectural");
        }

        if (backpackUI == null)
        {
            backpackUI = FindObjectOfType<BackpackUI>(true);
        }

        if (backpack == null)
        {
            Debug.LogError("PlayerGetArchitectural missing BackpackMananger");
            return false;
        }

        return true;
    }

    private void ShowRewardDialog(BuildingRewardDefinition reward)
    {
        if (reward == null)
        {
            return;
        }

        Dialog dialog = FindObjectOfType<Dialog>(true);
        if (dialog == null)
        {
            return;
        }

        dialog.ShowClickCloseDialog($"{reward.title}\n{reward.description}");
    }
}
