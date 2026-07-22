using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class RuntimeUiSpriteFactory
{
    private const int DefaultTextureSize = 64;
    private const string RuntimeUiSpriteCatalogResourcePath = "UI/RuntimeUiSpriteCatalog";
    private const string MapFrameResourcePath = "UI/RuntimeMapFrame";
    private const string SpiritPanelFrameResourcePath = "UI/SpiritPanelFrame";
    private const string SaveBackgroundAssetPath = "Assets/File/Prop/UIProp/NewUI/Setting_5.png";
    private const string SavePanelFrameAssetPath = "Assets/File/Prop/UIProp/NewUI/Setting_12.png";
    private const string SavePreviewFrameAssetPath = "Assets/File/Prop/UIProp/NewUI/Setting_2.png";
    private const string SaveButtonFrameAssetPath = "Assets/File/Prop/UIProp/NewUI/Setting_8.png";
    private const string SaveCloseIconAssetPath = "Assets/File/Prop/UIProp/NewUI/Setting_10.png";
    private const string SaveDeleteIconAssetPath = "Assets/File/Prop/UIProp/NewUI/Dele.png";
    private const string SaveDividerAssetPath = "Assets/File/Prop/UIProp/NewUI/Setting_7.png";
    private const string SavePromptLineAssetPath = "Assets/File/Prop/UIProp/NewUI/Setting_6.png";
    private const string SaveBackgroundSpriteName = "Setting_5";
    private const string SavePanelFrameSpriteName = "Setting_12";
    private const string SavePreviewFrameSpriteName = "Setting_2";
    private const string SaveButtonFrameSpriteName = "Setting_8";
    private const string SaveCloseIconSpriteName = "Setting_10";
    private const string SaveDeleteIconSpriteName = "Dele";
    private const string SaveDividerSpriteName = "Setting_7";
    private const string SavePromptLineSpriteName = "Setting_6";
    private const string MainMenuStartButtonAssetPath = "Assets/File/MainSceneMaterial/G Home Page Atlas/Button Atlas/Start button.png";
    private const string MainMenuSettingsButtonAssetPath = "Assets/File/MainSceneMaterial/G Home Page Atlas/Button Atlas/Settings button.png";
    private const string MainMenuExitButtonAssetPath = "Assets/File/MainSceneMaterial/G Home Page Atlas/Button Atlas/Exit button.png";
    private const string MainMenuTextButtonFrameAssetPath = "Assets/File/MainSceneMaterial/G Home Page Atlas/Settings Popup Image/ButtonFrame.png";
    private const string MainMenuStartButtonSpriteName = "游戏开始_0";
    private const string MainMenuSettingsButtonSpriteName = "设置_0";
    private const string MainMenuExitButtonSpriteName = "退出按钮_0";
    private const string MainMenuTextButtonFrameSpriteName = "按钮框_0";

    private static readonly Dictionary<string, Sprite> RoundedSpriteCache = new Dictionary<string, Sprite>();
    private static readonly Rect MapFrameSpriteRectTopLeft = new Rect(17f, 16f, 38f, 34f);
    private static readonly Rect MainMenuStartButtonSpriteRect = new Rect(353f, 289f, 527f, 258f);
    private static readonly Rect MainMenuSettingsButtonSpriteRect = new Rect(400f, 231f, 430f, 149f);
    private static readonly Rect MainMenuExitButtonSpriteRect = new Rect(0f, 640f, 90f, 72f);
    private static readonly Rect MainMenuTextButtonFrameSpriteRect = new Rect(427f, 364f, 68f, 68f);
    private static readonly Vector4 MapFrameSpriteBorder = new Vector4(5f, 5f, 5f, 5f);
    private static readonly Vector4 SpiritPanelFrameBorder = new Vector4(12f, 12f, 12f, 12f);
    private static readonly Vector4 SavePanelFrameBorder = new Vector4(24f, 24f, 24f, 24f);
    private static readonly Vector4 SaveButtonFrameBorder = new Vector4(18f, 18f, 18f, 18f);
    private static readonly Vector4 MainMenuTextButtonFrameBorder = new Vector4(18f, 18f, 18f, 18f);

    private static Texture2D mapFrameTexture;
    private static Sprite mapFrameSprite;
    private static Sprite mapPanelFrameSprite;
    private static Texture2D spiritPanelFrameTexture;
    private static Sprite spiritPanelFrameSprite;
    private static RuntimeUiSpriteCatalog runtimeUiSpriteCatalog;
    private static Sprite saveBackgroundSprite;
    private static Sprite savePanelFrameSprite;
    private static Sprite savePreviewFrameSprite;
    private static Sprite saveButtonFrameSprite;
    private static Sprite saveCloseIconSprite;
    private static Sprite saveDeleteIconSprite;
    private static Sprite saveDividerSprite;
    private static Sprite savePromptLineSprite;
    private static Sprite settingPanelFrameSprite;
    private static Sprite settingButtonFrameSprite;
    private static Sprite mainMenuStartButtonSprite;
    private static Sprite mainMenuSettingsButtonSprite;
    private static Sprite mainMenuExitButtonSprite;
    private static Sprite mainMenuTextButtonFrameSprite;

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

    public static Sprite GetMapPanelFrameSprite()
    {
        if (mapPanelFrameSprite != null)
        {
            return mapPanelFrameSprite;
        }

        mapPanelFrameSprite = GetSettingPanelFrameSprite();
        if (mapPanelFrameSprite != null)
        {
            return mapPanelFrameSprite;
        }

        mapPanelFrameSprite = GetMapFrameSprite();
        return mapPanelFrameSprite;
    }

    public static bool TryGetMapPanelFrameRenderData(out Texture2D texture, out Rect sourceRect, out Vector4 sourceBorder)
    {
        Sprite sprite = GetMapPanelFrameSprite();
        if (sprite != null && sprite.texture != null)
        {
            texture = sprite.texture;
            sourceRect = sprite.rect;
            sourceBorder = sprite.border;
            return true;
        }

        texture = null;
        sourceRect = Rect.zero;
        sourceBorder = Vector4.zero;
        return false;
    }

    public static void ApplyMapFrameSprite(Image image, Color color)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = GetMapPanelFrameSprite();
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

    public static void ApplySavePanelFrameSprite(Image image, Color fallbackColor)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = GetSavePanelFrameSprite();
        if (sprite == null)
        {
            ApplyRoundedSprite(image, fallbackColor, 10, 10, 1.2f);
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = Color.white;
    }

    public static void ApplySaveBackgroundSprite(Image image, Color fallbackColor)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = GetSaveBackgroundSprite();
        if (sprite == null)
        {
            ApplyRoundedSprite(image, fallbackColor, 10, 10, 1.2f);
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
    }

    public static void ApplySavePreviewFrameSprite(Image image, Color fallbackColor)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = GetSavePreviewFrameSprite();
        if (sprite == null)
        {
            ApplyRoundedSprite(image, fallbackColor, 4, 6, 1f);
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
    }

    public static void ApplySaveButtonFrameSprite(Image image, Color fallbackColor)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = GetSaveButtonFrameSprite();
        if (sprite == null)
        {
            ApplyRoundedSprite(image, fallbackColor, 6, 8, 1.1f);
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = Color.white;
    }

    public static void ApplySettingPanelFrameSprite(Image image, Color fallbackColor)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = GetSettingPanelFrameSprite();
        if (sprite == null)
        {
            ApplyRoundedSprite(image, fallbackColor, 10, 10, 1.2f);
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = fallbackColor;
    }

    public static void ApplySettingButtonFrameSprite(Image image, Color fallbackColor)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = GetSettingButtonFrameSprite();
        if (sprite == null)
        {
            ApplyRoundedSprite(image, fallbackColor, 6, 8, 1.1f);
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = fallbackColor;
    }

    public static Sprite GetSaveCloseIconSprite()
    {
        if (saveCloseIconSprite != null)
        {
            return saveCloseIconSprite;
        }

        saveCloseIconSprite = ResolveSpriteFromLoadedOrProjectAsset(
            SaveCloseIconSpriteName,
            SaveCloseIconAssetPath,
            Rect.zero,
            Vector4.zero,
            "SaveCloseIconSprite");
        return saveCloseIconSprite;
    }

    public static Sprite GetSaveDeleteIconSprite()
    {
        if (saveDeleteIconSprite != null)
        {
            return saveDeleteIconSprite;
        }

        saveDeleteIconSprite = ResolveSpriteFromLoadedOrProjectAsset(
            SaveDeleteIconSpriteName,
            SaveDeleteIconAssetPath,
            Rect.zero,
            Vector4.zero,
            "SaveDeleteIconSprite");
        return saveDeleteIconSprite;
    }

    public static Sprite GetSaveDividerSprite()
    {
        if (saveDividerSprite != null)
        {
            return saveDividerSprite;
        }

        saveDividerSprite = ResolveSpriteFromLoadedOrProjectAsset(
            SaveDividerSpriteName,
            SaveDividerAssetPath,
            Rect.zero,
            Vector4.zero,
            "SaveDividerSprite");
        return saveDividerSprite;
    }

    public static Sprite GetSavePromptLineSprite()
    {
        if (savePromptLineSprite != null)
        {
            return savePromptLineSprite;
        }

        savePromptLineSprite = ResolveSpriteFromLoadedOrProjectAsset(
            SavePromptLineSpriteName,
            SavePromptLineAssetPath,
            Rect.zero,
            Vector4.zero,
            "SavePromptLineSprite");
        return savePromptLineSprite;
    }

    public static Sprite GetMainMenuStartButtonSprite()
    {
        Sprite catalogSprite = GetRuntimeUiSpriteCatalog()?.MainMenuStartButton;
        if (catalogSprite != null)
        {
            return catalogSprite;
        }

        return ResolveMainMenuButtonSprite(
            ref mainMenuStartButtonSprite,
            MainMenuStartButtonSpriteName,
            MainMenuStartButtonAssetPath,
            MainMenuStartButtonSpriteRect,
            "MainMenuStartButtonSprite");
    }

    public static Sprite GetMainMenuSettingsButtonSprite()
    {
        Sprite catalogSprite = GetRuntimeUiSpriteCatalog()?.MainMenuSettingsButton;
        if (catalogSprite != null)
        {
            return catalogSprite;
        }

        return ResolveMainMenuButtonSprite(
            ref mainMenuSettingsButtonSprite,
            MainMenuSettingsButtonSpriteName,
            MainMenuSettingsButtonAssetPath,
            MainMenuSettingsButtonSpriteRect,
            "MainMenuSettingsButtonSprite");
    }

    public static Sprite GetMainMenuExitButtonSprite()
    {
        Sprite catalogSprite = GetRuntimeUiSpriteCatalog()?.MainMenuExitButton;
        if (catalogSprite != null)
        {
            return catalogSprite;
        }

        return ResolveMainMenuButtonSprite(
            ref mainMenuExitButtonSprite,
            MainMenuExitButtonSpriteName,
            MainMenuExitButtonAssetPath,
            MainMenuExitButtonSpriteRect,
            "MainMenuExitButtonSprite");
    }

    public static void ApplyMainMenuTextButtonFrameSprite(Image image, Color fallbackColor)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = GetMainMenuTextButtonFrameSprite();
        if (sprite == null)
        {
            ApplyRoundedSprite(image, fallbackColor, 10, 10, 1.1f);
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = Color.white;
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

    private static Sprite GetSavePanelFrameSprite()
    {
        if (savePanelFrameSprite != null)
        {
            return savePanelFrameSprite;
        }

        savePanelFrameSprite = ResolveSpriteFromLoadedOrProjectAsset(
            SavePanelFrameSpriteName,
            SavePanelFrameAssetPath,
            Rect.zero,
            SavePanelFrameBorder,
            "SavePanelFrameSprite");
        return savePanelFrameSprite;
    }

    private static Sprite GetSaveBackgroundSprite()
    {
        if (saveBackgroundSprite != null)
        {
            return saveBackgroundSprite;
        }

        saveBackgroundSprite = ResolveSpriteFromLoadedOrProjectAsset(
            SaveBackgroundSpriteName,
            SaveBackgroundAssetPath,
            Rect.zero,
            Vector4.zero,
            "SaveBackgroundSprite");
        return saveBackgroundSprite;
    }

    private static Sprite GetSavePreviewFrameSprite()
    {
        if (savePreviewFrameSprite != null)
        {
            return savePreviewFrameSprite;
        }

        savePreviewFrameSprite = ResolveSpriteFromLoadedOrProjectAsset(
            SavePreviewFrameSpriteName,
            SavePreviewFrameAssetPath,
            Rect.zero,
            Vector4.zero,
            "SavePreviewFrameSprite");
        return savePreviewFrameSprite;
    }

    private static Sprite GetSaveButtonFrameSprite()
    {
        if (saveButtonFrameSprite != null)
        {
            return saveButtonFrameSprite;
        }

        saveButtonFrameSprite = ResolveSpriteFromLoadedOrProjectAsset(
            SaveButtonFrameSpriteName,
            SaveButtonFrameAssetPath,
            Rect.zero,
            SaveButtonFrameBorder,
            "SaveButtonFrameSprite");
        return saveButtonFrameSprite;
    }

    private static Sprite GetSettingPanelFrameSprite()
    {
        if (settingPanelFrameSprite != null)
        {
            return settingPanelFrameSprite;
        }

        Sprite catalogSprite = GetRuntimeUiSpriteCatalog()?.SettingPanelFrame;
        if (catalogSprite != null)
        {
            settingPanelFrameSprite = CreateSlicedSpriteFromSource(catalogSprite, SavePanelFrameBorder, "SettingPanelFrameSprite");
            return settingPanelFrameSprite;
        }

        settingPanelFrameSprite = ResolveSpriteFromLoadedOrProjectAsset(
            SavePanelFrameSpriteName,
            SavePanelFrameAssetPath,
            Rect.zero,
            SavePanelFrameBorder,
            "SettingPanelFrameSprite");
        return settingPanelFrameSprite;
    }

    private static Sprite GetSettingButtonFrameSprite()
    {
        if (settingButtonFrameSprite != null)
        {
            return settingButtonFrameSprite;
        }

        Sprite catalogSprite = GetRuntimeUiSpriteCatalog()?.SettingButtonFrame;
        if (catalogSprite != null)
        {
            settingButtonFrameSprite = CreateSlicedSpriteFromSource(catalogSprite, SaveButtonFrameBorder, "SettingButtonFrameSprite");
            return settingButtonFrameSprite;
        }

        settingButtonFrameSprite = ResolveSpriteFromLoadedOrProjectAsset(
            SaveButtonFrameSpriteName,
            SaveButtonFrameAssetPath,
            Rect.zero,
            SaveButtonFrameBorder,
            "SettingButtonFrameSprite");
        return settingButtonFrameSprite;
    }

    private static RuntimeUiSpriteCatalog GetRuntimeUiSpriteCatalog()
    {
        if (runtimeUiSpriteCatalog != null)
        {
            return runtimeUiSpriteCatalog;
        }

        runtimeUiSpriteCatalog = Resources.Load<RuntimeUiSpriteCatalog>(RuntimeUiSpriteCatalogResourcePath);
        return runtimeUiSpriteCatalog;
    }

    private static Sprite GetMainMenuTextButtonFrameSprite()
    {
        if (mainMenuTextButtonFrameSprite != null)
        {
            return mainMenuTextButtonFrameSprite;
        }

        Sprite catalogSprite = GetRuntimeUiSpriteCatalog()?.MainMenuTextButtonFrame;
        if (catalogSprite != null && catalogSprite.texture != null)
        {
            mainMenuTextButtonFrameSprite = CreateSlicedSpriteFromSource(catalogSprite, MainMenuTextButtonFrameBorder, "MainMenuTextButtonFrameSprite");
            return mainMenuTextButtonFrameSprite;
        }

        mainMenuTextButtonFrameSprite = ResolveSpriteFromLoadedOrProjectAsset(
            MainMenuTextButtonFrameSpriteName,
            MainMenuTextButtonFrameAssetPath,
            MainMenuTextButtonFrameSpriteRect,
            MainMenuTextButtonFrameBorder,
            "MainMenuTextButtonFrameSprite");
        return mainMenuTextButtonFrameSprite;
    }

    private static Sprite ResolveMainMenuButtonSprite(
        ref Sprite cache,
        string loadedSpriteName,
        string assetPath,
        Rect spriteRect,
        string generatedName)
    {
        if (cache != null)
        {
            return cache;
        }

        cache = ResolveSpriteFromLoadedOrProjectAsset(
            loadedSpriteName,
            assetPath,
            spriteRect,
            Vector4.zero,
            generatedName);
        return cache;
    }

    private static Sprite ResolveSpriteFromLoadedOrProjectAsset(
        string loadedSpriteName,
        string assetPath,
        Rect spriteRect,
        Vector4 border,
        string generatedName)
    {
        Sprite loadedSprite = FindLoadedSprite(loadedSpriteName);
        if (loadedSprite != null)
        {
            return CreateSlicedSprite(loadedSprite.texture, loadedSprite.rect, loadedSprite.pivot, loadedSprite.pixelsPerUnit, border, generatedName);
        }

        Sprite projectSprite = RuntimeProjectSpriteLoader.LoadSprite(assetPath, true, SpriteMeshType.FullRect);
        if (projectSprite == null || projectSprite.texture == null)
        {
            return null;
        }

        Rect rect = spriteRect.width > 0f && spriteRect.height > 0f
            ? ResolveSpriteRect(projectSprite.texture, spriteRect)
            : projectSprite.rect;
        return CreateSlicedSprite(projectSprite.texture, rect, new Vector2(0.5f, 0.5f), 100f, border, generatedName);
    }

    private static Sprite CreateSlicedSpriteFromSource(Sprite source, Vector4 border, string generatedName)
    {
        if (source == null || source.texture == null)
        {
            return null;
        }

        return CreateSlicedSprite(source.texture, source.rect, source.pivot, source.pixelsPerUnit, border, generatedName);
    }

    private static Sprite FindLoadedSprite(string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            return null;
        }

        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite != null && string.Equals(sprite.name, spriteName, StringComparison.Ordinal))
            {
                return sprite;
            }
        }

        return null;
    }

    private static Rect ResolveSpriteRect(Texture2D texture, Rect requestedRect)
    {
        if (texture == null)
        {
            return requestedRect;
        }

        float x = Mathf.Clamp(requestedRect.x, 0f, texture.width - 1f);
        float y = Mathf.Clamp(requestedRect.y, 0f, texture.height - 1f);
        float width = Mathf.Clamp(requestedRect.width, 1f, texture.width - x);
        float height = Mathf.Clamp(requestedRect.height, 1f, texture.height - y);
        return new Rect(x, y, width, height);
    }

    private static Sprite CreateSlicedSprite(
        Texture2D texture,
        Rect rect,
        Vector2 pivot,
        float pixelsPerUnit,
        Vector4 border,
        string spriteName)
    {
        if (texture == null)
        {
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Sprite sprite = Sprite.Create(
            texture,
            rect,
            pivot,
            pixelsPerUnit,
            0u,
            SpriteMeshType.FullRect,
            border);
        sprite.name = spriteName;
        return sprite;
    }

    private static float SignedDistanceToRoundedBox(Vector2 point, Vector2 halfExtents, float radius)
    {
        Vector2 q = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - halfExtents + new Vector2(radius, radius);
        Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
        float inside = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
        return outside.magnitude + inside - radius;
    }
}
