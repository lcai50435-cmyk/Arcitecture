using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class RuntimeProjectSpriteLoader
{
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
}
