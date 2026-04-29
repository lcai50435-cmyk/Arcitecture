using System.Collections.Generic;
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
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        createdScenes.Clear();
    }

    [Test]
    public void HudStaysVisibleWhenIllustratedUiSceneLoadsAdditivelyOverGameplay()
    {
        Scene gameplayScene = GetOrCreateScene("GameScene");
        Scene illustratedScene = GetOrCreateScene("IllustratedUIScene");
        Assert.IsTrue(SceneManager.SetActiveScene(gameplayScene));

        BeaverAssistantHud hud = BeaverAssistantHud.EnsureInstance();
        InvokeSceneLoaded(gameplayScene, LoadSceneMode.Single);
        GameObject canvas = hud.transform.Find("BeaverAssistantCanvas")?.gameObject;
        Assert.IsNotNull(canvas);
        Assert.IsTrue(canvas.activeSelf);

        InvokeSceneLoaded(illustratedScene, LoadSceneMode.Additive);

        Assert.IsTrue(canvas.activeSelf);
    }

    [Test]
    public void HudReappearsWhenBlockingUiClosesAfterCanvasWasHidden()
    {
        Scene gameplayScene = GetOrCreateScene("GameScene");
        Assert.IsTrue(SceneManager.SetActiveScene(gameplayScene));

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
        Assert.IsTrue(SceneManager.SetActiveScene(gameplayScene));

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
        Assert.IsTrue(SceneManager.SetActiveScene(gameplayScene));

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
        Assert.IsTrue(SceneManager.SetActiveScene(gameplayScene));

        BeaverAssistantHud hud = BeaverAssistantHud.EnsureInstance();
        InvokeSceneChanged(hud, "GameScene");

        Canvas canvas = hud.transform.Find("BeaverAssistantCanvas")?.GetComponent<Canvas>();
        Assert.IsNotNull(canvas);
        Assert.IsTrue(canvas.overrideSorting);
        Assert.That(canvas.sortingOrder, Is.GreaterThan(RuntimeModalStyle.BackdropSortingOrder));
        Assert.That(canvas.sortingOrder, Is.LessThan(RuntimeModalStyle.ModalSortingOrder));
    }

    [Test]
    public void HudEnsuresEventSystemForAvatarClick()
    {
        DestroyEventSystemsInCreatedScenes();
        Scene gameplayScene = GetOrCreateScene("GameScene");
        Assert.IsTrue(SceneManager.SetActiveScene(gameplayScene));

        BeaverAssistantHud hud = BeaverAssistantHud.EnsureInstance();
        InvokeSceneChanged(hud, "GameScene");

        EventSystem eventSystem = EventSystem.current ?? Object.FindObjectOfType<EventSystem>(true);
        Assert.IsNotNull(eventSystem);
        Assert.IsNotNull(eventSystem.GetComponent<BaseInputModule>());
    }

    [Test]
    public void PanelCanvasUsesModalSortingOrderWhenShown()
    {
        Scene baseScene = GetOrCreateScene("NewBase");
        Assert.IsTrue(SceneManager.SetActiveScene(baseScene));

        BeaverAssistantPanel panel = BeaverAssistantPanel.EnsureInstance();
        panel.Show();

        Canvas canvas = panel.transform.Find("BeaverAssistantPanelCanvas")?.GetComponent<Canvas>();
        Assert.IsNotNull(canvas);
        Assert.IsTrue(canvas.overrideSorting);
        Assert.That(canvas.sortingOrder, Is.EqualTo(RuntimeModalStyle.ModalSortingOrder));

        panel.Hide();
    }

    [Test]
    public void PanelHistoryUsesScrollableViewport()
    {
        Scene baseScene = GetOrCreateScene("NewBase");
        Assert.IsTrue(SceneManager.SetActiveScene(baseScene));

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
        Assert.IsTrue(SceneManager.SetActiveScene(baseScene));

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
        Assert.IsTrue(SceneManager.SetActiveScene(baseScene));

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
        Assert.IsTrue(SceneManager.SetActiveScene(gameplayScene));

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
            return existingScene;
        }

        Scene createdScene = SceneManager.CreateScene(sceneName);
        createdScenes.Add(createdScene);
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
}
