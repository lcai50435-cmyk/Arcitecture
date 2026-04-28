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
        healthTrans.slider.maxValue = maxHp;
        healthTrans.slider.value = currentHp;

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
            weaponTrans.slider.maxValue = maxDurability;
            weaponTrans.slider.value = currentDurability;
        }

        if (weaponFillImage != null)
        {
            weaponFillImage.color = InkTypeCatalog.GetDisplayColor(GetInkType());
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
