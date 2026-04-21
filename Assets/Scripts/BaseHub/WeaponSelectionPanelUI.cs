using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectionPanelUI : MonoBehaviour
{
    private readonly List<WeaponOptionView> optionViews = new List<WeaponOptionView>();

    [SerializeField] private Color selectedColor = new Color(0.86f, 0.67f, 0.34f, 1f);
    [SerializeField] private Color normalColor = new Color(0.18f, 0.15f, 0.12f, 0.92f);

    private PlayerProfileData profileData;

    public void Bind(PlayerProfileData profile)
    {
        profileData = profile;
        RefreshSelected();
    }

    public void RegisterOption(
        WeaponOptionData data,
        Button button,
        Image background,
        TextMeshProUGUI stateLabel)
    {
        if (data == null || button == null) return;

        WeaponOptionView view = new WeaponOptionView(data, background, stateLabel);
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

        foreach (WeaponOptionView view in optionViews)
        {
            bool selected = view.Data.weaponType == profileData.currentWeaponType;

            if (view.Background != null)
                view.Background.color = selected ? selectedColor : normalColor;

            if (view.StateLabel != null)
                view.StateLabel.text = selected ? "当前装备" : "点击装备";
        }
    }

    private sealed class WeaponOptionView
    {
        public readonly WeaponOptionData Data;
        public readonly Image Background;
        public readonly TextMeshProUGUI StateLabel;

        public WeaponOptionView(WeaponOptionData data, Image background, TextMeshProUGUI stateLabel)
        {
            Data = data;
            Background = background;
            StateLabel = stateLabel;
        }
    }
}
