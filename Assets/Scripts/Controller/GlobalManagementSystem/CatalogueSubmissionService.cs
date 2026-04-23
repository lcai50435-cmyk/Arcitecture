using UnityEngine;

public struct CatalogueAutoSubmitResult
{
    public int specialStructureCount;
    public int inkSupplyCount;
    public int remainingCommonStructureCount;
}

public struct CatalogueSubmitCommonStructureResult
{
    public bool success;
    public CatalogueBuildingId buildingId;
    public int rolledPercent;
    public int requestedProgress;
    public int appliedProgress;
    public int previousProgress;
    public int currentProgress;
    public BuildingRewardDefinition completionReward;
}

public static class CatalogueSubmissionService
{
    public static CatalogueAutoSubmitResult SubmitAllAuto(BackpackMananger backpack)
    {
        CatalogueAutoSubmitResult result = new CatalogueAutoSubmitResult();
        if (backpack == null)
        {
            return result;
        }

        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();

        for (int i = backpack.backpackItems.Count - 1; i >= 0; i--)
        {
            ArchitecturalCrystal? nullableItem = backpack.backpackItems[i];
            if (!nullableItem.HasValue)
            {
                continue;
            }

            ArchitecturalCrystal crystal = nullableItem.Value;
            if (crystal.IsCommonStructure)
            {
                result.remainingCommonStructureCount++;
                continue;
            }

            if (crystal.IsSpecialStructure)
            {
                runtimeState.AddSpecialStructureInventory(1);
                backpack.RemoveItem(i);
                result.specialStructureCount++;
                continue;
            }

            if (crystal.IsInkSupply)
            {
                backpack.RemoveItem(i);
                result.inkSupplyCount++;
            }
        }

        return result;
    }

    public static CatalogueSubmitCommonStructureResult SubmitSingleCommonStructure(
        BackpackMananger backpack,
        int slotIndex,
        CatalogueBuildingId buildingId)
    {
        CatalogueSubmitCommonStructureResult result = new CatalogueSubmitCommonStructureResult
        {
            buildingId = buildingId
        };

        if (backpack == null)
        {
            return result;
        }

        ArchitecturalCrystal? nullableItem = backpack.GetItem(slotIndex);
        if (!nullableItem.HasValue)
        {
            return result;
        }

        ArchitecturalCrystal crystal = nullableItem.Value;
        if (!crystal.IsCommonStructure)
        {
            return result;
        }

        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();
        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);

        result.rolledPercent = crystal.buildProgressPercent > 0
            ? ArchitecturalCrystalFactory.ClampBuildProgressPercent(crystal.buildProgressPercent)
            : ArchitecturalCrystalFactory.MinimumBuildProgressPercent;
        result.previousProgress = runtimeState.GetBuildingProgress(buildingId);
        result.requestedProgress = Mathf.Max(
            1,
            Mathf.RoundToInt(definition.requiredProgress * (result.rolledPercent / 100f)));

        bool added = runtimeState.AddBuildingProgress(
            buildingId,
            result.requestedProgress,
            out BuildingRewardDefinition completionReward);

        result.completionReward = completionReward;
        result.currentProgress = runtimeState.GetBuildingProgress(buildingId);
        result.appliedProgress = Mathf.Max(0, result.currentProgress - result.previousProgress);

        if (!added || result.appliedProgress <= 0)
        {
            result.completionReward = null;
            return result;
        }

        backpack.RemoveItem(slotIndex);
        result.success = true;
        return result;
    }
}
