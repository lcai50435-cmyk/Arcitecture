public static class RuntimeWeaponTypeResolver
{
    public static WeaponType ResolveEffectiveWeaponType(BackpackMananger backpack)
    {
        return ResolveEffectiveWeaponType(backpack, PlayerLoadoutRuntime.CurrentWeaponType);
    }

    public static WeaponType ResolveEffectiveWeaponType(BackpackMananger backpack, WeaponType fallbackWeaponType)
    {
        if (PlayerLoadoutRuntime.TryGetDebugWeaponOverride(out WeaponType debugWeaponType))
        {
            return debugWeaponType;
        }

        return fallbackWeaponType;
    }

    public static bool TryGetActiveWeaponOverride(
        BackpackMananger backpack,
        out ArchitecturalCrystal crystal,
        out WeaponType weaponType,
        out int slotIndex)
    {
        crystal = default;
        weaponType = WeaponType.DirectInk;
        slotIndex = -1;
        return false;
    }
}
