using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Test]
    public void AncestorCanvasGroupIsKeptWhenRetargetingToChildDetail()
    {
        GameObject parentObject = new GameObject("IllustratedHandbookCanvasUI", typeof(RectTransform), typeof(CanvasGroup));
        GameObject detailObject = new GameObject("DetailInformationFuJianCanvas", typeof(RectTransform), typeof(CanvasGroup));
        try
        {
            detailObject.transform.SetParent(parentObject.transform, false);

            MethodInfo method = typeof(UIRootManager).GetMethod(
                "IsAncestorCanvasGroup",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method);

            bool isAncestor = (bool)method.Invoke(
                null,
                new object[]
                {
                    parentObject.GetComponent<CanvasGroup>(),
                    detailObject.GetComponent<CanvasGroup>()
                });

            Assert.IsTrue(isAncestor);
        }
        finally
        {
            Object.DestroyImmediate(detailObject);
            Object.DestroyImmediate(parentObject);
        }
    }

    [Test]
    public void DetailUiRestoresHiddenScaledCanvasAndBindsCloseButton()
    {
        GameObject parentObject = new GameObject(
            IllustratedHandbookTabsController.RootObjectName,
            typeof(RectTransform),
            typeof(CanvasGroup));
        GameObject detailObject = new GameObject(
            "DetailInformationFuJianCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasGroup),
            typeof(GraphicRaycaster));
        GameObject buttonObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        GameObject dataObject = new GameObject("DetailData");
        try
        {
            detailObject.transform.SetParent(parentObject.transform, false);
            buttonObject.transform.SetParent(detailObject.transform, false);
            detailObject.transform.localScale = Vector3.zero;
            parentObject.SetActive(false);

            DetailedInformationUI detailUi = detailObject.AddComponent<DetailedInformationUI>();
            BuildingDetailData detailData = dataObject.AddComponent<BuildingDetailData>();
            detailData.buildingName = "福建土楼";
            detailData.introduction1 = "建筑介绍";

            detailUi.ShowDetail(detailData);

            Assert.IsTrue(parentObject.activeSelf);
            Assert.IsTrue(detailObject.activeSelf);
            Assert.AreEqual(Vector3.one, detailObject.transform.localScale);
            Assert.AreSame(parentObject, detailUi.illustratedHandbookPanel);
            Assert.IsNotNull(detailUi.closeButton1);

            detailUi.closeButton1.onClick.Invoke();

            Assert.IsFalse(detailObject.activeSelf);
            Assert.IsTrue(parentObject.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(dataObject);
            Object.DestroyImmediate(buttonObject);
            Object.DestroyImmediate(detailObject);
            Object.DestroyImmediate(parentObject);
        }
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
