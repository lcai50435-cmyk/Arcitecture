using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RuntimePhotoCaptureManagerTests
{
    private Scene originalActiveScene;

    [SetUp]
    public void SetUp()
    {
        originalActiveScene = SceneManager.GetActiveScene();
        DestroyPhotoCaptureManagers();
        DestroyEventSystems();
    }

    [TearDown]
    public void TearDown()
    {
        DestroyPhotoCaptureManagers();
        DestroyEventSystems();

        if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(originalActiveScene);
        }
    }

    [Test]
    public void AdditiveNonGameplaySceneKeepsActiveGameplayCaptureContext()
    {
        Assert.AreEqual(
            "GameScene",
            ResolvePreparedSceneName("IllustratedUIScene", LoadSceneMode.Additive, "GameScene"));
    }

    [Test]
    public void SingleLoadedSceneBecomesCaptureContext()
    {
        Assert.AreEqual(
            "NewBase",
            ResolvePreparedSceneName("NewBase", LoadSceneMode.Single, "GameScene"));
    }

    [Test]
    public void BaseSceneSupportsPhotoCapture()
    {
        Assert.IsFalse(GameplayStageCatalog.IsGameplayScene("NewBase"));
        Assert.IsTrue(IsCaptureSupportedScene("NewBase"));
    }

    [Test]
    public void MainMenuDoesNotSupportPhotoCapture()
    {
        Assert.IsFalse(IsCaptureSupportedScene("MainScene"));
    }

    [Test]
    public void BaseSceneCaptureUsesBaseLocationMetadata()
    {
        Assert.AreEqual(string.Empty, ResolveCaptureStageId("NewBase"));
        Assert.AreEqual("基地", ResolveCaptureLocationLabel("NewBase"));
    }

    [Test]
    public void FirstPassSupportsPhotoCapture()
    {
        Assert.IsTrue(GameplayStageCatalog.IsGameplayScene("FirstPass_1"));
        Assert.IsTrue(IsCaptureSupportedScene("FirstPass_1"));
        Assert.AreEqual("stage_01", ResolveCaptureStageId("FirstPass_1"));
        Assert.AreEqual("第一关 · 福建土楼", ResolveCaptureLocationLabel("FirstPass_1"));
    }

    [TestCase("NewBase", "", "基地")]
    [TestCase("FirstPass_1", "stage_01", "第一关 · 福建土楼")]
    public void SaveScreenshotPersistsCaptureAndAlbumIndexForSupportedScene(
        string sceneName,
        string expectedStageId,
        string expectedLocationLabel)
    {
        string tempAlbumDirectory = Path.Combine(
            Path.GetTempPath(),
            "ArcitecturePhotoCaptureTests",
            System.Guid.NewGuid().ToString("N"));
        Texture2D texture = null;

        try
        {
            using (PhotoAlbumRepository.UseAlbumDirectoryForTests(tempAlbumDirectory))
            {
                RuntimePhotoCaptureManager manager = RuntimePhotoCaptureManager.EnsureInstance();
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.red);
                texture.SetPixel(1, 0, Color.green);
                texture.SetPixel(0, 1, Color.blue);
                texture.SetPixel(1, 1, Color.white);
                texture.Apply();

                PhotoAlbumEntry entry = InvokeSaveScreenshot(manager, texture, sceneName);
                Assert.IsNotNull(entry);
                Assert.AreEqual(sceneName, entry.sceneName);
                Assert.AreEqual(expectedStageId, entry.stageId);
                Assert.IsTrue(File.Exists(PhotoAlbumRepository.GetPhotoPath(entry)));
                Assert.IsTrue(PhotoAlbumRepository.HasEntries());

                IReadOnlyList<PhotoAlbumEntry> entries = PhotoAlbumRepository.LoadEntries();
                Assert.AreEqual(1, entries.Count);
                Assert.AreEqual(entry.id, entries[0].id);
                Assert.AreEqual(expectedLocationLabel, ResolveCaptureLocationLabel(sceneName));
            }
        }
        finally
        {
            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }

            if (Directory.Exists(tempAlbumDirectory))
            {
                Directory.Delete(tempAlbumDirectory, true);
            }
        }
    }

    [Test]
    public void EnsureInstanceCreatesEventSystemForPhotoConfirmationButtons()
    {
        Assert.IsNull(Object.FindObjectOfType<EventSystem>(true));

        RuntimePhotoCaptureManager.EnsureInstance();

        EventSystem eventSystem = EventSystem.current ?? Object.FindObjectOfType<EventSystem>(true);
        Assert.IsNotNull(eventSystem);
        Assert.IsTrue(eventSystem.gameObject.activeSelf);
        Assert.IsTrue(eventSystem.enabled);

        BaseInputModule inputModule = eventSystem.GetComponent<BaseInputModule>();
        Assert.IsNotNull(inputModule);
        Assert.IsTrue(inputModule.enabled);
    }

    [Test]
    public void ConfirmationButtonsReceiveRaycastClicks()
    {
        RuntimePhotoCaptureManager manager = RuntimePhotoCaptureManager.EnsureInstance();
        PrepareForScene(manager, "FirstPass_1");
        ShowConfirmationImmediate(manager);

        Button cancelButton = FindButtonByLabel("取消");
        Button saveButton = FindButtonByLabel("保存留念");
        Assert.IsNotNull(cancelButton);
        Assert.IsNotNull(saveButton);
        Assert.IsTrue(cancelButton.IsInteractable());
        Assert.IsTrue(saveButton.IsInteractable());

        AssertButtonReceivesRaycastClick(cancelButton, manager, false);
        ResetPendingDecision(manager);
        AssertButtonReceivesRaycastClick(saveButton, manager, true);
    }

    private static void PrepareForScene(RuntimePhotoCaptureManager manager, string sceneName)
    {
        MethodInfo method = typeof(RuntimePhotoCaptureManager).GetMethod(
            "PrepareForScene",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(string) },
            null);
        Assert.IsNotNull(method);
        method.Invoke(manager, new object[] { sceneName });
    }

    private static string ResolvePreparedSceneName(
        string loadedSceneName,
        LoadSceneMode loadMode,
        string activeSceneName)
    {
        MethodInfo method = typeof(RuntimePhotoCaptureManager).GetMethod(
            "ResolvePreparedSceneName",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[] { typeof(string), typeof(LoadSceneMode), typeof(string) },
            null);
        Assert.IsNotNull(method);

        object resolved = method.Invoke(null, new object[] { loadedSceneName, loadMode, activeSceneName });
        return (string)resolved;
    }

    private static bool IsCaptureSupportedScene(string sceneName)
    {
        MethodInfo method = typeof(RuntimePhotoCaptureManager).GetMethod(
            "IsCaptureSupportedScene",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[] { typeof(string) },
            null);
        Assert.IsNotNull(method);

        object resolved = method.Invoke(null, new object[] { sceneName });
        return (bool)resolved;
    }

    private static string ResolveCaptureStageId(string sceneName)
    {
        return InvokePrivateStringMethod("ResolveCaptureStageId", sceneName);
    }

    private static string ResolveCaptureLocationLabel(string sceneName)
    {
        return InvokePrivateStringMethod("ResolveCaptureLocationLabel", sceneName);
    }

    private static string InvokePrivateStringMethod(string methodName, string sceneName)
    {
        MethodInfo method = typeof(RuntimePhotoCaptureManager).GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[] { typeof(string) },
            null);
        Assert.IsNotNull(method);

        object resolved = method.Invoke(null, new object[] { sceneName });
        return (string)resolved;
    }

    private static PhotoAlbumEntry InvokeSaveScreenshot(
        RuntimePhotoCaptureManager manager,
        Texture2D screenshot,
        string sceneName)
    {
        MethodInfo method = typeof(RuntimePhotoCaptureManager).GetMethod(
            "SaveScreenshot",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(Texture2D), typeof(string) },
            null);
        Assert.IsNotNull(method);

        object savedEntry = method.Invoke(manager, new object[] { screenshot, sceneName });
        return (PhotoAlbumEntry)savedEntry;
    }

    private static void ShowConfirmationImmediate(RuntimePhotoCaptureManager manager)
    {
        MethodInfo setVisibleMethod = typeof(RuntimePhotoCaptureManager).GetMethod(
            "SetConfirmationVisible",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(bool) },
            null);
        Assert.IsNotNull(setVisibleMethod);
        setVisibleMethod.Invoke(manager, new object[] { true });

        Transform confirmRoot = manager.transform.Find("RuntimePhotoCaptureCanvas/ConfirmRoot");
        Assert.IsNotNull(confirmRoot);

        CanvasGroup canvasGroup = confirmRoot.GetComponent<CanvasGroup>();
        Assert.IsNotNull(canvasGroup);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        Canvas.ForceUpdateCanvases();
    }

    private static Button FindButtonByLabel(string label)
    {
        Button[] buttons = Object.FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            TextMeshProUGUI text = buttons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null && text.text == label)
            {
                return buttons[i];
            }
        }

        return null;
    }

    private static void AssertButtonReceivesRaycastClick(
        Button button,
        RuntimePhotoCaptureManager manager,
        bool expectedDecision)
    {
        EventSystem eventSystem = ResolveEventSystem();
        AssertButtonHasClickableRaycastSurface(button);

        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position)
        };
        ExecuteEvents.ExecuteHierarchy(button.gameObject, eventData, ExecuteEvents.pointerClickHandler);

        Assert.AreEqual(expectedDecision, GetPendingDecision(manager));
    }

    private static void AssertButtonHasClickableRaycastSurface(Button button)
    {
        Graphic targetGraphic = button.targetGraphic;
        Assert.IsNotNull(targetGraphic);
        Assert.IsTrue(targetGraphic.raycastTarget);

        TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            Assert.IsFalse(labels[i].raycastTarget);
        }
    }

    private static EventSystem ResolveEventSystem()
    {
        EventSystem eventSystem = EventSystem.current ?? Object.FindObjectOfType<EventSystem>(true);
        Assert.IsNotNull(eventSystem);
        return eventSystem;
    }

    private static bool? GetPendingDecision(RuntimePhotoCaptureManager manager)
    {
        FieldInfo field = typeof(RuntimePhotoCaptureManager).GetField(
            "pendingConfirmDecision",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        return (bool?)field.GetValue(manager);
    }

    private static void ResetPendingDecision(RuntimePhotoCaptureManager manager)
    {
        FieldInfo field = typeof(RuntimePhotoCaptureManager).GetField(
            "pendingConfirmDecision",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(manager, null);
    }

    private static void DestroyPhotoCaptureManagers()
    {
        RuntimePhotoCaptureManager[] managers = Object.FindObjectsOfType<RuntimePhotoCaptureManager>(true);
        for (int i = 0; i < managers.Length; i++)
        {
            Object.DestroyImmediate(managers[i].gameObject);
        }
    }

    private static void DestroyEventSystems()
    {
        EventSystem[] eventSystems = Object.FindObjectsOfType<EventSystem>(true);
        for (int i = 0; i < eventSystems.Length; i++)
        {
            Object.DestroyImmediate(eventSystems[i].gameObject);
        }

        Time.timeScale = 1f;
    }
}
