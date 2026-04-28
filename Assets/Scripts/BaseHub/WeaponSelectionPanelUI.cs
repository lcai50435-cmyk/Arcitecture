using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectionPanelUI : MonoBehaviour
{
    private readonly List<WeaponOptionView> optionViews = new List<WeaponOptionView>();

    [SerializeField] private Color selectedColor = new Color(0.86f, 0.67f, 0.34f, 1f);
    [SerializeField] private Color effectiveColor = new Color(0.34f, 0.76f, 0.92f, 1f);
    [SerializeField] private Color normalColor = new Color(0.18f, 0.15f, 0.12f, 0.92f);
    [SerializeField] private Color lockedColor = new Color(0.14f, 0.14f, 0.14f, 0.56f);

    private PlayerProfileData profileData;
    private TextMeshProUGUI runtimeSummaryText;

    public void Bind(PlayerProfileData profile)
    {
        profileData = profile;
        profileData?.SyncSelectedLoadoutFromRuntime();
        RefreshSelected();
    }

    public void ConfigureSummary(TextMeshProUGUI summaryText)
    {
        runtimeSummaryText = summaryText;
    }

    private void OnEnable()
    {
        RuntimeProgressState.EnsureInstance().OnStateChanged += RefreshSelected;
    }

    private void OnDisable()
    {
        if (RuntimeProgressState.Instance != null)
        {
            RuntimeProgressState.Instance.OnStateChanged -= RefreshSelected;
        }
    }

    public void RegisterOption(
        WeaponOptionData data,
        Button button,
        Image background,
        TextMeshProUGUI stateLabel)
    {
        if (data == null || button == null) return;

        WeaponOptionView view = new WeaponOptionView(data, button, background, stateLabel);
        optionViews.Add(view);

        button.onClick.AddListener(() =>
        {
            if (profileData == null) return;
            profileData.SelectWeapon(data.weaponType);

            PlayerAttributeManager attributeManager = PlayerAttributeManager.Instance != null
                ? PlayerAttributeManager.Instance
                : FindObjectOfType<PlayerAttributeManager>(true);
            if (attributeManager != null)
            {
                attributeManager.profileData = profileData;
                attributeManager.ApplyAllBonus();
            }

            PlayerAttack playerAttack = FindObjectOfType<PlayerAttack>(true);
            if (playerAttack != null)
            {
                playerAttack.RefreshInkUI();
            }

            RefreshSelected();
        });
    }

    public void RefreshSelected()
    {
        if (profileData == null) return;

        PlayerLoadoutRuntime.EnsureCurrentWeaponUnlocked();
        profileData.SyncSelectedLoadoutFromRuntime();

        WeaponType baseWeaponType = PlayerLoadoutRuntime.CurrentWeaponType;
        bool hasOverride = RuntimeWeaponTypeResolver.TryGetActiveWeaponOverride(
            BackpackMananger.Instance,
            out ArchitecturalCrystal overrideCrystal,
            out WeaponType overrideWeaponType,
            out int overrideSlotIndex);
        WeaponType effectiveWeaponType = hasOverride ? overrideWeaponType : baseWeaponType;
        profileData.SetEffectiveWeapon(effectiveWeaponType);

        foreach (WeaponOptionView view in optionViews)
        {
            bool unlocked = PlayerLoadoutRuntime.IsWeaponUnlocked(view.Data.weaponType);
            bool selected = view.Data.weaponType == baseWeaponType;
            bool isEffectiveOverride = hasOverride &&
                                       effectiveWeaponType != baseWeaponType &&
                                       view.Data.weaponType == effectiveWeaponType;

            if (view.Background != null)
            {
                view.Background.color = !unlocked
                    ? lockedColor
                    : selected
                        ? selectedColor
                        : isEffectiveOverride
                            ? effectiveColor
                        : normalColor;
            }

            if (view.StateLabel != null)
            {
                view.StateLabel.text = !unlocked
                    ? "未解锁"
                    : selected
                        ? "基础装备"
                        : isEffectiveOverride
                            ? "当前实战墨水"
                        : "点击装备";
            }

            if (view.Button != null)
            {
                view.Button.interactable = unlocked;
            }
        }

        RefreshRuntimeSummary(baseWeaponType, effectiveWeaponType, hasOverride, overrideCrystal, overrideSlotIndex);
    }

    private void RefreshRuntimeSummary(
        WeaponType baseWeaponType,
        WeaponType effectiveWeaponType,
        bool hasOverride,
        ArchitecturalCrystal overrideCrystal,
        int overrideSlotIndex)
    {
        if (runtimeSummaryText == null)
        {
            return;
        }

        string baseWeaponName = InkTypeCatalog.GetDisplayName(baseWeaponType);
        string effectiveWeaponName = InkTypeCatalog.GetDisplayName(effectiveWeaponType);

        if (!hasOverride)
        {
            runtimeSummaryText.text =
                $"基础墨水：{baseWeaponName}\n当前实战墨水：{effectiveWeaponName}\n当前攻击按基础墨水生效。";
            return;
        }

        string overrideDescription = overrideSlotIndex >= 0
            ? $"{overrideCrystal.DisplayName}（背包槽 {overrideSlotIndex + 1}，最后拾取优先）"
            : $"{overrideCrystal.DisplayName}（最后拾取优先）";

        runtimeSummaryText.text = effectiveWeaponType == baseWeaponType
            ? $"基础墨水：{baseWeaponName}\n当前实战墨水：{effectiveWeaponName}\n覆盖来源：{overrideDescription}，当前结果与基础墨水一致。"
            : $"基础墨水：{baseWeaponName}\n当前实战墨水：{effectiveWeaponName}\n覆盖来源：{overrideDescription}，当前攻击按背包覆盖结果生效。";
    }

    private sealed class WeaponOptionView
    {
        public readonly WeaponOptionData Data;
        public readonly Button Button;
        public readonly Image Background;
        public readonly TextMeshProUGUI StateLabel;

        public WeaponOptionView(WeaponOptionData data, Button button, Image background, TextMeshProUGUI stateLabel)
        {
            Data = data;
            Button = button;
            Background = background;
            StateLabel = stateLabel;
        }
    }
}
