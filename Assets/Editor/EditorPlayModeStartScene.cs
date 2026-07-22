using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class EditorPlayModeStartScene
{
    public const string MainScenePath = "Assets/Scenes/MainScene.unity";

    [System.Obsolete("Use MainScenePath instead.")]
    public const string BaseScenePath = MainScenePath;

    static EditorPlayModeStartScene()
    {
        EnsureMainStartScene();
    }

    [System.Obsolete("Use EnsureMainStartScene instead.")]
    public static void EnsureBaseStartScene()
    {
        EnsureMainStartScene();
    }

    public static void EnsureMainStartScene()
    {
        SceneAsset mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
        if (mainScene == null)
        {
            Debug.LogWarning($"Editor Play Mode start scene setup failed: missing {MainScenePath}");
            return;
        }

        if (EditorSceneManager.playModeStartScene == mainScene)
        {
            return;
        }

        EditorSceneManager.playModeStartScene = mainScene;
    }
}
