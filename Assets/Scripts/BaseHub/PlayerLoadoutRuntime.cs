using System;
using UnityEngine;

public static class PlayerLoadoutRuntime
{
    private static bool hasDebugWeaponOverride;
    private static WeaponType debugWeaponOverride = WeaponType.DirectInk;
    private static InkType currentInkType = InkType.DirectInk;

    public static InkType CurrentInkType
    {
        get => currentInkType;
        set
        {
            currentInkType = value;
            ClearDebugWeaponOverride();
        }
    }

    public static WeaponType CurrentWeaponType
    {
        get => CurrentInkType.ToWeaponType();
        set => CurrentInkType = value.ToInkType();
    }

    public static bool AllowBaseAttack { get; set; } = false;

    public static bool HasDebugWeaponOverride => hasDebugWeaponOverride;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        currentInkType = InkType.DirectInk;
        AllowBaseAttack = false;
        ClearDebugWeaponOverride();
    }

    public static void SetDebugWeaponOverride(WeaponType weaponType)
    {
        debugWeaponOverride = weaponType;
        hasDebugWeaponOverride = true;
    }

    public static void ClearDebugWeaponOverride()
    {
        debugWeaponOverride = WeaponType.DirectInk;
        hasDebugWeaponOverride = false;
    }

    public static bool TryGetDebugWeaponOverride(out WeaponType weaponType)
    {
        weaponType = debugWeaponOverride;
        return hasDebugWeaponOverride;
    }

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
