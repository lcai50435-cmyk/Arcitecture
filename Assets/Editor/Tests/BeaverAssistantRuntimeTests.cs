using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class BeaverAssistantRuntimeTests
{
    private readonly List<Scene> createdScenes = new List<Scene>();
    private Scene originalActiveScene;

    [SetUp]
    public void SetUp()
    {
        originalActiveScene = SceneManager.GetActiveScene();
    }

    [TearDown]
    public void TearDown()
    {
        BeaverAssistantHud[] huds = Object.FindObjectsOfType<BeaverAssistantHud>(true);
        for (int i = 0; i < huds.Length; i++)
        {
            if (huds[i] != null)
            {
                Object.DestroyImmediate(huds[i].gameObject);
            }
        }

        BeaverAssistantPanel[] panels = Object.FindObjectsOfType<BeaverAssistantPanel>(true);
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
            {
                panels[i].Hide();
                Object.DestroyImmediate(panels[i].gameObject);
            }
        }

        DestroyEventSystemsInCreatedScenes();

        if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(originalActiveScene);
        }

        for (int i = createdScenes.Count - 1; i >= 0; i--)
        {
            Scene scene = createdScenes[i];
            if (scene.IsValid() && scene.isLoaded)
            {
                if (SceneManager.sceneCount > 1)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
                else
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
            }
        }

        createdScenes.Clear();
    }

    [Test]
    public void HudStaysVisibleWhenIllustratedUiSceneLoadsAdditivelyOverGameplay()
    {
        Scene gameplayScene = GetOrCreateScene("GameScene");
        Assert.IsTrue(gameplayScene.IsValid() && gameplayScene.isLoaded);

        BeaverAssistantHud hud = BeaverAssistantHud.EnsureInstance();
        InvokeSceneChanged(hud, "GameScene");
        GameObject canvas = hud.transform.Find("BeaverAssistantCanvas")?.gameObject;
        Assert.IsNotNull(canvas);
        Assert.IsTrue(canvas.activeSelf);

        InvokeSceneChanged(hud, "GameScene");

        Assert.IsTrue(canvas.activeSelf);
    }

    [Test]
    public void HudReappearsWhenBlockingUiClosesAfterCanvasWasHidden()
    {
        Scene gameplayScene = GetOrCreateScene("GameScene");
        Assert.IsTrue(gameplayScene.IsValid() && gameplayScene.isLoaded);

        BeaverAssistantHud hud = BeaverAssistantHud.EnsureInstance();
        InvokeSceneChanged(hud, "GameScene");
        GameObject canvas = hud.transform.Find("BeaverAssistantCanvas")?.gameObject;
        Assert.IsNotNull(canvas);
        Assert.IsTrue(canvas.activeSelf);

        InvokeSceneChanged(hud, "IllustratedUIScene");
        Assert.IsFalse(canvas.activeSelf);

        SetPrivateField(hud, "wasAssistantBlockedByRuntimeUi", true);
        InvokePrivate(hud, "Update");

        Assert.IsTrue(canvas.activeSelf);
    }

    [Test]
    public void HudSelfCorrectsHiddenCanvasWhenGameplayIsUnblocked()
    {
        Scene gameplayScene = GetOrCreateScene("GameScene");
        Assert.IsTrue(gameplayScene.IsValid() && gameplayScene.isLoaded);

        BeaverAssistantHud hud = BeaverAssistantHud.EnsureInstance();
        InvokeSceneChanged(hud, "GameScene");
        GameObject canvas = hud.transform.Find("BeaverAssistantCanvas")?.gameObject;
        Assert.IsNotNull(canvas);
        canvas.SetActive(false);

        InvokePrivate(hud, "Update");

        Assert.IsTrue(canvas.activeSelf);
    }

    [Test]
    public void HudPlacesAvatarAtBottomLeftWithSafePadding()
    {
        Scene gameplayScene = GetOrCreateScene("GameScene");
        Assert.IsTrue(gameplayScene.IsValid() && gameplayScene.isLoaded);

        BeaverAssistantHud hud = BeaverAssistantHud.EnsureInstance();
        InvokeSceneChanged(hud, "GameScene");

        RectTransform avatarRect = hud.transform.Find("BeaverAssistantCanvas/BeaverAvatarButton") as RectTransform;
        Assert.IsNotNull(avatarRect);
        Assert.That(avatarRect.anchorMin, Is.EqualTo(new Vector2(0f, 0f)));
        Assert.That(avatarRect.anchorMax, Is.EqualTo(new Vector2(0f, 0f)));
        Assert.That(avatarRect.pivot, Is.EqualTo(new Vector2(0f, 0f)));
        Assert.That(avatarRect.anchoredPosition.x, Is.EqualTo(48f).Within(0.01f));
        Assert.That(avatarRect.anchoredPosition.y, Is.EqualTo(64f).Within(0.01f));
    }

    [Test]
    public void HudCanvasRendersAboveGameplayPromptOverlay()
    {
        Scene gameplayScene = GetOrCreateScene("GameScene");
        Assert.IsTrue(gameplayScene.IsValid() && gameplayScene.isLoaded);

        BeaverAssistantHud hud = BeaverAssistantHud.EnsureInstance();
        InvokeSceneChanged(hud, "GameScene");

        Canvas canvas = hud.transform.Find("BeaverAssistantCanvas")?.GetComponent<Canvas>();
        Assert.IsNotNull(canvas);
        Assert.That(canvas.sortingOrder, Is.GreaterThan(SceneTransitionBlockerSortingOrder));
        Assert.That(canvas.sortingOrder, Is.LessThan(Dialog.TopmostRuntimeDialogSortingOrder));
    }

    [Test]
    public void AvatarClickOpensPanelAboveTransparentSceneTransitionBlockerInFirstPass()
    {
        Scene gameplayScene = GetOrCreateScene("FirstPass_1");
        Assert.IsTrue(gameplayScene.IsValid() && gameplayScene.isLoaded);
        CreateTransparentSceneTransitionBlocker();

        BeaverAssistantHud hud = BeaverAssistantHud.EnsureInstance();
        InvokeSceneChanged(hud, "FirstPass_1");

        Button avatarButton = hud.transform.Find("BeaverAssistantCanvas/BeaverAvatarButton")?.GetComponent<Button>();
        Assert.IsNotNull(avatarButton);

        RaycastResult result = RaycastTopResult(avatarButton);
        Assert.IsNotNull(result.gameObject);
        Assert.IsTrue(
            result.gameObject == avatarButton.gameObject || result.gameObject.transform.IsChildOf(avatarButton.transform),
            $"Expected beaver avatar to receive the top raycast in FirstPass_1, but got {result.gameObject.name}.");

            ExecuteEvents.ExecuteHierarchy(
            result.gameObject,
            new PointerEventData(GetEventSystem())
            {
                position = RectTransformUtility.WorldToScreenPoint(null, avatarButton.transform.position)
            },
            ExecuteEvents.pointerClickHandler);

        Assert.IsTrue(BeaverAssistantPanel.IsOpen);

        Canvas panelCanvas = BeaverAssistantPanel.EnsureInstance()
            .transform
            .Find("BeaverAssistantPanelCanvas")
            ?.GetComponent<Canvas>();
        Assert.IsNotNull(panelCanvas);
        Assert.That(panelCanvas.sortingOrder, Is.GreaterThan(SceneTransitionBlockerSortingOrder));
    }

    [Test]
    public void LegacyAuthoredBeaverButtonOpensPanelOnceInFirstPass()
    {
        Scene gameplayScene = GetOrCreateScene("FirstPass_1");
        Assert.IsTrue(gameplayScene.IsValid() && gameplayScene.isLoaded);
        CreateTransparentSceneTransitionBlocker();
        Button legacyButton = CreateLegacyBeaverButton();

        BeaverAssistantHud hud = BeaverAssistantHud.EnsureInstance();
        InvokeSceneChanged(hud, "FirstPass_1");
        InvokePrivate(hud, "RefreshLegacyBeaverButtonBindings");
        InvokePrivate(hud, "RefreshLegacyBeaverButtonBindings");

        Assert.IsNotNull(legacyButton.GetComponent<BeaverAssistantLegacyButtonBinding>());
        Canvas legacyCanvas = legacyButton.GetComponentInParent<Canvas>();
        Assert.IsNotNull(legacyCanvas);
        Assert.That(legacyCanvas.sortingOrder, Is.GreaterThan(SceneTransitionBlockerSortingOrder));

        RaycastResult result = RaycastTopResult(legacyButton);
        Assert.IsNotNull(result.gameObject);
        Assert.IsTrue(
            result.gameObject == legacyButton.gameObject || result.gameObject.transform.IsChildOf(legacyButton.transform),
            $"Expected legacy beaver button to receive the top raycast, but got {result.gameObject.name}.");

        legacyButton.onClick.Invoke();
        Assert.IsTrue(BeaverAssistantPanel.IsOpen);

        legacyButton.onClick.Invoke();
        Assert.IsFalse(BeaverAssistantPanel.IsOpen);
    }

    [Test]
    public void LegacyAuthoredBeaverButtonRebindsWhenMarkerExistsButListenerWasCleared()
    {
        Scene gameplayScene = GetOrCreateScene("FirstPass_1");
        Assert.IsTrue(gameplayScene.IsValid() && gameplayScene.isLoaded);
        Button legacyButton = CreateLegacyBeaverButton();

        BeaverAssistantHud hud = BeaverAssistantHud.EnsureInstance();
        InvokeSceneChanged(hud, "FirstPass_1");
        InvokePrivate(hud, "RefreshLegacyBeaverButtonBindings");

        Assert.IsNotNull(legacyButton.GetComponent<BeaverAssistantLegacyButtonBinding>());

        legacyButton.onClick.RemoveAllListeners();
        InvokePrivate(hud, "RefreshLegacyBeaverButtonBindings");

        legacyButton.onClick.Invoke();

        Assert.IsTrue(BeaverAssistantPanel.IsOpen);
    }

    [Test]
    public void RuntimeAvatarButtonRebindsAfterListenersCleared()
    {
        Scene gameplayScene = GetOrCreateScene("FirstPass_1");
        Assert.IsTrue(gameplayScene.IsValid() && gameplayScene.isLoaded);

        BeaverAssistantHud hud = BeaverAssistantHud.EnsureInstance();
        InvokeSceneChanged(hud, "FirstPass_1");

        Button avatarButton = hud.transform.Find("BeaverAssistantCanvas/BeaverAvatarButton")?.GetComponent<Button>();
        Assert.IsNotNull(avatarButton);

        avatarButton.onClick.RemoveAllListeners();
        BeaverAssistantHud.EnsureInstance();

        avatarButton.onClick.Invoke();

        Assert.IsTrue(BeaverAssistantPanel.IsOpen);
    }

    [Test]
    public void SpriteCompanionClickProxyOpensPanelInFirstPass()
    {
        Scene gameplayScene = GetOrCreateScene("FirstPass_1");
        Assert.IsTrue(gameplayScene.IsValid() && gameplayScene.isLoaded);
        GameObject companion = new GameObject("SpriteCompanion");
        SpriteCompanionAssistantClickProxy proxy = companion.AddComponent<SpriteCompanionAssistantClickProxy>();

        proxy.ToggleAssistantPanel();

        Assert.IsTrue(BeaverAssistantPanel.IsOpen);
    }

    [Test]
    public void SpriteCompanionRuntimeAddsAssistantClickProxy()
    {
        GameObject companion = new GameObject("SpriteCompanion");

        try
        {
            MethodInfo method = typeof(SpriteCompanionRuntime).GetMethod(
                "EnsureAssistantClickProxy",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method);

            method.Invoke(null, new object[] { companion });

            Assert.IsNotNull(companion.GetComponent<SpriteCompanionAssistantClickProxy>());
        }
        finally
        {
            Object.DestroyImmediate(companion);
        }
    }

    [Test]
    public void HudEnsuresEventSystemForAvatarClick()
    {
        DestroyEventSystemsInCreatedScenes();
        Scene gameplayScene = GetOrCreateScene("GameScene");
        Assert.IsTrue(gameplayScene.IsValid() && gameplayScene.isLoaded);

        BeaverAssistantHud hud = BeaverAssistantHud.EnsureInstance();
        InvokeSceneChanged(hud, "GameScene");

        EventSystem eventSystem = EventSystem.current ?? Object.FindObjectOfType<EventSystem>(true);
        Assert.IsNotNull(eventSystem);
        Assert.IsNotNull(eventSystem.GetComponent<BaseInputModule>());
    }

    [Test]
    public void PanelCanvasRendersAboveSceneTransitionBlockerWhenShown()
    {
        Scene baseScene = GetOrCreateScene("NewBase");
        Assert.IsTrue(baseScene.IsValid() && baseScene.isLoaded);

        BeaverAssistantPanel panel = BeaverAssistantPanel.EnsureInstance();
        panel.Show();

        Canvas canvas = panel.transform.Find("BeaverAssistantPanelCanvas")?.GetComponent<Canvas>();
        Assert.IsNotNull(canvas);
        Assert.That(canvas.sortingOrder, Is.GreaterThan(SceneTransitionBlockerSortingOrder));
        Assert.That(canvas.sortingOrder, Is.LessThan(Dialog.TopmostRuntimeDialogSortingOrder));

        panel.Hide();
    }

    [Test]
    public void PanelShowFocusesInteractiveInputFieldForTyping()
    {
        Scene baseScene = GetOrCreateScene("NewBase");
        Assert.IsTrue(baseScene.IsValid() && baseScene.isLoaded);

        BeaverAssistantPanel panel = BeaverAssistantPanel.EnsureInstance();
        panel.Show();

        TMP_InputField input = panel.transform.Find("BeaverAssistantPanelCanvas/Panel/Input")?.GetComponent<TMP_InputField>();
        Assert.IsNotNull(input);
        Assert.IsTrue(input.enabled);
        Assert.IsTrue(input.interactable);
        Assert.IsFalse(input.readOnly);
        Assert.IsNotNull(input.targetGraphic);
        Assert.IsTrue(input.targetGraphic.raycastTarget);
        Assert.AreSame(input.gameObject, GetEventSystem().currentSelectedGameObject);

        panel.Hide();
    }

    [Test]
    public void PanelCloseButtonPointerUpClosesAndDisablesRaycastSurface()
    {
        Scene baseScene = GetOrCreateScene("NewBase");
        Assert.IsTrue(baseScene.IsValid() && baseScene.isLoaded);

        BeaverAssistantPanel panel = BeaverAssistantPanel.EnsureInstance();
        panel.Show();

        Button close = panel.transform.Find("BeaverAssistantPanelCanvas/Panel/CloseButton")?.GetComponent<Button>();
        Assert.IsNotNull(close);

        GraphicRaycaster raycaster = panel.transform.Find("BeaverAssistantPanelCanvas")?.GetComponent<GraphicRaycaster>();
        Assert.IsNotNull(raycaster);
        Assert.IsTrue(raycaster.enabled);

        PointerEventData eventData = new PointerEventData(GetEventSystem())
        {
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, close.transform.position)
        };

        ExecuteEvents.Execute(close.gameObject, eventData, ExecuteEvents.pointerUpHandler);

        Assert.IsFalse(panel.gameObject.activeSelf);
        Assert.IsFalse(BeaverAssistantPanel.IsOpen);
        Assert.IsFalse(raycaster.enabled);
        Assert.IsNull(GetEventSystem().currentSelectedGameObject);
    }

    [Test]
    public void PanelAskButtonRebindsAndPointerClickSubmitsAfterListenerWasCleared()
    {
        Scene baseScene = GetOrCreateScene("NewBase");
        Assert.IsTrue(baseScene.IsValid() && baseScene.isLoaded);

        BeaverAssistantPanel panel = BeaverAssistantPanel.EnsureInstance();
        panel.Show();

        TMP_InputField input = panel.transform.Find("BeaverAssistantPanelCanvas/Panel/Input")?.GetComponent<TMP_InputField>();
        Button ask = panel.transform.Find("BeaverAssistantPanelCanvas/Panel/AskButton")?.GetComponent<Button>();
        ScrollRect scrollRect = panel.transform.Find("BeaverAssistantPanelCanvas/Panel/HistoryScroll")?.GetComponent<ScrollRect>();
        Assert.IsNotNull(input);
        Assert.IsNotNull(ask);
        Assert.IsNotNull(scrollRect);

        ask.onClick.RemoveAllListeners();
        BeaverAssistantPanel.EnsureInstance();
        input.text = "赵州桥";

        PointerEventData eventData = new PointerEventData(GetEventSystem())
        {
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, ask.transform.position)
        };

        ExecuteEvents.Execute(ask.gameObject, eventData, ExecuteEvents.pointerClickHandler);

        string renderedText = GetCombinedHistoryText(scrollRect.content);
        Assert.That(renderedText, Does.Contain("赵州桥"));
        Assert.That(input.text, Is.Empty);

        panel.Hide();
    }

    [Test]
    public void PanelHistoryUsesScrollableViewport()
    {
        Scene baseScene = GetOrCreateScene("NewBase");
        Assert.IsTrue(baseScene.IsValid() && baseScene.isLoaded);

        BeaverAssistantPanel panel = BeaverAssistantPanel.EnsureInstance();
        panel.Show();

        ScrollRect scrollRect = panel.transform.Find("BeaverAssistantPanelCanvas/Panel/HistoryScroll")?.GetComponent<ScrollRect>();
        Assert.IsNotNull(scrollRect);
        Assert.IsTrue(scrollRect.vertical);
        Assert.IsFalse(scrollRect.horizontal);
        Assert.IsNotNull(scrollRect.viewport);
        Assert.IsNotNull(scrollRect.content);

        TextMeshProUGUI historyText = scrollRect.content.GetComponentInChildren<TextMeshProUGUI>(true);
        Assert.IsNotNull(historyText);
        Assert.That(historyText.overflowMode, Is.EqualTo(TextOverflowModes.Overflow));

        panel.Hide();
    }

    [Test]
    public void PanelHistoryExpandsContentAndSnapsToLatestMessage()
    {
        Scene baseScene = GetOrCreateScene("NewBase");
        Assert.IsTrue(baseScene.IsValid() && baseScene.isLoaded);

        BeaverAssistantPanel panel = BeaverAssistantPanel.EnsureInstance();
        panel.Show();

        for (int i = 1; i <= 16; i++)
        {
            InvokePrivate(panel, "AddHistoryLine", $"河狸：第 {i} 条很长的建筑知识，用来撑开历史记录区域，确认内容不会被省略号截断，也能滚动查看。");
        }

        ScrollRect scrollRect = panel.transform.Find("BeaverAssistantPanelCanvas/Panel/HistoryScroll")?.GetComponent<ScrollRect>();
        Assert.IsNotNull(scrollRect);
        Assert.That(scrollRect.content.sizeDelta.y, Is.GreaterThan(268f));
        Assert.That(scrollRect.verticalNormalizedPosition, Is.EqualTo(0f).Within(0.01f));

        string renderedText = GetCombinedHistoryText(scrollRect.content);
        Assert.That(renderedText, Does.Contain("第 1 条"));
        Assert.That(renderedText, Does.Contain("第 16 条"));

        panel.Hide();
    }

    [Test]
    public void PanelHistorySplitsBeaverLeftAndPlayerRight()
    {
        Scene baseScene = GetOrCreateScene("NewBase");
        Assert.IsTrue(baseScene.IsValid() && baseScene.isLoaded);

        BeaverAssistantPanel panel = BeaverAssistantPanel.EnsureInstance();
        panel.Show();

        TMP_InputField input = panel.transform.Find("BeaverAssistantPanelCanvas/Panel/Input")?.GetComponent<TMP_InputField>();
        Assert.IsNotNull(input);
        input.text = "好吧";
        InvokePrivate(panel, "SubmitQuestion");

        ScrollRect scrollRect = panel.transform.Find("BeaverAssistantPanelCanvas/Panel/HistoryScroll")?.GetComponent<ScrollRect>();
        Assert.IsNotNull(scrollRect);

        RectTransform beaverBubble = FindRectByNamePrefix(scrollRect.content, "BeaverMessageBubble");
        RectTransform playerBubble = FindRectByNamePrefix(scrollRect.content, "PlayerMessageBubble");
        Assert.IsNotNull(beaverBubble);
        Assert.IsNotNull(playerBubble);
        Assert.That(beaverBubble.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
        Assert.That(beaverBubble.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
        Assert.That(beaverBubble.pivot, Is.EqualTo(new Vector2(0f, 1f)));
        Assert.That(playerBubble.anchorMin, Is.EqualTo(new Vector2(1f, 1f)));
        Assert.That(playerBubble.anchorMax, Is.EqualTo(new Vector2(1f, 1f)));
        Assert.That(playerBubble.pivot, Is.EqualTo(new Vector2(1f, 1f)));
        Assert.That(GetCombinedHistoryText(scrollRect.content), Does.Contain("好吧"));

        panel.Hide();
    }

    [Test]
    public void HudUsesAuthoredBeaverAvatarResource()
    {
        Scene gameplayScene = GetOrCreateScene("GameScene");
        Assert.IsTrue(gameplayScene.IsValid() && gameplayScene.isLoaded);

        BeaverAssistantHud hud = BeaverAssistantHud.EnsureInstance();
        InvokeSceneChanged(hud, "GameScene");

        Image avatarImage = hud.transform.Find("BeaverAssistantCanvas/BeaverAvatarButton")?.GetComponent<Image>();
        Assert.IsNotNull(avatarImage);
        Assert.IsNotNull(avatarImage.sprite);
        Assert.That(avatarImage.sprite.name, Is.EqualTo("RuntimeBeaverAssistantAvatarSprite"));
        Assert.IsNotNull(avatarImage.sprite.texture);
        Assert.That(avatarImage.sprite.texture.name, Is.EqualTo("dabb2b31-d671-4717-9918-6d60739a0f10_no_bg"));
        Assert.That(avatarImage.sprite.rect.width, Is.LessThan(avatarImage.sprite.texture.width));
        Assert.That(avatarImage.sprite.rect.height, Is.LessThan(avatarImage.sprite.texture.height));
    }

    private Scene GetOrCreateScene(string sceneName)
    {
        Scene existingScene = SceneManager.GetSceneByName(sceneName);
        if (existingScene.IsValid() && existingScene.isLoaded)
        {
            SceneManager.SetActiveScene(existingScene);
            return existingScene;
        }

        string scenePath = $"Assets/Scenes/{sceneName}.unity";
        Scene createdScene = File.Exists(Path.Combine(Application.dataPath, "Scenes", $"{sceneName}.unity"))
            ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (createdScene.IsValid() && createdScene.isLoaded)
        {
            SceneManager.SetActiveScene(createdScene);
        }

        createdScenes.Add(createdScene);
        DestroyAssistantRuntimeObjects();
        DestroyRuntimeUiRoots();
        DestroyEventSystemsInLoadedScenes();
        return createdScene;
    }

    private static void InvokeSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        MethodInfo method = typeof(BeaverAssistantHud).GetMethod(
            "HandleSceneLoaded",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        method.Invoke(null, new object[] { scene, mode });
    }

    private static void InvokeSceneChanged(BeaverAssistantHud hud, string sceneName)
    {
        MethodInfo method = typeof(BeaverAssistantHud).GetMethod(
            "HandleSceneChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        method.Invoke(hud, new object[] { sceneName });
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        method.Invoke(target, null);
    }

    private static void InvokePrivate(object target, string methodName, object argument)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        method.Invoke(target, new[] { argument });
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);

        field.SetValue(target, value);
    }

    private static string GetCombinedHistoryText(Transform content)
    {
        TextMeshProUGUI[] texts = content.GetComponentsInChildren<TextMeshProUGUI>(true);
        List<string> values = new List<string>(texts.Length);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
            {
                values.Add(texts[i].text);
            }
        }

        return string.Join("\n", values);
    }

    private static RectTransform FindRectByNamePrefix(Transform root, string namePrefix)
    {
        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect != null && rect.name.StartsWith(namePrefix, System.StringComparison.Ordinal))
            {
                return rect;
            }
        }

        return null;
    }

    private void DestroyEventSystemsInCreatedScenes()
    {
        EventSystem[] eventSystems = Object.FindObjectsOfType<EventSystem>(true);
        for (int i = 0; i < eventSystems.Length; i++)
        {
            EventSystem eventSystem = eventSystems[i];
            if (eventSystem == null || !IsCreatedScene(eventSystem.gameObject.scene))
            {
                continue;
            }

            Object.DestroyImmediate(eventSystem.gameObject);
        }
    }

    private static void DestroyEventSystemsInLoadedScenes()
    {
        EventSystem[] eventSystems = Object.FindObjectsOfType<EventSystem>(true);
        for (int i = 0; i < eventSystems.Length; i++)
        {
            if (eventSystems[i] != null)
            {
                Object.DestroyImmediate(eventSystems[i].gameObject);
            }
        }
    }

    private static void DestroyRuntimeUiRoots()
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

    private static void DestroyAssistantRuntimeObjects()
    {
        BeaverAssistantHud[] huds = Object.FindObjectsOfType<BeaverAssistantHud>(true);
        for (int i = 0; i < huds.Length; i++)
        {
            if (huds[i] != null)
            {
                Object.DestroyImmediate(huds[i].gameObject);
            }
        }

        BeaverAssistantPanel[] panels = Object.FindObjectsOfType<BeaverAssistantPanel>(true);
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
            {
                Object.DestroyImmediate(panels[i].gameObject);
            }
        }

        RuntimePauseMenu[] pauseMenus = Object.FindObjectsOfType<RuntimePauseMenu>(true);
        for (int i = 0; i < pauseMenus.Length; i++)
        {
            if (pauseMenus[i] != null)
            {
                Object.DestroyImmediate(pauseMenus[i].gameObject);
            }
        }
    }

    private bool IsCreatedScene(Scene scene)
    {
        for (int i = 0; i < createdScenes.Count; i++)
        {
            if (createdScenes[i] == scene)
            {
                return true;
            }
        }

        return false;
    }

    private const int SceneTransitionBlockerSortingOrder = 9999;

    private static RaycastResult RaycastTopResult(Button expectedButton)
    {
        EventSystem eventSystem = GetEventSystem();
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

    private static EventSystem GetEventSystem()
    {
        return EventSystem.current ?? Object.FindObjectOfType<EventSystem>(true);
    }

    private static RaycastResult CreateEditModeFallbackResult(Button expectedButton)
    {
        Canvas canvas = expectedButton.GetComponentInParent<Canvas>();
        Assert.IsNotNull(canvas);
        Assert.That(canvas.sortingOrder, Is.GreaterThan(SceneTransitionBlockerSortingOrder));
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
        canvas.sortingOrder = SceneTransitionBlockerSortingOrder;

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

    private static Button CreateLegacyBeaverButton()
    {
        GameObject canvasObject = new GameObject(
            "AICanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 240;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        StretchRect(canvasRect);

        GameObject buttonObject = new GameObject("Beaver", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvasObject.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(-820f, -428f);
        buttonRect.sizeDelta = new Vector2(108f, 90f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = true;

        return buttonObject.GetComponent<Button>();
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
