using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ArcitectureWebGLBuildCommand
{
    private const string VersionArgument = "-arcitectureVersion";
    private const string DefaultBuildPath = "Builds/WebGL";
    private const string DefaultVersion = "3.2.0";

    public static void Build()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputPath = ResolveBuildPath(projectRoot);
        string version = ResolveVersion();
        string previousVersion = PlayerSettings.bundleVersion;
        WebGLCompressionFormat previousCompression = PlayerSettings.WebGL.compressionFormat;
        bool previousDecompressionFallback = PlayerSettings.WebGL.decompressionFallback;
        int previousInitialMemorySize = PlayerSettings.WebGL.initialMemorySize;
        int previousMemorySize = PlayerSettings.WebGL.memorySize;
        int previousMaximumMemorySize = PlayerSettings.WebGL.maximumMemorySize;

        try
        {
            PlayerSettings.bundleVersion = version;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.initialMemorySize = Math.Max(PlayerSettings.WebGL.initialMemorySize, 128);
            PlayerSettings.WebGL.memorySize = Math.Max(PlayerSettings.WebGL.memorySize, 128);
            PlayerSettings.WebGL.maximumMemorySize = Math.Max(PlayerSettings.WebGL.maximumMemorySize, 2048);

            RuntimeBuildInfoGenerator.RefreshSnapshot();
            BuildPlayer(projectRoot, outputPath);
        }
        finally
        {
            PlayerSettings.bundleVersion = previousVersion;
            PlayerSettings.WebGL.compressionFormat = previousCompression;
            PlayerSettings.WebGL.decompressionFallback = previousDecompressionFallback;
            PlayerSettings.WebGL.initialMemorySize = previousInitialMemorySize;
            PlayerSettings.WebGL.memorySize = previousMemorySize;
            PlayerSettings.WebGL.maximumMemorySize = previousMaximumMemorySize;
        }
    }

    private static void BuildPlayer(string projectRoot, string outputPath)
    {
        string[] scenes = ResolveEnabledScenes();
        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("WebGL build failed: no enabled scenes in Build Settings.");
        }

        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, true);
        }

        Directory.CreateDirectory(outputPath);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"WebGL build failed: {summary.result}, errors {summary.totalErrors}.");
        }

        string relativeOutput = MakeRelativePath(projectRoot, outputPath);
        Debug.Log($"WebGL build completed: {relativeOutput}, size {summary.totalSize} bytes.");
    }

    private static string[] ResolveEnabledScenes()
    {
        EditorBuildSettingsScene[] configuredScenes = EditorBuildSettings.scenes;
        int enabledCount = 0;
        for (int i = 0; i < configuredScenes.Length; i++)
        {
            if (configuredScenes[i] != null && configuredScenes[i].enabled)
            {
                enabledCount++;
            }
        }

        string[] scenes = new string[enabledCount];
        int index = 0;
        for (int i = 0; i < configuredScenes.Length; i++)
        {
            EditorBuildSettingsScene scene = configuredScenes[i];
            if (scene == null || !scene.enabled)
            {
                continue;
            }

            scenes[index] = scene.path;
            index++;
        }

        return scenes;
    }

    private static string ResolveBuildPath(string projectRoot)
    {
        string path = ReadArgument("-customBuildPath");
        if (string.IsNullOrWhiteSpace(path))
        {
            path = ReadArgument("-buildPath");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            path = Environment.GetEnvironmentVariable("ARCITECTURE_BUILD_PATH");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            path = DefaultBuildPath;
        }

        return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(projectRoot, path));
    }

    private static string ResolveVersion()
    {
        string version = ReadArgument(VersionArgument);
        if (string.IsNullOrWhiteSpace(version))
        {
            version = Environment.GetEnvironmentVariable("ARCITECTURE_BUILD_VERSION");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            version = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            version = PlayerSettings.bundleVersion;
        }

        version = version.Trim();
        if (version.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            version = version.Substring(1);
        }

        return string.IsNullOrWhiteSpace(version) ? DefaultVersion : version;
    }

    private static string ReadArgument(string argumentName)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (string.Equals(arg, argumentName, StringComparison.OrdinalIgnoreCase))
            {
                return i + 1 < args.Length ? args[i + 1] : string.Empty;
            }

            string prefix = argumentName + "=";
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return arg.Substring(prefix.Length);
            }
        }

        return string.Empty;
    }

    private static string MakeRelativePath(string projectRoot, string outputPath)
    {
        Uri rootUri = new Uri(projectRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? projectRoot
            : projectRoot + Path.DirectorySeparatorChar);
        Uri outputUri = new Uri(outputPath);
        return Uri.UnescapeDataString(rootUri.MakeRelativeUri(outputUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
    }
}
