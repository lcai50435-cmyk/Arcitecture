using System.Collections.Generic;
using UnityEngine;

public static class RuntimeSpriteDisplaySanitizer
{
    private const byte SimilarityTolerance = 18;
    private const byte AlphaThreshold = 10;
    private const int CropPadding = 1;

    private static readonly Dictionary<int, Sprite> sanitizedSpriteCache = new Dictionary<int, Sprite>();

    public static Sprite GetDisplaySprite(Sprite source)
    {
        if (source == null)
        {
            return null;
        }

        int cacheKey = source.GetInstanceID();
        if (sanitizedSpriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
        {
            return cached;
        }

        Sprite sanitized = CreateSanitizedSprite(source);
        sanitizedSpriteCache[cacheKey] = sanitized != null ? sanitized : source;
        return sanitizedSpriteCache[cacheKey];
    }

    private static Sprite CreateSanitizedSprite(Sprite source)
    {
        Rect textureRect = source.textureRect;
        int width = Mathf.RoundToInt(textureRect.width);
        int height = Mathf.RoundToInt(textureRect.height);
        if (width <= 1 || height <= 1)
        {
            return source;
        }

        Texture2D sourceTexture = GetReadableTexture(source.texture);
        if (sourceTexture == null)
        {
            return source;
        }

        Color32[] originalPixels = sourceTexture.GetPixels32();
        if (originalPixels == null || originalPixels.Length == 0)
        {
            return source;
        }

        int textureWidth = sourceTexture.width;
        Color32[] regionPixels = new Color32[width * height];
        int originX = Mathf.RoundToInt(textureRect.x);
        int originY = Mathf.RoundToInt(textureRect.y);

        for (int y = 0; y < height; y++)
        {
            int sourceRow = (originY + y) * textureWidth + originX;
            int targetRow = y * width;
            for (int x = 0; x < width; x++)
            {
                int sourceIndex = sourceRow + x;
                if (sourceIndex >= 0 && sourceIndex < originalPixels.Length)
                {
                    regionPixels[targetRow + x] = originalPixels[sourceIndex];
                }
            }
        }

        if (!TryExtractOpaqueRegion(regionPixels, width, height, out Color32[] sanitizedPixels, out RectInt opaqueRect))
        {
            return source;
        }

        Texture2D sanitizedTexture = new Texture2D(opaqueRect.width, opaqueRect.height, TextureFormat.RGBA32, false)
        {
            name = $"{source.name}_Sanitized",
            filterMode = sourceTexture.filterMode,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] croppedPixels = new Color32[opaqueRect.width * opaqueRect.height];
        for (int y = 0; y < opaqueRect.height; y++)
        {
            int sourceRow = (opaqueRect.y + y) * width + opaqueRect.x;
            int targetRow = y * opaqueRect.width;
            for (int x = 0; x < opaqueRect.width; x++)
            {
                croppedPixels[targetRow + x] = sanitizedPixels[sourceRow + x];
            }
        }

        sanitizedTexture.SetPixels32(croppedPixels);
        sanitizedTexture.Apply(false, false);

        Sprite sprite = Sprite.Create(
            sanitizedTexture,
            new Rect(0f, 0f, opaqueRect.width, opaqueRect.height),
            new Vector2(0.5f, 0.5f),
            source.pixelsPerUnit);
        sprite.name = sanitizedTexture.name;
        return sprite;
    }

    private static bool TryExtractOpaqueRegion(
        Color32[] pixels,
        int width,
        int height,
        out Color32[] sanitizedPixels,
        out RectInt opaqueRect)
    {
        sanitizedPixels = new Color32[pixels.Length];
        pixels.CopyTo(sanitizedPixels, 0);
        opaqueRect = default;

        bool[] backgroundMask = BuildBackgroundMask(pixels, width, height);

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (backgroundMask[index])
                {
                    sanitizedPixels[index] = new Color32(0, 0, 0, 0);
                    continue;
                }

                if (sanitizedPixels[index].a <= AlphaThreshold)
                {
                    sanitizedPixels[index] = new Color32(0, 0, 0, 0);
                    continue;
                }

                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return false;
        }

        minX = Mathf.Max(0, minX - CropPadding);
        minY = Mathf.Max(0, minY - CropPadding);
        maxX = Mathf.Min(width - 1, maxX + CropPadding);
        maxY = Mathf.Min(height - 1, maxY + CropPadding);
        opaqueRect = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        return true;
    }

    private static bool[] BuildBackgroundMask(Color32[] pixels, int width, int height)
    {
        bool[] visited = new bool[pixels.Length];
        bool[] backgroundMask = new bool[pixels.Length];
        Queue<int> queue = new Queue<int>();

        for (int x = 0; x < width; x++)
        {
            EnqueueBorderSeed(x, 0);
            EnqueueBorderSeed(x, height - 1);
        }

        for (int y = 1; y < height - 1; y++)
        {
            EnqueueBorderSeed(0, y);
            EnqueueBorderSeed(width - 1, y);
        }

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            int x = index % width;
            int y = index / width;
            Color32 seedColor = pixels[index];

            TryVisitNeighbour(x - 1, y, seedColor);
            TryVisitNeighbour(x + 1, y, seedColor);
            TryVisitNeighbour(x, y - 1, seedColor);
            TryVisitNeighbour(x, y + 1, seedColor);
        }

        return backgroundMask;

        void EnqueueBorderSeed(int x, int y)
        {
            int index = y * width + x;
            if (visited[index])
            {
                return;
            }

            Color32 pixel = pixels[index];
            if (pixel.a <= AlphaThreshold || IsLikelyBackgroundSeed(pixel))
            {
                visited[index] = true;
                backgroundMask[index] = true;
                queue.Enqueue(index);
            }
        }

        void TryVisitNeighbour(int x, int y, Color32 seedColor)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return;
            }

            int index = y * width + x;
            if (visited[index])
            {
                return;
            }

            Color32 pixel = pixels[index];
            if (pixel.a <= AlphaThreshold)
            {
                visited[index] = true;
                backgroundMask[index] = true;
                queue.Enqueue(index);
                return;
            }

            if (!IsSimilar(seedColor, pixel))
            {
                return;
            }

            visited[index] = true;
            backgroundMask[index] = true;
            queue.Enqueue(index);
        }
    }

    private static bool IsLikelyBackgroundSeed(Color32 color)
    {
        int max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        int min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        bool isNearWhite = min >= 225;
        bool isNearBlack = max <= 24;
        bool isLowSaturationLight = max >= 185 && max - min <= 24;
        return isNearWhite || isNearBlack || isLowSaturationLight;
    }

    private static bool IsSimilar(Color32 a, Color32 b)
    {
        return Mathf.Abs(a.r - b.r) <= SimilarityTolerance &&
               Mathf.Abs(a.g - b.g) <= SimilarityTolerance &&
               Mathf.Abs(a.b - b.b) <= SimilarityTolerance &&
               Mathf.Abs(a.a - b.a) <= 32;
    }

    private static Texture2D GetReadableTexture(Texture2D sourceTexture)
    {
        if (sourceTexture == null)
        {
            return null;
        }

        if (sourceTexture.isReadable)
        {
            return sourceTexture;
        }

        RenderTexture active = RenderTexture.active;
        RenderTexture temp = RenderTexture.GetTemporary(
            sourceTexture.width,
            sourceTexture.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear);

        try
        {
            Graphics.Blit(sourceTexture, temp);
            RenderTexture.active = temp;

            Texture2D readable = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false)
            {
                name = $"{sourceTexture.name}_ReadableCopy",
                filterMode = sourceTexture.filterMode,
                wrapMode = TextureWrapMode.Clamp
            };
            readable.ReadPixels(new Rect(0f, 0f, temp.width, temp.height), 0, 0);
            readable.Apply(false, false);
            return readable;
        }
        finally
        {
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(temp);
        }
    }
}
