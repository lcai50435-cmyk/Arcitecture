using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissingScriptDiagnostics
{
    [MenuItem("Tools/Diagnostics/Scan Missing Scripts")]
    public static void ScanMissingScripts()
    {
        List<string> issues = CollectMissingScriptIssues(true);

        if (issues.Count == 0)
        {
            Debug.Log("MissingScriptDiagnostics: no missing script references found.");
            return;
        }

        Debug.LogWarning($"MissingScriptDiagnostics: found {issues.Count} missing script references.\n" + string.Join("\n", issues));
    }

    public static List<string> CollectMissingScriptIssues(bool includeLoadedScenes)
    {
        List<string> issues = new List<string>();
        if (includeLoadedScenes)
        {
            ScanLoadedScenes(issues);
        }

        ScanBuildScenes(issues);
        ScanAllPrefabs(issues);
        return issues;
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

    private static void ScanBuildScenes(List<string> issues)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            EditorBuildSettingsScene scene = scenes[i];
            if (scene == null || !scene.enabled || string.IsNullOrWhiteSpace(scene.path))
            {
                continue;
            }

            ScanSerializedAssetForMissingScript(scene.path, issues);

            string[] dependencies = AssetDatabase.GetDependencies(scene.path, true);
            for (int dependencyIndex = 0; dependencyIndex < dependencies.Length; dependencyIndex++)
            {
                string dependency = dependencies[dependencyIndex];
                if (dependency.EndsWith(".prefab"))
                {
                    ScanSerializedAssetForMissingScript(dependency, issues);
                }
            }
        }
    }

    private static void ScanSerializedAssetForMissingScript(string assetPath, List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(assetPath) || !File.Exists(assetPath))
        {
            return;
        }

        string[] lines = File.ReadAllLines(assetPath);
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("m_Script: {fileID: 0}"))
            {
                issues.Add($"{assetPath}:{i + 1} missing script reference");
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
