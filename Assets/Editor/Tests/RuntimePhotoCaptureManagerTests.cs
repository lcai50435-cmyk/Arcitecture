using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RuntimePhotoCaptureManagerTests
{
    private Scene originalActiveScene;
    private Scene createdScene;

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

        if (createdScene.IsValid() && createdScene.isLoaded)
        {
            EditorSceneManager.CloseScene(createdScene, true);
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
    public void EnsureInstanceCreatesEventSystemForPhotoConfirmationButtons()
    {
        createdScene = SceneManager.CreateScene("FirstPass_1");
        Assert.IsTrue(SceneManager.SetActiveScene(createdScene));
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
        createdScene = SceneManager.CreateScene("FirstPass_1");
        Assert.IsTrue(SceneManager.SetActiveScene(createdScene));

        RuntimePhotoCaptureManager manager = RuntimePhotoCaptureManager.EnsureInstance();
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
        RaycastResult result = RaycastTopClickableResult(button);
        Assert.IsNotNull(result.gameObject);
        Assert.IsTrue(result.gameObject == button.gameObject || result.gameObject.transform.IsChildOf(button.transform));

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position)
        };
        ExecuteEvents.ExecuteHierarchy(result.gameObject, eventData, ExecuteEvents.pointerClickHandler);

        Assert.AreEqual(expectedDecision, GetPendingDecision(manager));
    }

    private static RaycastResult RaycastTopClickableResult(Button expectedButton)
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

        for (int i = 0; i < results.Count; i++)
        {
            GameObject clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(results[i].gameObject);
            if (clickHandler != null)
            {
                return results[i];
            }
        }

        Assert.Fail("No clickable raycast result was found.");
        return default;
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
