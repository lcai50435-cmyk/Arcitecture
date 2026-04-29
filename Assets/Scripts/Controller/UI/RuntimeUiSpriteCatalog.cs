using UnityEngine;

public sealed class RuntimeUiSpriteCatalog : ScriptableObject
{
    [SerializeField] private Sprite settingPanelFrame = null;
    [SerializeField] private Sprite settingButtonFrame = null;

    public Sprite SettingPanelFrame => settingPanelFrame;
    public Sprite SettingButtonFrame => settingButtonFrame;
}
