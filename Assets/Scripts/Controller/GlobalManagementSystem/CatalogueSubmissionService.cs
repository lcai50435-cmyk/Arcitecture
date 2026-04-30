using UnityEngine;

public struct CatalogueAutoSubmitResult
{
    public int remainingSpecialStructureCount;
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

public struct CatalogueSubmitCommonStructuresResult
{
    public bool success;
    public CatalogueBuildingId buildingId;
    public int submittedCount;
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
                result.remainingSpecialStructureCount++;
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

    public static CatalogueSubmitCommonStructuresResult SubmitAllCommonStructures(
        BackpackMananger backpack,
        CatalogueBuildingId buildingId)
    {
        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();
        CatalogueSubmitCommonStructuresResult result = new CatalogueSubmitCommonStructuresResult
        {
            buildingId = buildingId,
            previousProgress = runtimeState.GetBuildingProgress(buildingId)
        };

        if (backpack == null)
        {
            result.currentProgress = result.previousProgress;
            return result;
        }

        for (int i = backpack.backpackItems.Count - 1; i >= 0; i--)
        {
            ArchitecturalCrystal? nullableItem = backpack.GetItem(i);
            if (!nullableItem.HasValue || !nullableItem.Value.IsCommonStructure)
            {
                continue;
            }

            CatalogueSubmitCommonStructureResult singleResult = SubmitSingleCommonStructure(backpack, i, buildingId);
            if (!singleResult.success)
            {
                continue;
            }

            result.submittedCount++;
            result.requestedProgress += singleResult.requestedProgress;
            result.appliedProgress += singleResult.appliedProgress;
            result.currentProgress = singleResult.currentProgress;
            if (singleResult.completionReward != null)
            {
                result.completionReward = singleResult.completionReward;
            }
        }

        if (result.currentProgress <= 0)
        {
            result.currentProgress = runtimeState.GetBuildingProgress(buildingId);
        }

        result.success = result.submittedCount > 0 && result.appliedProgress > 0;
        return result;
    }
}
