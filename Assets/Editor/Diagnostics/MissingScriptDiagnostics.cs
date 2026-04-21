using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissingScriptDiagnostics
{
    [MenuItem("Tools/Diagnostics/Scan Missing Scripts")]
    public static void ScanMissingScripts()
    {
        List<string> issues = new List<string>();
        ScanLoadedScenes(issues);
        ScanAllPrefabs(issues);

        if (issues.Count == 0)
        {
            Debug.Log("MissingScriptDiagnostics: 未发现缺失脚本挂载。");
            return;
        }

        Debug.LogWarning($"MissingScriptDiagnostics: 发现 {issues.Count} 处缺失脚本挂载。\n" + string.Join("\n", issues));
    }

    private static void ScanLoadedScenes(List<string> issues)
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                continue;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                ScanHierarchy(roots[i], $"Scene:{scene.path}", issues);
            }
        }
    }

    private static void ScanAllPrefabs(List<string> issues)
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabRoot == null)
            {
                continue;
            }

            ScanHierarchy(prefabRoot, $"Prefab:{prefabPath}", issues);
        }
    }

    private static void ScanHierarchy(GameObject gameObject, string owner, List<string> issues)
    {
        if (gameObject == null)
        {
            return;
        }

        int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
        if (missingCount > 0)
        {
            issues.Add($"{owner} -> {GetHierarchyPath(gameObject.transform)} (missing={missingCount})");
        }

        Transform transform = gameObject.transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            ScanHierarchy(transform.GetChild(i).gameObject, owner, issues);
        }
    }

    private static string GetHierarchyPath(Transform current)
    {
        if (current == null)
        {
            return "<null>";
        }

        string path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
}
