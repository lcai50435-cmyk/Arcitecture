using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class UIRootManagerTests
{
    private Scene originalActiveScene;
    private Scene createdScene;

    [SetUp]
    public void SetUp()
    {
        originalActiveScene = SceneManager.GetActiveScene();
    }

    [TearDown]
    public void TearDown()
    {
        if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(originalActiveScene);
        }

        if (createdScene.IsValid() && createdScene.isLoaded)
        {
            EditorSceneManager.CloseScene(createdScene, true);
        }
    }

    [Test]
    public void HandbookHotkeyOpensPhotoAlbumWhenPhotosExist()
    {
        Assert.AreEqual(IllustratedHandbookPage.PhotoAlbum, ResolveHandbookHotkeyPage(true));
    }

    [Test]
    public void HandbookHotkeyKeepsPersonalInfoFallbackWithoutPhotos()
    {
        Assert.AreEqual(IllustratedHandbookPage.PersonalInformation, ResolveHandbookHotkeyPage(false));
    }

    [Test]
    public void GameSceneBootstrapperCreatesRuntimeUiRootForFirstPass()
    {
        createdScene = SceneManager.CreateScene("FirstPass_1");
        Assert.IsTrue(SceneManager.SetActiveScene(createdScene));

        GameObject bootstrapperObject = new GameObject("GameSceneBaseReturnUI");
        SceneManager.MoveGameObjectToScene(bootstrapperObject, createdScene);
        GameSceneBaseReturnBootstrapper bootstrapper = bootstrapperObject.AddComponent<GameSceneBaseReturnBootstrapper>();

        MethodInfo buildMethod = typeof(GameSceneBaseReturnBootstrapper).GetMethod(
            "Build",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(buildMethod);

        buildMethod.Invoke(bootstrapper, null);

        UIRootManager rootManager = Object.FindObjectOfType<UIRootManager>(true);
        Assert.IsNotNull(rootManager);
        Assert.AreEqual(createdScene, rootManager.gameObject.scene);
    }

    [Test]
    public void RefreshRuntimeBindingsKeepsGameplaySpiritPanel()
    {
        createdScene = SceneManager.CreateScene("FirstPass_1");
        Assert.IsTrue(SceneManager.SetActiveScene(createdScene));

        GameObject rootObject = new GameObject("RuntimeUIRootManager");
        SceneManager.MoveGameObjectToScene(rootObject, createdScene);
        UIRootManager rootManager = rootObject.AddComponent<UIRootManager>();

        GameObject panelObject = new GameObject("SpiritPanel", typeof(RectTransform), typeof(CanvasGroup));
        SceneManager.MoveGameObjectToScene(panelObject, createdScene);
        panelObject.AddComponent<SpiritPanelUI>();

        rootManager.RefreshRuntimeBindings();

        Assert.IsNotNull(rootManager.spiritPanelUI);
        Assert.AreSame(panelObject, rootManager.spiritPanelUI.gameObject);
    }

    private static IllustratedHandbookPage ResolveHandbookHotkeyPage(bool hasPhotoEntries)
    {
        MethodInfo method = typeof(UIRootManager).GetMethod(
            "ResolveHandbookHotkeyPage",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[] { typeof(bool) },
            null);
        Assert.IsNotNull(method);

        object resolved = method.Invoke(null, new object[] { hasPhotoEntries });
        return (IllustratedHandbookPage)resolved;
    }
}
