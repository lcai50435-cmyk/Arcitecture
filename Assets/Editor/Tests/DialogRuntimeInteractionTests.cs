using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DialogRuntimeInteractionTests
{
    private readonly System.Collections.Generic.List<GameObject> roots = new System.Collections.Generic.List<GameObject>();

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

        Transform card = dialog.dialogPanel.transform.Find("RuntimeDialogTextArea");
        Assert.IsNotNull(card);

        RectTransform cardRect = card.GetComponent<RectTransform>();
        Assert.IsNotNull(cardRect);
        Assert.LessOrEqual(cardRect.sizeDelta.x, 900f);
        Assert.LessOrEqual(cardRect.sizeDelta.y, 360f);

        Assert.LessOrEqual(dialog.descriptionText.fontSize, 32);

        RectTransform buttonRect = dialog.clickCloseButton.GetComponent<RectTransform>();
        Assert.IsNotNull(buttonRect);
        Assert.LessOrEqual(buttonRect.sizeDelta.x, 160f);
        Assert.LessOrEqual(buttonRect.sizeDelta.y, 60f);
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
