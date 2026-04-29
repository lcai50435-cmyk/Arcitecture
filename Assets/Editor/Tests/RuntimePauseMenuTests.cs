using System.Collections.Generic;
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
        DestroyEventSystems();
    }

    [TearDown]
    public void TearDown()
    {
        DestroyPauseMenu();
        DestroyEventSystems();

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
    public void EnsureInstanceCreatesEventSystemForPauseMenuButtons()
    {
        createdScene = SceneManager.CreateScene("FirstPass_1");
        Assert.IsTrue(SceneManager.SetActiveScene(createdScene));
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
        createdScene = SceneManager.CreateScene("FirstPass_1");
        Assert.IsTrue(SceneManager.SetActiveScene(createdScene));

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
        createdScene = SceneManager.CreateScene("NewBase");
        Assert.IsTrue(SceneManager.SetActiveScene(createdScene));
        Time.timeScale = 1f;

        Assert.IsTrue(RuntimePauseMenu.TryOpenFromExternal());

        Assert.IsTrue(RuntimePauseMenu.IsPauseOpen);
        Assert.AreEqual(0f, Time.timeScale);
    }

    [Test]
    public void TryOpenFromPauseKeyBypassesExistingGameplayBlockingUi()
    {
        createdScene = SceneManager.CreateScene("FirstPass_1");
        Assert.IsTrue(SceneManager.SetActiveScene(createdScene));
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
        createdScene = SceneManager.CreateScene("FirstPass_1");
        Assert.IsTrue(SceneManager.SetActiveScene(createdScene));
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
        createdScene = SceneManager.CreateScene("FirstPass_1");
        Assert.IsTrue(SceneManager.SetActiveScene(createdScene));
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
        createdScene = SceneManager.CreateScene("FirstPass_1");
        Assert.IsTrue(SceneManager.SetActiveScene(createdScene));
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
        createdScene = SceneManager.CreateScene("FirstPass_1");
        Assert.IsTrue(SceneManager.SetActiveScene(createdScene));
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
    public void PauseMenuButtonBecomesInteractiveBeforeRevealAnimationFullyCompletes()
    {
        createdScene = SceneManager.CreateScene("FirstPass_1");
        Assert.IsTrue(SceneManager.SetActiveScene(createdScene));
        Time.timeScale = 1f;

        Assert.IsTrue(RuntimePauseMenu.TryOpenFromExternal());

        Button resumeButton = FindButtonByLabel("返回游戏");
        Assert.IsNotNull(resumeButton);
        CanvasGroup canvasGroup = resumeButton.GetComponent<CanvasGroup>();
        Assert.IsNotNull(canvasGroup);

        ApplyMenuButtonRevealProgress(resumeButton, 0.5f);

        Assert.IsTrue(canvasGroup.interactable);
        Assert.IsTrue(canvasGroup.blocksRaycasts);
    }

    private static void DestroyPauseMenu()
    {
        if (RuntimePauseMenu.Instance != null)
        {
            Object.DestroyImmediate(RuntimePauseMenu.Instance.gameObject);
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
        EventSystem eventSystem = EventSystem.current;
        Assert.IsNotNull(eventSystem);

        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, expectedButton.transform.position)
        };

        List<RaycastResult> results = new List<RaycastResult>();
        eventSystem.RaycastAll(eventData, results);
        Assert.IsNotEmpty(results);

        return results[0];
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
