using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ArcitectureWebGLBuildCommand
{
    private const string DefaultBuildPath = "Builds/WebGL";

    public static void Build()
    {
        string version = GetCommandLineValue("-arcitectureVersion", "0.0.0");
        string buildNumber = GetCommandLineValue("-arcitectureBuildNumber", "local");
        string gitSha = GetCommandLineValue("-arcitectureGitSha", "unknown");
        string gitDescribe = GetCommandLineValue("-arcitectureGitDescribe", gitSha);
        string outputPath = ResolveBuildPath();

        PlayerSettings.bundleVersion = version;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes found in EditorBuildSettings.");
        }

        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, true);
        }

        Directory.CreateDirectory(outputPath);

        Debug.Log($"Building Arcitecture WebGL {version} ({buildNumber}) from {gitDescribe} / {gitSha}");
        Debug.Log($"WebGL output path: {outputPath}");

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"WebGL build failed: {report.summary.result}");
        }

        Debug.Log($"WebGL build succeeded: {report.summary.totalSize} bytes");
    }

    private static string ResolveBuildPath()
    {
        string explicitPath = GetCommandLineValue("-arcitectureOutputPath", null)
            ?? GetCommandLineValue("-customBuildPath", null)
            ?? GetCommandLineValue("-buildPath", null);

        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return NormalizePath(explicitPath);
        }

        string buildsPath = GetCommandLineValue("-buildsPath", "Builds");
        string buildName = GetCommandLineValue("-buildName", "WebGL");

        if (!string.IsNullOrWhiteSpace(buildsPath) && !string.IsNullOrWhiteSpace(buildName))
        {
            return NormalizePath(Path.Combine(buildsPath, buildName));
        }

        return NormalizePath(DefaultBuildPath);
    }

    private static string GetCommandLineValue(string name, string fallback)
    {
        string[] args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }

        return fallback;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimEnd('/');
    }
}
