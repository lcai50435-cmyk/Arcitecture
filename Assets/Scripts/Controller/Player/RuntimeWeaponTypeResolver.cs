public static class RuntimeWeaponTypeResolver
{
    public static WeaponType ResolveEffectiveWeaponType(BackpackMananger backpack)
    {
        return ResolveEffectiveWeaponType(backpack, PlayerLoadoutRuntime.CurrentWeaponType);
    }

    public static WeaponType ResolveEffectiveWeaponType(BackpackMananger backpack, WeaponType fallbackWeaponType)
    {
        WeaponType baseWeaponType = PlayerLoadoutRuntime.TryGetDebugWeaponOverride(out WeaponType debugWeaponType)
            ? debugWeaponType
            : fallbackWeaponType;

        return TryGetActiveWeaponOverride(backpack, out _, out WeaponType overrideWeaponType, out _)
            ? overrideWeaponType
            : baseWeaponType;
    }

    public static bool TryGetActiveWeaponOverride(
        BackpackMananger backpack,
        out ArchitecturalCrystal crystal,
        out WeaponType weaponType,
        out int slotIndex)
    {
        return TryResolveLatestOverride(backpack, out crystal, out weaponType, out slotIndex);
    }

    public static bool TryGetOverrideWeaponType(ArchitecturalType type, out WeaponType weaponType)
    {
        switch (type)
        {
            case ArchitecturalType.Brackets:
            case ArchitecturalType.Tile:
                weaponType = WeaponType.BurstInk;
                return true;
            case ArchitecturalType.MortiseAndTenonJoint:
            case ArchitecturalType.BeamFrame:
                weaponType = WeaponType.PierceInk;
                return true;
            case ArchitecturalType.TampedEarth:
            case ArchitecturalType.GroundMass:
                weaponType = WeaponType.FlowInk;
                return true;
            default:
                weaponType = WeaponType.DirectInk;
                return false;
        }
    }

    private static bool TryResolveLatestOverride(
        BackpackMananger backpack,
        out ArchitecturalCrystal crystal,
        out WeaponType weaponType,
        out int slotIndex)
    {
        crystal = default;
        weaponType = WeaponType.DirectInk;
        slotIndex = -1;

        if (backpack == null || backpack.backpackItems == null)
        {
            return false;
        }

        bool foundOverride = false;
        int latestPickupOrder = int.MinValue;
        int latestSlotIndex = -1;

        for (int i = 0; i < backpack.backpackItems.Count; i++)
        {
            ArchitecturalCrystal? nullableItem = backpack.backpackItems[i];
            if (!nullableItem.HasValue)
            {
                continue;
            }

            ArchitecturalCrystal item = nullableItem.Value;
            if (!item.IsCommonStructure || !TryGetOverrideWeaponType(item.type, out WeaponType overrideWeaponType))
            {
                continue;
            }

            if (!foundOverride ||
                item.runtimePickupOrder > latestPickupOrder ||
                (item.runtimePickupOrder == latestPickupOrder && i > latestSlotIndex))
            {
                foundOverride = true;
                latestPickupOrder = item.runtimePickupOrder;
                latestSlotIndex = i;
                crystal = item;
                weaponType = overrideWeaponType;
                slotIndex = i;
            }
        }

        return foundOverride;
    }
}
