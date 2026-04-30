using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class RuntimeSettingsPanelMaterialTests
{
    [TearDown]
    public void TearDown()
    {
        if (RuntimeSettingsPanel.Instance != null)
        {
            Object.DestroyImmediate(RuntimeSettingsPanel.Instance.gameObject);
        }
    }

    [Test]
    public void RuntimeSettingsPanelUsesSettingSceneFrameSprites()
    {
        RuntimeSettingsPanel panel = RuntimeSettingsPanel.EnsureInstance();

        Transform canvas = panel.transform.Find("RuntimeSettingsPanelCanvas");
        Assert.IsNotNull(canvas);

        Image panelImage = canvas.Find("Panel")?.GetComponent<Image>();
        Assert.IsNotNull(panelImage);
        Assert.IsNotNull(panelImage.sprite);
        Assert.AreEqual("SettingPanelFrameSprite", panelImage.sprite.name);

        Image applyButtonImage = canvas.Find("Panel/Footer/ApplyButton")?.GetComponent<Image>();
        Assert.IsNotNull(applyButtonImage);
        Assert.IsNotNull(applyButtonImage.sprite);
        Assert.AreEqual("SettingButtonFrameSprite", applyButtonImage.sprite.name);
    }
}
