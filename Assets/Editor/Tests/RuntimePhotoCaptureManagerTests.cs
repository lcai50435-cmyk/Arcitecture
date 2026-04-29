using System.Reflection;
using NUnit.Framework;
using UnityEngine.SceneManagement;

public sealed class RuntimePhotoCaptureManagerTests
{
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
}
