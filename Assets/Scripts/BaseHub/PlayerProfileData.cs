using UnityEngine;

public class PlayerProfileData : MonoBehaviour
{
    [Header("玩家头像")]
    public Sprite avatar;

    [Header("耐久")]
    public float currentDurability = 100f;
    public float maxDurability = 100f;

    [Header("当前武器")]
    public WeaponType currentWeaponType = WeaponType.Ranged;

    private void Awake()
    {
        currentWeaponType = PlayerLoadoutRuntime.CurrentWeaponType;
    }

    public void SelectWeapon(WeaponType weaponType)
    {
        currentWeaponType = weaponType;
        PlayerLoadoutRuntime.CurrentWeaponType = weaponType;
    }
}
