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
        bool hasDebugOverride = PlayerLoadoutRuntime.TryGetDebugWeaponOverride(out WeaponType debugWeaponType);
        WeaponType selectedWeaponType = hasDebugOverride ? debugWeaponType : baseWeaponType;
        bool hasBackpackOverride = RuntimeWeaponTypeResolver.TryGetActiveWeaponOverride(
            BackpackMananger.Instance,
            out ArchitecturalCrystal overrideCrystal,
            out WeaponType overrideWeaponType,
            out int overrideSlotIndex);
        WeaponType effectiveWeaponType = hasBackpackOverride ? overrideWeaponType : selectedWeaponType;
        profileData.SetEffectiveWeapon(effectiveWeaponType);

        foreach (WeaponOptionView view in optionViews)
        {
            bool unlocked = PlayerLoadoutRuntime.IsWeaponUnlocked(view.Data.weaponType);
            bool selected = view.Data.weaponType == baseWeaponType;
            bool isDebugBase = hasDebugOverride && view.Data.weaponType == debugWeaponType;
            bool isEffectiveOverride = hasBackpackOverride && view.Data.weaponType == effectiveWeaponType;

            if (view.Background != null)
            {
                if (isEffectiveOverride)
                {
                    view.Background.color = effectiveColor;
                }
                else if (isDebugBase)
                {
                    view.Background.color = selectedColor;
                }
                else if (!unlocked)
                {
                    view.Background.color = lockedColor;
                }
                else if (selected)
                {
                    view.Background.color = selectedColor;
                }
                else
                {
                    view.Background.color = normalColor;
                }
            }

            if (view.StateLabel != null)
            {
                if (isEffectiveOverride)
                {
                    view.StateLabel.text = "当前实战墨水";
                }
                else if (isDebugBase)
                {
                    view.StateLabel.text = "调试基础";
                }
                else if (!unlocked)
                {
                    view.StateLabel.text = "未解锁";
                }
                else if (selected)
                {
                    view.StateLabel.text = "使用中";
                }
                else
                {
                    view.StateLabel.text = "点击装备";
                }
            }

            if (view.Button != null)
            {
                view.Button.interactable = unlocked;
            }
        }

        RefreshRuntimeSummary(
            baseWeaponType,
            selectedWeaponType,
            effectiveWeaponType,
            hasDebugOverride,
            hasBackpackOverride,
            overrideCrystal,
            overrideSlotIndex);
    }

    private void RefreshRuntimeSummary(
        WeaponType baseWeaponType,
        WeaponType selectedWeaponType,
        WeaponType effectiveWeaponType,
        bool hasDebugOverride,
        bool hasBackpackOverride,
        ArchitecturalCrystal overrideCrystal,
        int overrideSlotIndex)
    {
        if (runtimeSummaryText == null)
        {
            return;
        }

        string baseWeaponName = InkTypeCatalog.GetDisplayName(baseWeaponType);
        string selectedWeaponName = InkTypeCatalog.GetDisplayName(selectedWeaponType);
        string effectiveWeaponName = InkTypeCatalog.GetDisplayName(effectiveWeaponType);

        if (hasBackpackOverride)
        {
            string overrideDescription = overrideSlotIndex >= 0
                ? $"{overrideCrystal.DisplayName}（背包槽 {overrideSlotIndex + 1}，最后拾取优先）"
                : $"{overrideCrystal.DisplayName}（最后拾取优先）";
            string debugLine = hasDebugOverride ? $"\n调试基础墨水：{selectedWeaponName}" : string.Empty;
            runtimeSummaryText.text =
                $"基础墨水：{baseWeaponName}{debugLine}\n当前实战墨水：{effectiveWeaponName}\n覆盖来源：{overrideDescription}，当前攻击按背包覆盖结果生效。";
            return;
        }

        runtimeSummaryText.text = hasDebugOverride
            ? $"基础墨水：{baseWeaponName}\n调试基础墨水：{selectedWeaponName}\n当前实战墨水：{effectiveWeaponName}\n覆盖来源：调试面板，本次运行临时生效。"
            : $"基础墨水：{baseWeaponName}\n当前实战墨水：{effectiveWeaponName}\n当前攻击按已选墨水生效。";
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
