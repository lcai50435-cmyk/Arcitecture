using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class DialogRuntimeInteractionTests
{
    private readonly System.Collections.Generic.List<GameObject> roots = new System.Collections.Generic.List<GameObject>();
    private readonly System.Collections.Generic.List<Scene> createdScenes = new System.Collections.Generic.List<Scene>();
    private Scene originalActiveScene;

    [SetUp]
    public void SetUp()
    {
        originalActiveScene = SceneManager.GetActiveScene();
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < roots.Count; i++)
        {
            if (roots[i] != null)
            {
                Object.DestroyImmediate(roots[i]);
            }
        }

        roots.Clear();

        if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(originalActiveScene);
        }

        for (int i = createdScenes.Count - 1; i >= 0; i--)
        {
            Scene scene = createdScenes[i];
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        createdScenes.Clear();
    }

    [Test]
    public void DialogSubscribesWhenBackpackAppearsAfterStart()
    {
        DestroyRuntimeBackpackManager();
        DestroyRuntimeDialogs();

        GameObject dialogObject = CreateRoot("DialogRoot");
        Dialog dialog = dialogObject.AddComponent<Dialog>();
        dialog.dialogPanel = CreateRoot("DialogPanel");
        dialog.dialogPanel.transform.SetParent(dialogObject.transform, false);
        dialog.descriptionText = CreateRoot("DescriptionText").AddComponent<Text>();
        dialog.descriptionText.transform.SetParent(dialog.dialogPanel.transform, false);

        InvokePrivate(dialog, "Start");
        Assert.IsFalse(dialog.dialogPanel.activeSelf);

        CreateRoot("BackpackManager").AddComponent<BackpackMananger>();
        InvokePrivate(dialog, "Update");

        ArchitecturalCrystal crystal = ArchitecturalCrystalFactory.CreateCommonStructure(ArchitecturalType.Brackets);
        Assert.IsTrue(BackpackMananger.Instance.PickItem(crystal));

        Assert.IsTrue(dialog.dialogPanel.activeSelf);
        Assert.That(dialog.descriptionText.text, Does.Contain("精灵"));
        Assert.That(dialog.descriptionText.text, Does.Contain("斗拱"));
    }

    [Test]
    public void GameplayFirstPickUsesRuntimeDialogCreatedBeforeBackpack()
    {
        DestroyRuntimeBackpackManager();
        DestroyRuntimeDialogs();

        Scene gameplayScene = CreateScene("FirstPass_1");
        Assert.IsTrue(SceneManager.SetActiveScene(gameplayScene));

        Dialog dialog = Dialog.EnsureGameplayRuntimeInstance();
        Assert.IsNotNull(dialog);
        Assert.IsFalse(dialog.dialogPanel.activeSelf);

        CreateRoot("BackpackManager").AddComponent<BackpackMananger>();

        ArchitecturalCrystal crystal = ArchitecturalCrystalFactory.CreateCommonStructure(ArchitecturalType.Brackets);
        Assert.IsTrue(BackpackMananger.Instance.PickItem(crystal));

        Assert.IsTrue(dialog.dialogPanel.activeSelf);
        Assert.That(dialog.descriptionText.text, Does.Contain("精灵"));
        Assert.That(dialog.descriptionText.text, Does.Contain("斗拱"));
    }

    [Test]
    public void GameplayFirstPickStillCreatesRuntimeDialogWhenSceneDialogWasSubscribed()
    {
        DestroyRuntimeBackpackManager();
        DestroyRuntimeDialogs();

        Scene nonGameplayScene = CreateScene("NewBase");
        Assert.IsTrue(SceneManager.SetActiveScene(nonGameplayScene));

        CreateRoot("BackpackManager").AddComponent<BackpackMananger>();

        GameObject sceneDialogObject = CreateRoot("SceneDialogController");
        Dialog sceneDialog = sceneDialogObject.AddComponent<Dialog>();
        sceneDialog.dialogPanel = CreateRoot("SceneDialogPanel");
        sceneDialog.dialogPanel.transform.SetParent(sceneDialogObject.transform, false);
        sceneDialog.descriptionText = CreateRoot("SceneDescriptionText").AddComponent<Text>();
        sceneDialog.descriptionText.transform.SetParent(sceneDialog.dialogPanel.transform, false);
        InvokePrivate(sceneDialog, "Start");

        Scene gameplayScene = CreateScene("FirstPass_1");
        Assert.IsTrue(SceneManager.SetActiveScene(gameplayScene));

        ArchitecturalCrystal crystal = ArchitecturalCrystalFactory.CreateCommonStructure(ArchitecturalType.Tile);
        Assert.IsTrue(BackpackMananger.Instance.PickItem(crystal));

        Dialog runtimeDialog = Dialog.EnsureGameplayRuntimeInstance();
        Assert.AreEqual("RuntimeDialogController", runtimeDialog.gameObject.name);
        Assert.IsTrue(runtimeDialog.dialogPanel.activeSelf);
        Assert.That(runtimeDialog.descriptionText.text, Does.Contain("精灵"));
        Assert.That(runtimeDialog.descriptionText.text, Does.Contain("瓦片"));
    }

    [Test]
    public void EnsureRuntimeInstanceCreatesTextDialogWhenNoSceneDialogExists()
    {
        DestroyRuntimeDialogs();

        Dialog dialog = Dialog.EnsureRuntimeInstance();
        Assert.IsNotNull(dialog);
        Assert.IsNotNull(dialog.dialogPanel);
        Assert.IsNotNull(dialog.descriptionText);
        Assert.IsNotNull(dialog.clickCloseButton);

        dialog.ShowClickCloseDialog("精灵：发现了通用结构。");

        Assert.IsTrue(dialog.dialogPanel.activeSelf);
        Assert.That(dialog.descriptionText.text, Does.Contain("通用结构"));
    }

    [Test]
    public void ResourceBackedRuntimeDialogUsesCompactIntroductionLayout()
    {
        DestroyRuntimeDialogs();

        Dialog dialog = Dialog.EnsureRuntimeInstance();

        Transform card = dialog.dialogPanel.transform.Find("RuntimeDialogBox");
        Assert.IsNotNull(card);

        RectTransform cardRect = card.GetComponent<RectTransform>();
        Assert.IsNotNull(cardRect);
        Assert.AreEqual(0f, cardRect.anchorMin.y);
        Assert.AreEqual(0f, cardRect.anchorMax.y);
        Assert.AreEqual(0f, cardRect.pivot.y);
        Assert.GreaterOrEqual(cardRect.sizeDelta.x, 1700f);
        Assert.GreaterOrEqual(cardRect.sizeDelta.y, 620f);

        Image cardImage = card.GetComponent<Image>();
        Assert.IsNotNull(cardImage);
        Assert.IsNotNull(cardImage.sprite);
        Assert.AreEqual("DialogBox", cardImage.sprite.texture.name);
        Assert.That(cardImage.color.r, Is.EqualTo(1f).Within(0.001f));
        Assert.That(cardImage.color.g, Is.EqualTo(1f).Within(0.001f));
        Assert.That(cardImage.color.b, Is.EqualTo(1f).Within(0.001f));

        Assert.LessOrEqual(dialog.descriptionText.fontSize, 32);

        RectTransform buttonRect = dialog.clickCloseButton.GetComponent<RectTransform>();
        Assert.IsNotNull(buttonRect);
        Assert.LessOrEqual(buttonRect.sizeDelta.x, 2f);
        Assert.LessOrEqual(buttonRect.sizeDelta.y, 2f);

        Image buttonImage = dialog.clickCloseButton.GetComponent<Image>();
        Assert.IsNotNull(buttonImage);
        Assert.That(buttonImage.color.a, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void EnsureRuntimeInstanceIgnoresDialogWhosePanelParentIsInactive()
    {
        DestroyRuntimeDialogs();

        GameObject staleDialogObject = CreateRoot("StaleDialogController");
        Dialog staleDialog = staleDialogObject.AddComponent<Dialog>();
        GameObject inactiveCanvas = CreateRoot("DialogCanvas");
        inactiveCanvas.SetActive(false);
        staleDialog.dialogPanel = CreateRoot("DialogPanel");
        staleDialog.dialogPanel.transform.SetParent(inactiveCanvas.transform, false);
        staleDialog.descriptionText = CreateRoot("DescriptionText").AddComponent<Text>();
        staleDialog.descriptionText.transform.SetParent(staleDialog.dialogPanel.transform, false);

        Dialog dialog = Dialog.EnsureRuntimeInstance();

        Assert.AreNotSame(staleDialog, dialog);
        Assert.AreEqual("RuntimeDialogController", dialog.gameObject.name);
    }

    [Test]
    public void EnsureGameplayRuntimeInstanceDoesNotReuseSceneDialog()
    {
        DestroyRuntimeDialogs();

        GameObject sceneDialogObject = CreateRoot("SceneDialogController");
        Dialog sceneDialog = sceneDialogObject.AddComponent<Dialog>();
        sceneDialog.dialogPanel = CreateRoot("SceneDialogPanel");
        sceneDialog.dialogPanel.transform.SetParent(sceneDialogObject.transform, false);
        sceneDialog.descriptionText = CreateRoot("SceneDescriptionText").AddComponent<Text>();
        sceneDialog.descriptionText.transform.SetParent(sceneDialog.dialogPanel.transform, false);

        Dialog dialog = Dialog.EnsureGameplayRuntimeInstance();

        Assert.AreNotSame(sceneDialog, dialog);
        Assert.AreEqual("RuntimeDialogController", dialog.gameObject.name);
    }

    [Test]
    public void TopmostRuntimeDialogBlocksRaycastsAndBackdropClickClosesPanel()
    {
        DestroyRuntimeDialogs();

        Dialog dialog = Dialog.EnsureTopmostRuntimeInstance();
        dialog.ShowClickCloseDialog("建筑介绍");

        Assert.IsTrue(dialog.dialogPanel.activeSelf);

        Canvas canvas = dialog.dialogPanel.GetComponent<Canvas>();
        Assert.IsNotNull(canvas);
        Assert.IsTrue(canvas.overrideSorting);
        Assert.Greater(canvas.sortingOrder, RuntimeModalStyle.ModalSortingOrder);

        CanvasGroup canvasGroup = dialog.dialogPanel.GetComponent<CanvasGroup>();
        Assert.IsNotNull(canvasGroup);
        Assert.IsTrue(canvasGroup.interactable);
        Assert.IsTrue(canvasGroup.blocksRaycasts);

        Image backdropImage = dialog.dialogPanel.GetComponent<Image>();
        Assert.IsNotNull(backdropImage);
        Assert.IsTrue(backdropImage.raycastTarget);

        Button backdropButton = dialog.dialogPanel.GetComponent<Button>();
        Assert.IsNotNull(backdropButton);

        backdropButton.onClick.Invoke();

        Assert.IsFalse(dialog.dialogPanel.activeSelf);
    }

    [Test]
    public void UIRootManagerModalRegistrationDoesNotLowerTopmostRuntimeDialog()
    {
        DestroyRuntimeDialogs();

        Dialog dialog = Dialog.EnsureTopmostRuntimeInstance();
        Canvas canvas = dialog.dialogPanel.GetComponent<Canvas>();
        Assert.IsNotNull(canvas);
        Assert.AreEqual(Dialog.TopmostRuntimeDialogSortingOrder, canvas.sortingOrder);

        MethodInfo ensureModalCanvas = typeof(UIRootManager).GetMethod(
            "EnsureModalCanvas",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(ensureModalCanvas);

        Canvas registeredCanvas = (Canvas)ensureModalCanvas.Invoke(null, new object[] { dialog.dialogPanel });

        Assert.AreSame(canvas, registeredCanvas);
        Assert.AreEqual(Dialog.TopmostRuntimeDialogSortingOrder, canvas.sortingOrder);
        Assert.Greater(canvas.sortingOrder, RuntimeModalStyle.ModalSortingOrder);
    }

    [Test]
    public void ModalShellBackdropClickInvokesConfiguredHandler()
    {
        GameObject shellObject = CreateRoot("RuntimeModalShellHost");
        RuntimeModalShell shell = shellObject.AddComponent<RuntimeModalShell>();
        int clickCount = 0;
        shell.SetBackdropClickHandler(() => clickCount++);

        InvokePrivate(shell, "EnsureUi");
        Transform overlay = shellObject.transform.Find("RuntimeModalShellCanvas/Overlay");
        Assert.IsNotNull(overlay);

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventSystemObject = CreateRoot("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        PointerEventData eventData = new PointerEventData(eventSystem);
        ExecuteEvents.Execute<IPointerClickHandler>(
            overlay.gameObject,
            eventData,
            ExecuteEvents.pointerClickHandler);

        Assert.AreEqual(1, clickCount);
    }

    private GameObject CreateRoot(string name)
    {
        GameObject root = new GameObject(name);
        roots.Add(root);
        return root;
    }

    private Scene CreateScene(string sceneName)
    {
        Scene scene = SceneManager.CreateScene(sceneName);
        createdScenes.Add(scene);
        return scene;
    }

    private static void DestroyRuntimeBackpackManager()
    {
        if (BackpackMananger.Instance != null)
        {
            Object.DestroyImmediate(BackpackMananger.Instance.gameObject);
        }
    }

    private static void DestroyRuntimeDialogs()
    {
        Dialog[] dialogs = Object.FindObjectsOfType<Dialog>(true);
        for (int i = 0; i < dialogs.Length; i++)
        {
            if (dialogs[i] != null && dialogs[i].gameObject.name == "RuntimeDialogController")
            {
                Object.DestroyImmediate(dialogs[i].gameObject);
            }
        }
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        method.Invoke(target, null);
    }
}
