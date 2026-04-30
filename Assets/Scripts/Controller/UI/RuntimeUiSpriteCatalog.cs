using UnityEngine;

public sealed class RuntimeUiSpriteCatalog : ScriptableObject
{
    [SerializeField] private Sprite settingPanelFrame = null;
    [SerializeField] private Sprite settingButtonFrame = null;
    [SerializeField] private Sprite mainMenuStartButton = null;
    [SerializeField] private Sprite mainMenuSettingsButton = null;
    [SerializeField] private Sprite mainMenuExitButton = null;
    [SerializeField] private Sprite mainMenuTextButtonFrame = null;

    public Sprite SettingPanelFrame => settingPanelFrame;
    public Sprite SettingButtonFrame => settingButtonFrame;
    public Sprite MainMenuStartButton => mainMenuStartButton;
    public Sprite MainMenuSettingsButton => mainMenuSettingsButton;
    public Sprite MainMenuExitButton => mainMenuExitButton;
    public Sprite MainMenuTextButtonFrame => mainMenuTextButtonFrame;
}
