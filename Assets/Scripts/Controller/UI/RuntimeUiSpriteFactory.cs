using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class RuntimeUiSpriteFactory
{
    private const int DefaultTextureSize = 64;
    private const string MapFrameResourcePath = "UI/RuntimeMapFrame";
    private const string SpiritPanelFrameResourcePath = "UI/SpiritPanelFrame";

    private static readonly Dictionary<string, Sprite> RoundedSpriteCache = new Dictionary<string, Sprite>();
    private static readonly Rect MapFrameSpriteRectTopLeft = new Rect(17f, 16f, 38f, 34f);
    private static readonly Vector4 MapFrameSpriteBorder = new Vector4(5f, 5f, 5f, 5f);
    private static readonly Vector4 SpiritPanelFrameBorder = new Vector4(12f, 12f, 12f, 12f);

    private static Texture2D mapFrameTexture;
    private static Sprite mapFrameSprite;
    private static Texture2D spiritPanelFrameTexture;
    private static Sprite spiritPanelFrameSprite;

    public static void ApplyRoundedSprite(
        Image image,
        Color color,
        int radius = 10,
        int border = 12,
        float feather = 1.5f)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = GetRoundedSprite(DefaultTextureSize, radius, border, feather);
        image.type = Image.Type.Sliced;
        image.color = color;
    }

    public static Sprite GetRoundedSprite(int size, int radius, int border, float feather = 1.5f)
    {
        string cacheKey = $"{size}_{radius}_{border}_{feather:0.00}";
        if (RoundedSpriteCache.TryGetValue(cacheKey, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = $"RuntimeRoundedTexture_{cacheKey}",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float halfSize = size * 0.5f - 1.5f;
        Vector2 halfExtents = new Vector2(halfSize, halfSize);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f);
                float signedDistance = SignedDistanceToRoundedBox(point, halfExtents, radius);
                float alpha = signedDistance <= 0f
                    ? 1f
                    : Mathf.Clamp01(1f - signedDistance / Mathf.Max(0.001f, feather));

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        Vector4 spriteBorder = new Vector4(border, border, border, border);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect,
            spriteBorder);
        sprite.name = $"RuntimeRoundedSprite_{cacheKey}";

        RoundedSpriteCache[cacheKey] = sprite;
        return sprite;
    }

    public static Texture2D GetMapFrameTexture()
    {
        if (mapFrameTexture != null)
        {
            return mapFrameTexture;
        }

        mapFrameTexture = Resources.Load<Texture2D>(MapFrameResourcePath);
        if (mapFrameTexture != null)
        {
            mapFrameTexture.filterMode = FilterMode.Point;
            mapFrameTexture.wrapMode = TextureWrapMode.Clamp;
        }

        return mapFrameTexture;
    }

    public static Texture2D GetSpiritPanelFrameTexture()
    {
        if (spiritPanelFrameTexture != null)
        {
            return spiritPanelFrameTexture;
        }

        spiritPanelFrameTexture = Resources.Load<Texture2D>(SpiritPanelFrameResourcePath);
        if (spiritPanelFrameTexture != null)
        {
            spiritPanelFrameTexture.filterMode = FilterMode.Point;
            spiritPanelFrameTexture.wrapMode = TextureWrapMode.Clamp;
        }

        return spiritPanelFrameTexture;
    }

    public static Rect GetMapFrameUvRect()
    {
        Texture2D texture = GetMapFrameTexture();
        if (texture == null)
        {
            return new Rect(0f, 0f, 1f, 1f);
        }

        Rect spriteRect = GetMapFrameSpriteRect(texture);
        return new Rect(
            spriteRect.x / texture.width,
            spriteRect.y / texture.height,
            spriteRect.width / texture.width,
            spriteRect.height / texture.height);
    }

    public static Rect GetMapFramePixelRect()
    {
        Texture2D texture = GetMapFrameTexture();
        if (texture == null)
        {
            return new Rect(0f, 0f, 0f, 0f);
        }

        return GetMapFrameSpriteRect(texture);
    }

    public static Vector4 GetMapFrameBorder()
    {
        return MapFrameSpriteBorder;
    }

    public static Sprite GetMapFrameSprite()
    {
        if (mapFrameSprite != null)
        {
            return mapFrameSprite;
        }

        Texture2D texture = GetMapFrameTexture();
        if (texture == null)
        {
            return null;
        }

        Rect spriteRect = GetMapFrameSpriteRect(texture);
        mapFrameSprite = Sprite.Create(
            texture,
            spriteRect,
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect,
            MapFrameSpriteBorder);
        mapFrameSprite.name = "RuntimeMapFrameSprite";
        return mapFrameSprite;
    }

    public static void ApplyMapFrameSprite(Image image, Color color)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = GetMapFrameSprite();
        if (sprite == null)
        {
            ApplyRoundedSprite(image, color, 10, 10, 1.2f);
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
    }

    public static Sprite GetSpiritPanelFrameSprite()
    {
        if (spiritPanelFrameSprite != null)
        {
            return spiritPanelFrameSprite;
        }

        Texture2D texture = GetSpiritPanelFrameTexture();
        if (texture == null)
        {
            return null;
        }

        spiritPanelFrameSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect,
            SpiritPanelFrameBorder);
        spiritPanelFrameSprite.name = "SpiritPanelFrameSprite";
        return spiritPanelFrameSprite;
    }

    public static void ApplySpiritPanelFrameSprite(Image image, Color color)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = GetSpiritPanelFrameSprite();
        if (sprite == null)
        {
            ApplyRoundedSprite(image, color, 12, 12, 1.2f);
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
    }

    private static Rect GetMapFrameSpriteRect(Texture2D texture)
    {
        float x = Mathf.Clamp(MapFrameSpriteRectTopLeft.x, 0f, texture.width - 1f);
        float top = Mathf.Clamp(MapFrameSpriteRectTopLeft.y, 0f, texture.height - 1f);
        float width = Mathf.Clamp(MapFrameSpriteRectTopLeft.width, 1f, texture.width - x);
        float height = Mathf.Clamp(MapFrameSpriteRectTopLeft.height, 1f, texture.height - top);
        float y = Mathf.Clamp(texture.height - top - height, 0f, texture.height - height);
        return new Rect(x, y, width, height);
    }

    private static float SignedDistanceToRoundedBox(Vector2 point, Vector2 halfExtents, float radius)
    {
        Vector2 q = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - halfExtents + new Vector2(radius, radius);
        Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
        float inside = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
        return outside.magnitude + inside - radius;
    }
}
