public struct CatalogueAutoSubmitResult
{
    public int specialStructureCount;
    public int inkSupplyCount;
    public int remainingCommonStructureCount;
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

    public static bool SubmitSingleCommonStructure(
        BackpackMananger backpack,
        int slotIndex,
        CatalogueBuildingId buildingId,
        out BuildingRewardDefinition completionReward)
    {
        completionReward = null;
        if (backpack == null)
        {
            return false;
        }

        ArchitecturalCrystal? nullableItem = backpack.GetItem(slotIndex);
        if (!nullableItem.HasValue)
        {
            return false;
        }

        ArchitecturalCrystal crystal = nullableItem.Value;
        if (!crystal.IsCommonStructure)
        {
            return false;
        }

        bool added = RuntimeProgressState.EnsureInstance()
            .AddBuildingProgress(buildingId, crystal.expValue, out completionReward);

        if (!added)
        {
            return false;
        }

        backpack.RemoveItem(slotIndex);
        return true;
    }
}
