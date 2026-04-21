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

    private void Awake()
    {
        PlayerLoadoutRuntime.EnsureCurrentWeaponUnlocked();
        currentInkType = PlayerLoadoutRuntime.CurrentInkType;
        currentWeaponType = PlayerLoadoutRuntime.CurrentWeaponType;
    }

    public void SelectWeapon(WeaponType weaponType)
    {
        if (!PlayerLoadoutRuntime.IsWeaponUnlocked(weaponType))
        {
            return;
        }

        currentWeaponType = weaponType;
        currentInkType = weaponType.ToInkType();
        PlayerLoadoutRuntime.CurrentWeaponType = weaponType;
    }

    public void SelectInkType(InkType inkType)
    {
        WeaponType weaponType = inkType.ToWeaponType();
        if (!PlayerLoadoutRuntime.IsWeaponUnlocked(weaponType))
        {
            return;
        }

        currentInkType = inkType;
        currentWeaponType = weaponType;
        PlayerLoadoutRuntime.CurrentInkType = inkType;
    }
}
