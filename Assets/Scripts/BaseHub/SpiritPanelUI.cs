using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SpiritPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject statsPage;
    [SerializeField] private GameObject weaponPage;
    [SerializeField] private Button statsTabButton;
    [SerializeField] private Button weaponTabButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private PlayerStatsPanelUI statsPanel;
    [SerializeField] private WeaponSelectionPanelUI weaponPanel;

    private CharacterCore characterCore;
    private PlayerProfileData profileData;

    public void Configure(
        GameObject statsContent,
        GameObject weaponContent,
        Button statsButton,
        Button weaponButton,
        Button close,
        TextMeshProUGUI title,
        PlayerStatsPanelUI stats,
        WeaponSelectionPanelUI weapons)
    {
        statsPage = statsContent;
        weaponPage = weaponContent;
        statsTabButton = statsButton;
        weaponTabButton = weaponButton;
        closeButton = close;
        titleText = title;
        statsPanel = stats;
        weaponPanel = weapons;

        statsTabButton?.onClick.AddListener(ShowStatsPage);
        weaponTabButton?.onClick.AddListener(ShowWeaponPage);
    }

    public void Bind(CharacterCore core, PlayerProfileData profile)
    {
        characterCore = core;
        profileData = profile;
        statsPanel?.Bind(core, profile);
        weaponPanel?.Bind(profile);
    }

    public void SetCloseAction(UnityAction closeAction)
    {
        if (closeButton == null) return;

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(closeAction);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        ShowStatsPage();
        statsPanel?.Refresh();
        weaponPanel?.RefreshSelected();
    }

    public void ShowStatsPage()
    {
        SetPage(true);
        if (titleText != null) titleText.text = "精灵 · 玩家属性";
    }

    public void ShowWeaponPage()
    {
        SetPage(false);
        if (titleText != null) titleText.text = "精灵 · 武器选择";
    }

    private void SetPage(bool showStats)
    {
        if (statsPage != null) statsPage.SetActive(showStats);
        if (weaponPage != null) weaponPage.SetActive(!showStats);
    }
}
