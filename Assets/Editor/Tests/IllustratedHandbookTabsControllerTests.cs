using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class IllustratedHandbookTabsControllerTests
{
    private GameObject rootObject;

    [TearDown]
    public void TearDown()
    {
        if (rootObject != null)
        {
            Object.DestroyImmediate(rootObject);
        }
    }

    [Test]
    public void SceneBookmarkBindingKeepsAuthoredButtonAndDisablesTransparentBlocker()
    {
        rootObject = new GameObject("ControllerRoot");
        IllustratedHandbookTabsController controller = rootObject.AddComponent<IllustratedHandbookTabsController>();
        GameObject pageRoot = CreateScenePageRoot();
        pageRoot.transform.SetParent(rootObject.transform, false);

        InvokePrivate(controller, "RegisterScenePageButtons", pageRoot);

        Transform handbook = pageRoot.transform.Find("BookMark/HandBook");
        Button authoredButton = handbook.Find("Button").GetComponent<Button>();
        Button hitAreaButton = handbook.Find("SceneBookmarkHitArea").GetComponent<Button>();
        Image blockerImage = pageRoot.transform.Find("BookMark/Panel").GetComponent<Image>();

        Assert.IsNotNull(authoredButton);
        Assert.AreEqual(Selectable.Transition.None, authoredButton.transition);
        Assert.IsTrue(authoredButton.interactable);
        Assert.IsTrue(authoredButton.targetGraphic.raycastTarget);
        Assert.IsNotNull(hitAreaButton);
        Assert.IsTrue(hitAreaButton.interactable);
        Assert.IsTrue(hitAreaButton.targetGraphic.raycastTarget);
        Assert.IsFalse(blockerImage.raycastTarget);
    }

    [Test]
    public void SceneCloseBindingDisablesTransparentBookmarkBlocker()
    {
        rootObject = new GameObject("ControllerRoot");
        IllustratedHandbookTabsController controller = rootObject.AddComponent<IllustratedHandbookTabsController>();
        GameObject pageRoot = CreateScenePageRoot();
        pageRoot.transform.SetParent(rootObject.transform, false);

        InvokePrivate(controller, "BindSceneCloseButton", pageRoot);

        Image blockerImage = pageRoot.transform.Find("BookMark/Panel").GetComponent<Image>();
        Button hitAreaButton = pageRoot.transform.Find("BookMark/Setting/SceneBookmarkHitArea").GetComponent<Button>();

        Assert.IsFalse(blockerImage.raycastTarget);
        Assert.IsNotNull(hitAreaButton);
        Assert.IsTrue(hitAreaButton.interactable);
        Assert.IsTrue(hitAreaButton.targetGraphic.raycastTarget);
    }

    [Test]
    public void DisabledPersonalInformationBookmarkIsHiddenAndCannotOpenPage()
    {
        rootObject = CreateSceneAuthoredRoot();
        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);

        controller.SetPersonalInformationPageAvailable(false);
        controller.SwitchToPage(IllustratedHandbookPage.PersonalInformation);

        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        GameObject personalPage = rootObject.transform.Find("PersonalInformationCanvas").gameObject;
        GameObject personalBookmark = rootObject.transform.Find("IllustratedHandbookCanvas/BookMark/PersonalInformation").gameObject;

        Assert.IsTrue(handbookPage.activeSelf);
        Assert.IsFalse(personalPage.activeSelf);
        Assert.IsFalse(personalBookmark.activeSelf);
    }

    private static GameObject CreateScenePageRoot()
    {
        return CreateScenePageRoot("IllustratedHandbookCanvas");
    }

    private static GameObject CreateSceneAuthoredRoot()
    {
        GameObject sceneRoot = new GameObject(IllustratedHandbookTabsController.RootObjectName, typeof(RectTransform));
        CreateScenePageRoot("IllustratedHandbookCanvas").transform.SetParent(sceneRoot.transform, false);
        CreateScenePageRoot("PersonalInformationCanvas").transform.SetParent(sceneRoot.transform, false);
        CreateScenePageRoot("PhotoAlbumCanvas").transform.SetParent(sceneRoot.transform, false);
        CreateScenePageRoot("SettingCanvas").transform.SetParent(sceneRoot.transform, false);
        return sceneRoot;
    }

    private static GameObject CreateScenePageRoot(string pageName)
    {
        GameObject pageRoot = new GameObject(pageName, typeof(RectTransform));
        GameObject bookmarkRoot = new GameObject("BookMark", typeof(RectTransform));
        bookmarkRoot.transform.SetParent(pageRoot.transform, false);

        CreateBookmark("HandBook", bookmarkRoot.transform);
        CreateBookmark("PersonalInformation", bookmarkRoot.transform);
        CreateBookmark("PhotoAlbum", bookmarkRoot.transform);
        CreateBookmark("Mission", bookmarkRoot.transform);
        CreateBookmark("Setting", bookmarkRoot.transform);

        GameObject transparentPanel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        transparentPanel.transform.SetParent(bookmarkRoot.transform, false);
        Image panelImage = transparentPanel.GetComponent<Image>();
        panelImage.color = Color.clear;
        panelImage.raycastTarget = true;

        return pageRoot;
    }

    private static void CreateBookmark(string name, Transform parent)
    {
        GameObject bookmark = new GameObject(name, typeof(RectTransform), typeof(Image));
        bookmark.transform.SetParent(parent, false);

        GameObject authoredButton = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        authoredButton.transform.SetParent(bookmark.transform, false);

        Image buttonImage = authoredButton.GetComponent<Image>();
        buttonImage.color = Color.clear;
        buttonImage.raycastTarget = true;
        authoredButton.GetComponent<Button>().targetGraphic = buttonImage;
    }

    private static void InvokePrivate(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        method.Invoke(target, args);
    }
}
