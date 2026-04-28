using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
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
    public void HudPlacesAvatarAtBottomRightWithSafePadding()
    {
        Scene gameplayScene = GetOrCreateScene("GameScene");
        Assert.IsTrue(SceneManager.SetActiveScene(gameplayScene));

        BeaverAssistantHud hud = BeaverAssistantHud.EnsureInstance();
        InvokeSceneChanged(hud, "GameScene");

        RectTransform avatarRect = hud.transform.Find("BeaverAssistantCanvas/BeaverAvatarButton") as RectTransform;
        Assert.IsNotNull(avatarRect);
        Assert.That(avatarRect.anchorMin, Is.EqualTo(new Vector2(1f, 0f)));
        Assert.That(avatarRect.anchorMax, Is.EqualTo(new Vector2(1f, 0f)));
        Assert.That(avatarRect.pivot, Is.EqualTo(new Vector2(1f, 0f)));
        Assert.That(avatarRect.anchoredPosition.x, Is.EqualTo(-48f).Within(0.01f));
        Assert.That(avatarRect.anchoredPosition.y, Is.EqualTo(64f).Within(0.01f));
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

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);

        field.SetValue(target, value);
    }
}
