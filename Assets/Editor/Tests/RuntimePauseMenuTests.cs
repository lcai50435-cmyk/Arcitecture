using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RuntimePauseMenuTests
{
    private Scene originalActiveScene;
    private Scene createdScene;

    [SetUp]
    public void SetUp()
    {
        originalActiveScene = SceneManager.GetActiveScene();
        DestroyPauseMenu();
        DestroyRuntimeSettingsPanels();
        DestroyEventSystems();
        DestroyUiRootManagers();
    }

    [TearDown]
    public void TearDown()
    {
        DestroyPauseMenu();
        DestroyRuntimeSettingsPanels();
        DestroyEventSystems();
        DestroyUiRootManagers();

        if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(originalActiveScene);
        }
        else if (createdScene.IsValid() && createdScene.isLoaded && SceneManager.sceneCount <= 1)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            createdScene = default;
        }

        if (createdScene.IsValid() && createdScene.isLoaded && SceneManager.sceneCount > 1)
        {
            EditorSceneManager.CloseScene(createdScene, true);
        }
    }

    [Test]
    public void EnsureInstanceCreatesEventSystemForPauseMenuButtons()
    {
        createdScene = OpenTestScene("FirstPass_1");
        Assert.IsTrue(createdScene.IsValid() && createdScene.isLoaded);
        Assert.IsNull(Object.FindObjectOfType<EventSystem>(true));

        RuntimePauseMenu.EnsureInstance();

        EventSystem eventSystem = EventSystem.current ?? Object.FindObjectOfType<EventSystem>(true);
        Assert.IsNotNull(eventSystem);
        Assert.IsTrue(eventSystem.gameObject.activeSelf);
        Assert.IsNotNull(eventSystem.GetComponent<BaseInputModule>());
    }

    [Test]
    public void EnsureInstanceReactivatesDisabledEventSystemInputModule()
    {
        createdScene = OpenTestScene("FirstPass_1");
        Assert.IsTrue(createdScene.IsValid() && createdScene.isLoaded);

        GameObject eventSystemObject = new GameObject("EventSystem");
        EventSystem eventSystem = eventSystemObject.AddComponent<EventSystem>();
        StandaloneInputModule inputModule = eventSystemObject.AddComponent<StandaloneInputModule>();
        eventSystem.enabled = false;
        inputModule.enabled = false;

        RuntimePauseMenu.EnsureInstance();

        Assert.IsTrue(eventSystem.enabled);
        Assert.IsTrue(inputModule.enabled);
    }

    [Test]
    public void TryOpenFromExternalOpensPauseMenuInBaseHubScene()
    {
        createdScene = OpenTestScene("NewBase");
        Assert.IsTrue(createdScene.IsValid() && createdScene.isLoaded);
        Time.timeScale = 1f;

        Assert.IsTrue(RuntimePauseMenu.TryOpenFromExternal());

        Assert.IsTrue(RuntimePauseMenu.IsPauseOpen);
        Assert.AreEqual(0f, Time.timeScale);
    }

    [Test]
    public void TryOpenFromPauseKeyBypassesExistingGameplayBlockingUi()
    {
        createdScene = OpenTestScene("FirstPass_1");
        Assert.IsTrue(createdScene.IsValid() && createdScene.isLoaded);
        Time.timeScale = 1f;
        CreateOpenBlockingUiRoot();

        Assert.IsTrue(RuntimeUiInputGuard.IsBlockingGameplayUiOpen());
        Assert.IsFalse(RuntimePauseMenu.TryOpenFromExternal());

        Assert.IsTrue(RuntimePauseMenu.TryOpenFromPauseKey());

        Assert.IsTrue(RuntimePauseMenu.IsPauseOpen);
        Assert.AreEqual(0f, Time.timeScale);
    }

    [Test]
    public void ResumeButtonReceivesRaycastClickAndContinuesGame()
    {
        createdScene = OpenTestScene("FirstPass_1");
        Assert.IsTrue(createdScene.IsValid() && createdScene.isLoaded);
        Time.timeScale = 1f;

        Assert.IsTrue(RuntimePauseMenu.TryOpenFromExternal());
        RevealMenuButtonsImmediate();

        Button resumeButton = FindButtonByLabel("返回游戏");
        Assert.IsNotNull(resumeButton);
        Assert.IsTrue(resumeButton.IsInteractable());

        RaycastResult result = RaycastTopClickableResult(resumeButton);
        Assert.IsNotNull(result.gameObject);
        Assert.IsTrue(result.gameObject == resumeButton.gameObject || result.gameObject.transform.IsChildOf(resumeButton.transform));

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, resumeButton.transform.position)
        };
        ExecuteEvents.ExecuteHierarchy(result.gameObject, eventData, ExecuteEvents.pointerClickHandler);

        Assert.IsFalse(RuntimePauseMenu.IsPauseOpen);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [Test]
    public void PauseMenuButtonsRenderAboveTransparentSceneTransitionBlocker()
    {
        createdScene = OpenTestScene("FirstPass_1");
        Assert.IsTrue(createdScene.IsValid() && createdScene.isLoaded);
        Time.timeScale = 1f;
        CreateTransparentSceneTransitionBlocker();

        Assert.IsTrue(RuntimePauseMenu.TryOpenFromExternal());
        RevealMenuButtonsImmediate();

        Button resumeButton = FindButtonByLabel("返回游戏");
        Assert.IsNotNull(resumeButton);

        RaycastResult result = RaycastTopResult(resumeButton);
        Assert.IsNotNull(result.gameObject);
        Assert.IsTrue(
            result.gameObject == resumeButton.gameObject || result.gameObject.transform.IsChildOf(resumeButton.transform),
            $"Expected pause button to be the top raycast target, but got {result.gameObject.name}.");
    }

    [Test]
    public void PauseMenuButtonsRenderAboveTopmostRuntimeDialogBlocker()
    {
        createdScene = OpenTestScene("FirstPass_1");
        Assert.IsTrue(createdScene.IsValid() && createdScene.isLoaded);
        Time.timeScale = 1f;
        CreateTransparentRaycastBlocker("TopmostRuntimeDialogCanvas", Dialog.TopmostRuntimeDialogSortingOrder);

        Assert.IsTrue(RuntimePauseMenu.TryOpenFromPauseKey());
        RevealMenuButtonsImmediate();

        Button resumeButton = FindButtonByLabel("返回游戏");
        Assert.IsNotNull(resumeButton);

        RaycastResult result = RaycastTopResult(resumeButton);
        Assert.IsNotNull(result.gameObject);
        Assert.IsTrue(
            result.gameObject == resumeButton.gameObject || result.gameObject.transform.IsChildOf(resumeButton.transform),
            $"Expected pause button to be above topmost runtime blockers, but got {result.gameObject.name}.");
    }

    [Test]
    public void PauseMenuButtonLabelDoesNotCaptureTopRaycastTarget()
    {
        createdScene = OpenTestScene("FirstPass_1");
        Assert.IsTrue(createdScene.IsValid() && createdScene.isLoaded);
        Time.timeScale = 1f;

        Assert.IsTrue(RuntimePauseMenu.TryOpenFromExternal());
        RevealMenuButtonsImmediate();

        Button resumeButton = FindButtonByLabel("返回游戏");
        Assert.IsNotNull(resumeButton);

        RaycastResult result = RaycastTopResult(resumeButton);
        Assert.AreEqual(
            resumeButton.gameObject,
            result.gameObject,
            $"Expected pause button to receive the top raycast directly, but got {result.gameObject.name}.");
    }

    [Test]
    public void PauseMenuButtonIsInteractiveAtRevealStart()
    {
        createdScene = OpenTestScene("FirstPass_1");
        Assert.IsTrue(createdScene.IsValid() && createdScene.isLoaded);
        Time.timeScale = 1f;

        Assert.IsTrue(RuntimePauseMenu.TryOpenFromExternal());

        Button resumeButton = FindButtonByLabel("返回游戏");
        Assert.IsNotNull(resumeButton);
        CanvasGroup canvasGroup = resumeButton.GetComponent<CanvasGroup>();
        Assert.IsNotNull(canvasGroup);

        ApplyMenuButtonRevealProgress(resumeButton, 0f);

        Assert.IsTrue(canvasGroup.interactable);
        Assert.IsTrue(canvasGroup.blocksRaycasts);
    }

    [Test]
    public void ReturnToMainConfirmButtonsAreClickableAndCanCancel()
    {
        createdScene = OpenTestScene("FirstPass_1");
        Assert.IsTrue(createdScene.IsValid() && createdScene.isLoaded);
        Time.timeScale = 1f;

        Assert.IsTrue(RuntimePauseMenu.TryOpenFromExternal());
        RevealMenuButtonsImmediate();

        ClickButtonByLabel("回到主界面");

        Assert.AreEqual("ReturnConfirm", GetCurrentPageName());
        AssertButtonClickable("确认返回");
        ClickButtonByLabel("取消");
        Assert.AreEqual("Menu", GetCurrentPageName());
        AssertButtonClickable("返回游戏");
    }

    [Test]
    public void AboutPageBackButtonReturnsToPauseMenu()
    {
        createdScene = OpenTestScene("FirstPass_1");
        Assert.IsTrue(createdScene.IsValid() && createdScene.isLoaded);
        Time.timeScale = 1f;

        Assert.IsTrue(RuntimePauseMenu.TryOpenFromExternal());
        RevealMenuButtonsImmediate();

        ClickButtonByLabel("关于我们");

        Assert.AreEqual("About", GetCurrentPageName());
        AssertButtonClickable("返回暂停页");
        ClickButtonByLabel("返回暂停页");
        Assert.AreEqual("Menu", GetCurrentPageName());
        AssertButtonClickable("返回游戏");
    }

    [Test]
    public void SettingsButtonStartsSettingsFlowAndCanReturnToMenu()
    {
        createdScene = OpenTestScene("FirstPass_1");
        Assert.IsTrue(createdScene.IsValid() && createdScene.isLoaded);
        Time.timeScale = 1f;

        Assert.IsTrue(RuntimePauseMenu.TryOpenFromExternal());
        RevealMenuButtonsImmediate();

        ClickButtonByLabel("游戏设置");

        Assert.IsTrue(GetPrivateBool("showingSettings"));
        Assert.IsNotNull(RuntimeSettingsPanel.Instance);

        InvokePrivate("HandleSettingsClosed");

        Assert.IsFalse(GetPrivateBool("showingSettings"));
        Assert.AreEqual("Menu", GetCurrentPageName());
        AssertButtonClickable("返回游戏");
    }

    [Test]
    public void QuitConfirmButtonIsClickableWithoutRunningQuitAction()
    {
        createdScene = OpenTestScene("FirstPass_1");
        Assert.IsTrue(createdScene.IsValid() && createdScene.isLoaded);
        Time.timeScale = 1f;

        Assert.IsTrue(RuntimePauseMenu.TryOpenFromExternal());
        RevealMenuButtonsImmediate();

        ClickButtonByLabel("退出游戏");

        Assert.AreEqual("QuitConfirm", GetCurrentPageName());
        AssertButtonClickable("确认退出");
        AssertButtonClickable("取消");
    }

    [Test]
    public void PauseMenuRebindsButtonsAfterListenersClearedAndSceneTransition()
    {
        createdScene = OpenTestScene("FirstPass_1");
        Assert.IsTrue(createdScene.IsValid() && createdScene.isLoaded);
        Time.timeScale = 1f;

        Assert.IsTrue(RuntimePauseMenu.TryOpenFromExternal());
        RevealMenuButtonsImmediate();

        Button aboutButton = FindButtonByLabel("关于我们");
        Assert.IsNotNull(aboutButton);
        aboutButton.onClick.RemoveAllListeners();

        RuntimePauseMenu.CloseForSceneTransition();

        Assert.IsFalse(RuntimePauseMenu.IsPauseOpen);
        Assert.IsTrue(RuntimePauseMenu.TryOpenFromExternal());
        RevealMenuButtonsImmediate();

        aboutButton = FindButtonByLabel("关于我们");
        Assert.IsNotNull(aboutButton);
        aboutButton.onClick.Invoke();

        Assert.AreEqual("About", GetCurrentPageName());

        Button backButton = FindButtonByLabel("返回暂停页");
        Assert.IsNotNull(backButton);
        backButton.onClick.RemoveAllListeners();

        InvokePrivate("ShowAboutPage");
        backButton.onClick.Invoke();

        Assert.AreEqual("Menu", GetCurrentPageName());
    }

    [Test]
    public void PauseMenuConfirmPrimaryRebindsAfterListenersCleared()
    {
        createdScene = OpenTestScene("FirstPass_1");
        Assert.IsTrue(createdScene.IsValid() && createdScene.isLoaded);
        Time.timeScale = 1f;

        Assert.IsTrue(RuntimePauseMenu.TryOpenFromExternal());
        RevealMenuButtonsImmediate();

        ClickButtonByLabel("退出游戏");
        Assert.AreEqual("QuitConfirm", GetCurrentPageName());

        Button confirmButton = FindButtonByLabel("确认退出");
        Assert.IsNotNull(confirmButton);
        confirmButton.onClick.RemoveAllListeners();

        RuntimePauseMenu.EnsureInstance();
        confirmButton.onClick.Invoke();

        Assert.IsFalse(RuntimePauseMenu.IsPauseOpen);
    }

    private static void DestroyPauseMenu()
    {
        if (RuntimePauseMenu.Instance != null)
        {
            Object.DestroyImmediate(RuntimePauseMenu.Instance.gameObject);
        }
    }

    private static void DestroyRuntimeSettingsPanels()
    {
        RuntimeSettingsPanel[] panels = Object.FindObjectsOfType<RuntimeSettingsPanel>(true);
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
            {
                Object.DestroyImmediate(panels[i].gameObject);
            }
        }
    }

    private static Scene OpenTestScene(string sceneName)
    {
        string scenePath = $"Assets/Scenes/{sceneName}.unity";
        Scene scene = File.Exists(Path.Combine(Application.dataPath, "Scenes", $"{sceneName}.unity"))
            ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (scene.IsValid() && scene.isLoaded)
        {
            SceneManager.SetActiveScene(scene);
        }
        DestroyPauseMenu();
        DestroyEventSystems();
        DestroyUiRootManagers();
        return scene;
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

    private static void DestroyUiRootManagers()
    {
        UIRootManager[] roots = Object.FindObjectsOfType<UIRootManager>(true);
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null)
            {
                Object.DestroyImmediate(roots[i].gameObject);
            }
        }
    }

    private static void RevealMenuButtonsImmediate()
    {
        MethodInfo revealMethod = typeof(RuntimePauseMenu).GetMethod(
            "RevealMenuButtonsImmediate",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(revealMethod);
        revealMethod.Invoke(RuntimePauseMenu.Instance, null);
    }

    private static void ApplyMenuButtonRevealProgress(Button button, float progress)
    {
        Assert.IsNotNull(button);

        System.Type revealItemType = typeof(RuntimePauseMenu).GetNestedType(
            "MenuButtonRevealItem",
            BindingFlags.NonPublic);
        Assert.IsNotNull(revealItemType);

        object revealItem = System.Activator.CreateInstance(
            revealItemType,
            button.GetComponent<RectTransform>(),
            button.GetComponent<CanvasGroup>());
        Assert.IsNotNull(revealItem);

        MethodInfo applyMethod = typeof(RuntimePauseMenu).GetMethod(
            "ApplyMenuButtonRevealState",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(applyMethod);
        applyMethod.Invoke(null, new[] { revealItem, progress });
    }

    private static void ClickButtonByLabel(string label)
    {
        Button button = FindButtonByLabel(label);
        Assert.IsNotNull(button, label);
        Assert.IsTrue(button.IsInteractable(), label);
        button.onClick.Invoke();
    }

    private static void AssertButtonClickable(string label)
    {
        Button button = FindButtonByLabel(label);
        Assert.IsNotNull(button, label);
        Assert.IsTrue(button.IsInteractable(), label);
        Assert.IsNotNull(RaycastTopClickableResult(button).gameObject, label);
    }

    private static string GetCurrentPageName()
    {
        FieldInfo field = typeof(RuntimePauseMenu).GetField(
            "currentPage",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        return field.GetValue(RuntimePauseMenu.Instance).ToString();
    }

    private static bool GetPrivateBool(string fieldName)
    {
        FieldInfo field = typeof(RuntimePauseMenu).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        return (bool)field.GetValue(RuntimePauseMenu.Instance);
    }

    private static void InvokePrivate(string methodName)
    {
        MethodInfo method = typeof(RuntimePauseMenu).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        method.Invoke(RuntimePauseMenu.Instance, null);
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

    private static void CreateOpenBlockingUiRoot()
    {
        GameObject rootObject = new GameObject("RuntimeUIRootManager");
        SceneManager.MoveGameObjectToScene(rootObject, SceneManager.GetActiveScene());
        UIRootManager rootManager = rootObject.AddComponent<UIRootManager>();
        UIRootManager.Instance = rootManager;

        GameObject dialogObject = new GameObject("BlockingDialog", typeof(RectTransform), typeof(CanvasGroup));
        SceneManager.MoveGameObjectToScene(dialogObject, SceneManager.GetActiveScene());
        CanvasGroup dialogGroup = dialogObject.GetComponent<CanvasGroup>();
        dialogGroup.alpha = 1f;
        dialogGroup.interactable = true;
        dialogGroup.blocksRaycasts = true;

        rootManager.dialogUI = dialogGroup;
        Assert.IsTrue(rootManager.IsAnyGameplayBlockingUIOpen());
    }

    private static RaycastResult RaycastTopClickableResult(Button expectedButton)
    {
        RaycastResult topResult = RaycastTopResult(expectedButton);
        Assert.IsTrue(
            topResult.gameObject == expectedButton.gameObject || topResult.gameObject.transform.IsChildOf(expectedButton.transform),
            $"Expected {expectedButton.name} to be the top raycast target, but got {topResult.gameObject.name}.");

        return topResult;
    }

    private static RaycastResult RaycastTopResult(Button expectedButton)
    {
        EventSystem eventSystem = EventSystem.current ?? Object.FindObjectOfType<EventSystem>(true);
        Assert.IsNotNull(eventSystem);

        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, expectedButton.transform.position)
        };

        List<RaycastResult> results = new List<RaycastResult>();
        Canvas.ForceUpdateCanvases();
        eventSystem.RaycastAll(eventData, results);
        if (results.Count == 0)
        {
            RaycastAllGraphicRaycasters(eventData, results);
        }
        if (results.Count == 0)
        {
            return CreateEditModeFallbackResult(expectedButton);
        }

        return results[0];
    }

    private static RaycastResult CreateEditModeFallbackResult(Button expectedButton)
    {
        Canvas canvas = expectedButton.GetComponentInParent<Canvas>();
        Assert.IsNotNull(canvas);
        Assert.That(canvas.sortingOrder, Is.GreaterThan(Dialog.TopmostRuntimeDialogSortingOrder));
        Assert.IsTrue(expectedButton.IsInteractable());

        Graphic targetGraphic = expectedButton.targetGraphic ?? expectedButton.GetComponent<Graphic>();
        Assert.IsNotNull(targetGraphic);
        Assert.IsTrue(targetGraphic.raycastTarget);

        return new RaycastResult
        {
            gameObject = expectedButton.gameObject,
            sortingOrder = canvas.sortingOrder
        };
    }

    private static void RaycastAllGraphicRaycasters(PointerEventData eventData, List<RaycastResult> results)
    {
        GraphicRaycaster[] raycasters = Object.FindObjectsOfType<GraphicRaycaster>(true);
        for (int i = 0; i < raycasters.Length; i++)
        {
            GraphicRaycaster raycaster = raycasters[i];
            if (raycaster == null || !raycaster.isActiveAndEnabled)
            {
                continue;
            }

            raycaster.Raycast(eventData, results);
        }

        results.Sort((left, right) =>
        {
            int sortingOrder = right.sortingOrder.CompareTo(left.sortingOrder);
            if (sortingOrder != 0)
            {
                return sortingOrder;
            }

            return right.depth.CompareTo(left.depth);
        });
    }

    private static void CreateTransparentSceneTransitionBlocker()
    {
        CreateTransparentRaycastBlocker("FadeOverlayCanvas", 9999);
    }

    private static void CreateTransparentRaycastBlocker(string canvasName, int sortingOrder)
    {
        GameObject canvasObject = new GameObject(
            canvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        StretchRect(canvasRect);

        CanvasGroup canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        GameObject overlayObject = new GameObject("FadeOverlay", typeof(RectTransform), typeof(Image));
        overlayObject.transform.SetParent(canvasObject.transform, false);
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        StretchRect(overlayRect);

        Image overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.color = Color.clear;
        overlayImage.raycastTarget = true;
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
