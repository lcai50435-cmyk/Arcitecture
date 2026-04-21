using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectionPanelUI : MonoBehaviour
{
    private readonly List<WeaponOptionView> optionViews = new List<WeaponOptionView>();

    [SerializeField] private Color selectedColor = new Color(0.86f, 0.67f, 0.34f, 1f);
    [SerializeField] private Color normalColor = new Color(0.18f, 0.15f, 0.12f, 0.92f);
    [SerializeField] private Color lockedColor = new Color(0.14f, 0.14f, 0.14f, 0.56f);

    private PlayerProfileData profileData;

    public void Bind(PlayerProfileData profile)
    {
        profileData = profile;
        RefreshSelected();
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
            RefreshSelected();
        });
    }

    public void RefreshSelected()
    {
        if (profileData == null) return;

        PlayerLoadoutRuntime.EnsureCurrentWeaponUnlocked();
        profileData.currentWeaponType = PlayerLoadoutRuntime.CurrentWeaponType;
        profileData.currentInkType = PlayerLoadoutRuntime.CurrentInkType;

        foreach (WeaponOptionView view in optionViews)
        {
            bool unlocked = PlayerLoadoutRuntime.IsWeaponUnlocked(view.Data.weaponType);
            bool selected = view.Data.weaponType == profileData.currentWeaponType;

            if (view.Background != null)
            {
                view.Background.color = !unlocked
                    ? lockedColor
                    : selected
                        ? selectedColor
                        : normalColor;
            }

            if (view.StateLabel != null)
            {
                view.StateLabel.text = !unlocked
                    ? "未解锁"
                    : selected
                        ? "当前装备"
                        : "点击装备";
            }

            if (view.Button != null)
            {
                view.Button.interactable = unlocked;
            }
        }
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
