using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class RuntimeProjectSpriteLoader
{
    private const string RuntimeResourceRoot = "RuntimeProjectSprites";

    public static Sprite LoadSprite(
        string assetPath,
        bool usePointFilter = false,
        SpriteMeshType meshType = SpriteMeshType.Tight)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return null;
        }

#if UNITY_EDITOR
        Sprite editorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (editorSprite != null)
        {
            return editorSprite;
        }
#endif

        Sprite resourceSprite = LoadSyncedResourceSprite(assetPath, usePointFilter, meshType);
        if (resourceSprite != null)
        {
            return resourceSprite;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        return null;
#else
        if (!assetPath.StartsWith("Assets/"))
        {
            return null;
        }

        string relativePath = assetPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar);
        string absolutePath = Path.Combine(Application.dataPath, relativePath);
        if (!File.Exists(absolutePath))
        {
            return null;
        }

        byte[] bytes = File.ReadAllBytes(absolutePath);
        if (bytes == null || bytes.Length == 0)
        {
            return null;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            Object.Destroy(texture);
            return null;
        }

        texture.name = Path.GetFileNameWithoutExtension(assetPath);
        texture.filterMode = usePointFilter ? FilterMode.Point : FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Rect rect = new Rect(0f, 0f, texture.width, texture.height);
        Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 0, meshType);
        sprite.name = texture.name;
        return sprite;
#endif
    }

    private static Sprite LoadSyncedResourceSprite(
        string assetPath,
        bool usePointFilter,
        SpriteMeshType meshType)
    {
        string resourcePath = ToSyncedResourcePath(assetPath);
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            ApplyTextureSettings(sprite.texture, usePointFilter);
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            return null;
        }

        ApplyTextureSettings(texture, usePointFilter);
        Rect rect = new Rect(0f, 0f, texture.width, texture.height);
        sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 0, meshType);
        sprite.name = Path.GetFileNameWithoutExtension(assetPath);
        return sprite;
    }

    private static string ToSyncedResourcePath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath) || !assetPath.StartsWith("Assets/"))
        {
            return string.Empty;
        }

        string normalizedPath = assetPath.Replace('\\', '/');
        string extension = Path.GetExtension(normalizedPath);
        if (!string.IsNullOrEmpty(extension))
        {
            normalizedPath = normalizedPath.Substring(0, normalizedPath.Length - extension.Length);
        }

        return RuntimeResourceRoot + "/" + normalizedPath;
    }

    private static void ApplyTextureSettings(Texture2D texture, bool usePointFilter)
    {
        if (texture == null)
        {
            return;
        }

        texture.filterMode = usePointFilter ? FilterMode.Point : FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
    }
}
