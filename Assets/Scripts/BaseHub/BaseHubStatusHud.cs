using TMPro;
using UnityEngine;

public class BaseHubStatusHud : MonoBehaviour
{
    [SerializeField] private CharacterCore characterCore;
    [SerializeField] private PlayerProfileData profileData;
    [SerializeField] private ValueTrans healthTrans;
    [SerializeField] private ValueTrans weaponTrans;
    [SerializeField] private TextMeshProUGUI healthValueText;
    [SerializeField] private TextMeshProUGUI weaponValueText;

    public void Configure(
        CharacterCore core,
        PlayerProfileData profile,
        ValueTrans healthGauge,
        ValueTrans weaponGauge,
        TextMeshProUGUI healthText,
        TextMeshProUGUI weaponText)
    {
        characterCore = core;
        profileData = profile;
        healthTrans = healthGauge;
        weaponTrans = weaponGauge;
        healthValueText = healthText;
        weaponValueText = weaponText;

        if (healthTrans != null)
        {
            GameplayStatusHudRuntime.ApplyHealthStatusBarSkin(healthTrans.slider);
        }

        if (weaponTrans != null)
        {
            GameplayStatusHudRuntime.ApplyInkStatusBarSkin(weaponTrans.slider);
        }

        RefreshImmediate();
    }

    private void Update()
    {
        RuntimeMiniMapHud.EnsureInstance();
        RefreshImmediate();
    }

    private void RefreshImmediate()
    {
        RefreshHealth();
        RefreshWeapon();
    }

    private void RefreshHealth()
    {
        if (characterCore == null || healthTrans == null || healthTrans.slider == null)
        {
            return;
        }

        float maxHp = Mathf.Max(1f, characterCore.stats.maxHp);
        float currentHp = Mathf.Clamp(characterCore.currentHp, 0f, maxHp);
        healthTrans.SetMaxValue(maxHp);
        healthTrans.SetValue(currentHp);

        if (healthValueText != null)
        {
            healthValueText.text = $"{currentHp:0}/{maxHp:0}";
        }
    }

    private void RefreshWeapon()
    {
        float currentDurability = profileData != null ? profileData.currentDurability : 100f;
        float maxDurability = profileData != null ? Mathf.Max(1f, profileData.maxDurability) : 100f;

        if (weaponTrans != null && weaponTrans.slider != null)
        {
            weaponTrans.SetMaxValue(maxDurability);
            weaponTrans.SetValue(currentDurability);
        }

        UpdateWeaponText();
    }

    private void UpdateWeaponText()
    {
        if (weaponValueText == null)
        {
            return;
        }

        string durabilityText = profileData != null
            ? $"  耐久 {profileData.currentDurability:0}/{profileData.maxDurability:0}"
            : string.Empty;
        weaponValueText.text = $"{InkTypeCatalog.GetDisplayName(GetInkType())}{durabilityText}";
    }

    private InkType GetInkType()
    {
        return profileData != null
            ? profileData.currentInkType
            : PlayerLoadoutRuntime.CurrentInkType;
    }
}
