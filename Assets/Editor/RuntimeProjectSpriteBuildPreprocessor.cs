using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class RuntimeProjectSpriteBuildPreprocessor : IPreprocessBuildWithReport
{
    private const string ResourceRoot = "Assets/Resources/RuntimeProjectSprites";
    private static readonly Regex ProjectSpritePathRegex = new Regex(
        "\"(Assets/[^\"]+\\.(?:png|jpg|jpeg|webp|psd))\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        SyncRuntimeProjectSprites();
    }

    [MenuItem("Tools/Build/Sync Runtime Project Sprites")]
    public static void SyncRuntimeProjectSprites()
    {
        HashSet<string> assetPaths = CollectProjectSpritePaths();
        int copiedCount = 0;

        foreach (string assetPath in assetPaths)
        {
            if (!File.Exists(assetPath))
            {
                Debug.LogWarning($"RuntimeProjectSpriteBuildPreprocessor: missing source asset {assetPath}");
                continue;
            }

            string targetPath = ResolveResourceAssetPath(assetPath);
            string targetDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            if (!File.Exists(targetPath) || !FilesMatch(assetPath, targetPath))
            {
                File.Copy(assetPath, targetPath, true);
                CopyMetaFile(assetPath, targetPath);
                copiedCount++;
            }
            else
            {
                CopyMetaFile(assetPath, targetPath);
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"RuntimeProjectSpriteBuildPreprocessor: synced {assetPaths.Count} runtime sprite assets, copied {copiedCount}.");
    }

    private static HashSet<string> CollectProjectSpritePaths()
    {
        HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { "Assets/Scripts" });
        for (int i = 0; i < scriptGuids.Length; i++)
        {
            string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);
            if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
            {
                continue;
            }

            string content = File.ReadAllText(scriptPath);
            MatchCollection matches = ProjectSpritePathRegex.Matches(content);
            for (int matchIndex = 0; matchIndex < matches.Count; matchIndex++)
            {
                string assetPath = matches[matchIndex].Groups[1].Value.Replace('\\', '/');
                if (File.Exists(assetPath))
                {
                    paths.Add(assetPath);
                }
            }
        }

        return paths;
    }

    private static string ResolveResourceAssetPath(string sourceAssetPath)
    {
        return Path.Combine(ResourceRoot, sourceAssetPath).Replace('\\', '/');
    }

    private static bool FilesMatch(string leftPath, string rightPath)
    {
        FileInfo left = new FileInfo(leftPath);
        FileInfo right = new FileInfo(rightPath);
        if (left.Length != right.Length)
        {
            return false;
        }

        return File.GetLastWriteTimeUtc(leftPath) <= File.GetLastWriteTimeUtc(rightPath);
    }

    private static void CopyMetaFile(string sourceAssetPath, string targetAssetPath)
    {
        string sourceMetaPath = sourceAssetPath + ".meta";
        if (!File.Exists(sourceMetaPath))
        {
            return;
        }

        string targetMetaPath = targetAssetPath + ".meta";
        string targetDirectory = Path.GetDirectoryName(targetMetaPath);
        if (!string.IsNullOrEmpty(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        string targetGuid = ResolveTargetMetaGuid(targetMetaPath);
        string metaContent = File.ReadAllText(sourceMetaPath);
        metaContent = Regex.Replace(
            metaContent,
            "^guid: [0-9a-fA-F]+$",
            "guid: " + targetGuid,
            RegexOptions.Multiline);
        File.WriteAllText(targetMetaPath, metaContent);
    }

    private static string ResolveTargetMetaGuid(string targetMetaPath)
    {
        if (File.Exists(targetMetaPath))
        {
            string existingMeta = File.ReadAllText(targetMetaPath);
            Match match = Regex.Match(existingMeta, "^guid: ([0-9a-fA-F]+)$", RegexOptions.Multiline);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return GUID.Generate().ToString();
    }
}
