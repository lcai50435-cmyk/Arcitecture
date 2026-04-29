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
        GameObject canvasObject = new GameObject(
            "FadeOverlayCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 9999;

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
