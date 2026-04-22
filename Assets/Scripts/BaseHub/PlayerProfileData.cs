using UnityEngine;

public class PlayerProfileData : MonoBehaviour
{
    [Header("玩家头像")]
    public Sprite avatar;

    [Header("耐久")]
    public float currentDurability = 100f;
    public float maxDurability = 100f;

    [Header("当前墨水基型")]
    public InkType currentInkType = InkType.DirectInk;

    [Header("兼容旧字段")]
    public WeaponType currentWeaponType = WeaponType.DirectInk;

    [Header("当前实战墨水")]
    public InkType effectiveInkType = InkType.DirectInk;

    [Header("当前实战武器")]
    public WeaponType effectiveWeaponType = WeaponType.DirectInk;

    private void Awake()
    {
        PlayerLoadoutRuntime.EnsureCurrentWeaponUnlocked();
        SyncSelectedLoadoutFromRuntime();
        SetEffectiveWeapon(PlayerLoadoutRuntime.CurrentWeaponType);
    }

    public void SelectWeapon(WeaponType weaponType)
    {
        if (!PlayerLoadoutRuntime.IsWeaponUnlocked(weaponType))
        {
            return;
        }

        if (currentWeaponType == weaponType)
        {
            return;
        }

        currentWeaponType = weaponType;
        currentInkType = weaponType.ToInkType();
        PlayerLoadoutRuntime.CurrentWeaponType = weaponType;
        SetEffectiveWeapon(weaponType);
        GameProgressPersistence.SaveIfReady();
    }

    public void SelectInkType(InkType inkType)
    {
        WeaponType weaponType = inkType.ToWeaponType();
        if (!PlayerLoadoutRuntime.IsWeaponUnlocked(weaponType))
        {
            return;
        }

        if (currentWeaponType == weaponType)
        {
            return;
        }

        currentInkType = inkType;
        currentWeaponType = weaponType;
        PlayerLoadoutRuntime.CurrentInkType = inkType;
        SetEffectiveWeapon(weaponType);
        GameProgressPersistence.SaveIfReady();
    }

    public void SyncSelectedLoadoutFromRuntime()
    {
        PlayerLoadoutRuntime.EnsureCurrentWeaponUnlocked();
        currentInkType = PlayerLoadoutRuntime.CurrentInkType;
        currentWeaponType = PlayerLoadoutRuntime.CurrentWeaponType;
    }

    public void SetEffectiveWeapon(WeaponType weaponType)
    {
        effectiveWeaponType = weaponType;
        effectiveInkType = weaponType.ToInkType();
    }

    public void SyncRuntimeState(CharacterCore core, PlayerAttack attack, Sprite fallbackAvatar)
    {
        if (avatar == null && fallbackAvatar != null)
        {
            avatar = fallbackAvatar;
        }

        SyncSelectedLoadoutFromRuntime();
        SetEffectiveWeapon(currentWeaponType);

        if (attack != null)
        {
            currentDurability = attack.ink;
            maxDurability = attack.maxInk;
        }
        else if (core != null && Mathf.Approximately(maxDurability, 0f))
        {
            currentDurability = 100f;
            maxDurability = 100f;
        }
    }
}
