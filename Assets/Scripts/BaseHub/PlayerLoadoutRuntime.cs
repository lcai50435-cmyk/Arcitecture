using System;

public static class PlayerLoadoutRuntime
{
    public static InkType CurrentInkType { get; set; } = InkType.DirectInk;

    public static WeaponType CurrentWeaponType
    {
        get => CurrentInkType.ToWeaponType();
        set => CurrentInkType = value.ToInkType();
    }

    public static bool AllowBaseAttack { get; set; } = false;

    public static bool IsWeaponUnlocked(WeaponType weaponType)
    {
        if (weaponType == WeaponType.DirectInk)
        {
            return true;
        }

        RuntimeProgressState runtimeState = RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance();
        foreach (BuildingRewardDefinition reward in runtimeState.GetGrantedRewards())
        {
            if (reward != null && reward.unlocksWeapon && reward.unlockedWeaponType == weaponType)
            {
                return true;
            }
        }

        return false;
    }

    public static WeaponType GetFirstUnlockedWeapon()
    {
        Array values = Enum.GetValues(typeof(WeaponType));
        for (int i = 0; i < values.Length; i++)
        {
            WeaponType weaponType = (WeaponType)values.GetValue(i);
            if (IsWeaponUnlocked(weaponType))
            {
                return weaponType;
            }
        }

        return WeaponType.DirectInk;
    }

    public static void EnsureCurrentWeaponUnlocked()
    {
        if (IsWeaponUnlocked(CurrentWeaponType))
        {
            return;
        }

        CurrentWeaponType = GetFirstUnlockedWeapon();
    }
}
