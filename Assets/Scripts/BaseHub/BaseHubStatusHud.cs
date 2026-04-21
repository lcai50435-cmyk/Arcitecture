using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseHubStatusHud : MonoBehaviour
{
    [SerializeField] private CharacterCore characterCore;
    [SerializeField] private PlayerProfileData profileData;
    [SerializeField] private ValueTrans healthTrans;
    [SerializeField] private ValueTrans weaponTrans;
    [SerializeField] private Image weaponFillImage;
    [SerializeField] private TextMeshProUGUI healthValueText;
    [SerializeField] private TextMeshProUGUI weaponValueText;
    [SerializeField] private float weaponGaugeValue = 100f;

    public void Configure(
        CharacterCore core,
        PlayerProfileData profile,
        ValueTrans healthGauge,
        ValueTrans weaponGauge,
        Image weaponFill,
        TextMeshProUGUI healthText,
        TextMeshProUGUI weaponText)
    {
        characterCore = core;
        profileData = profile;
        healthTrans = healthGauge;
        weaponTrans = weaponGauge;
        weaponFillImage = weaponFill;
        healthValueText = healthText;
        weaponValueText = weaponText;
        RefreshImmediate();
    }

    private void Update()
    {
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
        healthTrans.slider.maxValue = maxHp;
        healthTrans.slider.value = currentHp;

        if (healthValueText != null)
        {
            healthValueText.text = $"{currentHp:0}/{maxHp:0}";
        }
    }

    private void RefreshWeapon()
    {
        if (weaponTrans != null && weaponTrans.slider != null)
        {
            weaponTrans.slider.maxValue = weaponGaugeValue;
            weaponTrans.slider.value = weaponGaugeValue;
        }

        if (weaponFillImage == null)
        {
            UpdateWeaponText();
            return;
        }

        weaponFillImage.color = GetWeaponColor();
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
        weaponValueText.text = $"{GetWeaponDisplayName()}{durabilityText}";
    }

    private WeaponType GetWeaponType()
    {
        return profileData != null
            ? profileData.currentWeaponType
            : PlayerLoadoutRuntime.CurrentWeaponType;
    }

    private Color GetWeaponColor()
    {
        WeaponType currentWeapon = GetWeaponType();

        switch (currentWeapon)
        {
            case WeaponType.Melee:
                return new Color(0.88f, 0.36f, 0.22f, 1f);
            case WeaponType.Special:
                return new Color(0.96f, 0.78f, 0.24f, 1f);
            default:
                return new Color(0.26f, 0.72f, 0.90f, 1f);
        }
    }

    private string GetWeaponDisplayName()
    {
        switch (GetWeaponType())
        {
            case WeaponType.Melee:
                return "近战";
            case WeaponType.Special:
                return "特殊";
            default:
                return "远程";
        }
    }
}
