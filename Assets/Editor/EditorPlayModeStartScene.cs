using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class EditorPlayModeStartScene
{
    public const string BaseScenePath = "Assets/Scenes/NewBase.unity";

    static EditorPlayModeStartScene()
    {
        EnsureBaseStartScene();
    }

    public static void EnsureBaseStartScene()
    {
        SceneAsset baseScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BaseScenePath);
        if (baseScene == null)
        {
            Debug.LogWarning($"编辑器 Play Mode 启动场景配置失败：未找到 {BaseScenePath}");
            return;
        }

        if (EditorSceneManager.playModeStartScene == baseScene)
        {
            return;
        }

        EditorSceneManager.playModeStartScene = baseScene;
    }
}
