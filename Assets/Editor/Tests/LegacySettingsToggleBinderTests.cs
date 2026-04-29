using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class LegacySettingsToggleBinderTests
{
    private GameObject rootObject;

    [SetUp]
    public void SetUp()
    {
        SetDefaultAudioToggles();
    }

    [TearDown]
    public void TearDown()
    {
        if (rootObject != null)
        {
            Object.DestroyImmediate(rootObject);
        }

        if (MusicManager.Instance != null)
        {
            Object.DestroyImmediate(MusicManager.Instance.gameObject);
        }

        SetDefaultAudioToggles();
    }

    [Test]
    public void BindKeepsToggleVisualVisibleAndPersistsState()
    {
        rootObject = CreateSettingsRoot();
        Toggle muteToggle = CreateVoiceToggle("Text (TMP)_1");
        Image stateImage = muteToggle.GetComponentInChildren<Image>();
        LegacySettingsToggleBinder binder = rootObject.AddComponent<LegacySettingsToggleBinder>();

        GameSettingsStore.SetAudioToggle(GameAudioToggle.MuteMode, false, false);
        binder.Bind();

        Assert.IsFalse(muteToggle.isOn);
        Assert.IsNull(muteToggle.graphic);
        Assert.AreSame(stateImage, muteToggle.targetGraphic);
        Assert.IsTrue(stateImage.enabled);
        Assert.AreEqual(1f, stateImage.color.a, 0.001f);

        muteToggle.isOn = true;

        Assert.IsTrue(GameSettingsStore.GetAudioToggle(GameAudioToggle.MuteMode));
        Assert.IsTrue(stateImage.enabled);
        Assert.AreEqual(1f, stateImage.color.a, 0.001f);

        muteToggle.isOn = false;

        Assert.IsFalse(GameSettingsStore.GetAudioToggle(GameAudioToggle.MuteMode));
        Assert.IsTrue(stateImage.enabled);
        Assert.AreEqual(1f, stateImage.color.a, 0.001f);
    }

    private static void SetDefaultAudioToggles()
    {
        GameSettingsStore.SetAudioToggle(GameAudioToggle.MuteMode, false, false);
        GameSettingsStore.SetAudioToggle(GameAudioToggle.MusicCrossfade, true, false);
        GameSettingsStore.SetAudioToggle(GameAudioToggle.SfxDynamicRange, false, false);
        GameSettingsStore.SetAudioToggle(GameAudioToggle.SpatialAudio, false, false);
    }

    private GameObject CreateSettingsRoot()
    {
        rootObject = new GameObject("SettingCanvas", typeof(RectTransform));
        EnsurePath(rootObject.transform, "Panel/BackGround/LeftPanel/VoiceSetting");
        return rootObject;
    }

    private Toggle CreateVoiceToggle(string rowName)
    {
        Transform voiceRoot = rootObject.transform.Find("Panel/BackGround/LeftPanel/VoiceSetting");
        GameObject rowObject = new GameObject(rowName, typeof(RectTransform));
        rowObject.transform.SetParent(voiceRoot, false);

        GameObject toggleObject = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
        toggleObject.transform.SetParent(rowObject.transform, false);

        GameObject imageObject = new GameObject("State", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(toggleObject.transform, false);

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.graphic = imageObject.GetComponent<Image>();
        return toggle;
    }

    private static Transform EnsurePath(Transform root, string path)
    {
        Transform current = root;
        string[] segments = path.Split('/');
        for (int i = 0; i < segments.Length; i++)
        {
            Transform child = current.Find(segments[i]);
            if (child == null)
            {
                GameObject childObject = new GameObject(segments[i], typeof(RectTransform));
                childObject.transform.SetParent(current, false);
                child = childObject.transform;
            }

            current = child;
        }

        return current;
    }
}
