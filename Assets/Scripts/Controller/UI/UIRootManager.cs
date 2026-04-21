using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIRootManager : MonoBehaviour
{
    public static UIRootManager Instance;

    [Header("图鉴主页")]
    public CanvasGroup handbookUI;

    [Header("详细信息页")]
    public CanvasGroup detailUIPage1;
    public CanvasGroup detailUIPage2;

    [Header("提交窗口 - 三个建筑分别一个")]
    public CanvasGroup submitSelectionUI1;
    public CanvasGroup submitSelectionUI2;
    public CanvasGroup submitSelectionUI3;

    [Header("Dialog弹窗")]
    public CanvasGroup dialogUI;

    [Header("场景交互提示UI")]
    public CanvasGroup interactTipUI;

    [Header("背包UI（可选）")]
    public CanvasGroup backpackUI;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        HideHandbook();
        HideAllDetail();
        HideAllSubmitSelection();
        HideDialog();

        ShowInteractTip();

        if (backpackUI != null)
        {
            ShowBackpack();
        }
    }

    private void SetUI(CanvasGroup cg, bool active, string name)
    {
        if (cg == null)
        {
            Debug.LogWarning($"{name} 没绑定 CanvasGroup");
            return;
        }

        // 关键：显示时强制确保物体本身激活
        if (active && !cg.gameObject.activeSelf)
        {
            cg.gameObject.SetActive(true);
        }

        cg.alpha = active ? 1f : 0f;
        cg.interactable = active;
        cg.blocksRaycasts = active;

    }

    // ========= 图鉴主页 =========
    public void ShowHandbook() => SetUI(handbookUI, true, "HandbookUI");
    public void HideHandbook() => SetUI(handbookUI, false, "HandbookUI");

    // ========= 详细页 =========
    public void ShowDetailPage1()
    {
        SetUI(detailUIPage1, true, "DetailUIPage1");
        SetUI(detailUIPage2, false, "DetailUIPage2");
    }

    public void ShowDetailPage2()
    {
        SetUI(detailUIPage1, false, "DetailUIPage1");
        SetUI(detailUIPage2, true, "DetailUIPage2");
    }

    public void HideAllDetail()
    {
        SetUI(detailUIPage1, false, "DetailUIPage1");
        SetUI(detailUIPage2, false, "DetailUIPage2");
    }

    // ========= 提交窗口 =========
    public void ShowSubmitSelection(int buildingIndex)
    {
        HideAllSubmitSelection();

        switch (buildingIndex)
        {
            case 0:
                SetUI(submitSelectionUI1, true, "SubmitSelectionUI1");
                break;
            case 1:
                SetUI(submitSelectionUI2, true, "SubmitSelectionUI2");
                break;
            case 2:
                SetUI(submitSelectionUI3, true, "SubmitSelectionUI3");
                break;
            default:
                Debug.LogWarning($"未知的提交窗口索引: {buildingIndex}");
                break;
        }
    }

    public void HideSubmitSelection(int buildingIndex)
    {
        switch (buildingIndex)
        {
            case 0:
                SetUI(submitSelectionUI1, false, "SubmitSelectionUI1");
                break;
            case 1:
                SetUI(submitSelectionUI2, false, "SubmitSelectionUI2");
                break;
            case 2:
                SetUI(submitSelectionUI3, false, "SubmitSelectionUI3");
                break;
            default:
                Debug.LogWarning($"未知的提交窗口索引: {buildingIndex}");
                break;
        }
    }

    public void HideAllSubmitSelection()
    {
        SetUI(submitSelectionUI1, false, "SubmitSelectionUI1");
        SetUI(submitSelectionUI2, false, "SubmitSelectionUI2");
        SetUI(submitSelectionUI3, false, "SubmitSelectionUI3");
    }

    // ========= Dialog =========
    public void ShowDialog() => SetUI(dialogUI, true, "DialogUI");
    public void HideDialog() => SetUI(dialogUI, false, "DialogUI");

    // ========= 交互提示 =========
    public void ShowInteractTip() => SetUI(interactTipUI, true, "InteractTipUI");
    public void HideInteractTip() => SetUI(interactTipUI, false, "InteractTipUI");

    // ========= 背包 =========
    public void ShowBackpack() => SetUI(backpackUI, true, "BackpackUI");
    public void HideBackpack() => SetUI(backpackUI, false, "BackpackUI");

    // ========= 常用组合 =========
    public void OpenHandbookView()
    {
        ShowHandbook();
        HideAllDetail();
        HideAllSubmitSelection();
        HideDialog();
        HideInteractTip();
    }

    public void OpenDetailViewPage1()
    {
        HideHandbook();
        ShowDetailPage1();
        HideAllSubmitSelection();
        HideDialog();
        HideInteractTip();
    }

    public void OpenDetailViewPage2()
    {
        HideHandbook();
        ShowDetailPage2();
        HideAllSubmitSelection();
        HideDialog();
        HideInteractTip();
    }

    public void CloseAllBookUI()
    {
        HideHandbook();
        HideAllDetail();
        HideAllSubmitSelection();
        HideDialog();
        ShowInteractTip();
    }

    public bool IsAnyGameplayBlockingUIOpen()
    {
        return
            IsCanvasGroupOpen(handbookUI) ||
            IsCanvasGroupOpen(detailUIPage1) ||
            IsCanvasGroupOpen(detailUIPage2) ||
            IsCanvasGroupOpen(submitSelectionUI1) ||
            IsCanvasGroupOpen(submitSelectionUI2) ||
            IsCanvasGroupOpen(submitSelectionUI3) ||
            IsCanvasGroupOpen(dialogUI) ||
            RuntimePauseMenu.IsPauseOpen;
    }

    private bool IsCanvasGroupOpen(CanvasGroup cg)
    {
        if (cg == null) return false;

        return cg.alpha > 0.01f && cg.blocksRaycasts;
    }


}

public class RuntimePauseMenu : MonoBehaviour
{
    private const string CanvasName = "RuntimePauseMenuCanvas";
    private const int SortingOrder = 280;

    private static readonly Color OverlayColor = new Color(0.02f, 0.03f, 0.05f, 0.76f);
    private static readonly Color PanelColor = new Color(0.10f, 0.12f, 0.16f, 0.96f);
    private static readonly Color BorderColor = new Color(0.33f, 0.45f, 0.55f, 1f);
    private static readonly Color ButtonColor = new Color(0.86f, 0.67f, 0.34f, 1f);
    private static readonly Color ButtonTextColor = new Color(0.14f, 0.09f, 0.05f, 1f);
    private static readonly Color TitleColor = new Color(0.95f, 0.97f, 1f, 1f);
    private static readonly Color HintColor = new Color(0.78f, 0.83f, 0.90f, 1f);

    public static RuntimePauseMenu Instance { get; private set; }
    public static bool IsPauseOpen => Instance != null && Instance.isOpen;

    private RuntimeSettingsPanel settingsPanel;
    private bool isOpen;
    private bool visible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureInstance();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance();
        if (Instance != null && !Instance.visible)
        {
            Instance.HideImmediate();
        }
    }

    public static RuntimePauseMenu EnsureInstance()
    {
        bool supportedScene = GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name);

        if (Instance != null)
        {
            Instance.SetVisible(supportedScene);
            return Instance;
        }

        RuntimePauseMenu existing = FindObjectOfType<RuntimePauseMenu>(true);
        if (existing != null)
        {
            Instance = existing;
            Instance.SetVisible(supportedScene);
            return existing;
        }

        GameObject runtimeObject = new GameObject("RuntimePauseMenu");
        Instance = runtimeObject.AddComponent<RuntimePauseMenu>();
        Instance.SetVisible(supportedScene);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureUi();
        SetVisible(GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name));
        HideImmediate();
    }

    private void Update()
    {
        if (!visible)
        {
            return;
        }

        KeyCode pauseKey = GameSettingsStore.GetKeyBinding(GameInputAction.Pause);
        if (!Input.GetKeyDown(pauseKey))
        {
            return;
        }

        if (RuntimeMiniMapHud.Instance != null && RuntimeMiniMapHud.Instance.IsExpandedViewVisible)
        {
            return;
        }

        if (isOpen && settingsPanel != null && settingsPanel.IsCapturingBinding)
        {
            return;
        }

        if (isOpen)
        {
            if (settingsPanel != null)
            {
                settingsPanel.RequestContinueGame();
            }
            else
            {
                ResumeGame();
            }

            return;
        }

        if (UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen())
        {
            return;
        }

        PauseGame();
    }

    private void OnDestroy()
    {
        if (settingsPanel != null)
        {
            settingsPanel.ContinueRequested -= ResumeGame;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void PauseGame()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        ApplyVisibility(true);
    }

    private void ResumeGame()
    {
        if (!isOpen)
        {
            return;
        }

        HideImmediate();
    }

    private void HideImmediate()
    {
        isOpen = false;
        ApplyVisibility(false);
    }

    private void SetVisible(bool shouldShow)
    {
        visible = shouldShow;

        if (settingsPanel != null)
        {
            settingsPanel.SetVisible(shouldShow);
        }

        if (!shouldShow)
        {
            HideImmediate();
        }
    }

    private void EnsureUi()
    {
        if (settingsPanel != null)
        {
            return;
        }

        settingsPanel = RuntimeSettingsPanel.EnsureInstance();
        settingsPanel.ContinueRequested -= ResumeGame;
        settingsPanel.ContinueRequested += ResumeGame;
        settingsPanel.SetVisible(visible);
        settingsPanel.HideImmediate();
    }

    private void ApplyVisibility(bool show)
    {
        if (settingsPanel == null)
        {
            return;
        }

        if (show)
        {
            settingsPanel.Show();
            return;
        }

        settingsPanel.HideImmediate();
    }

    private static Image CreateImage(string name, Transform parent, Color color, int radius, int border)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(image, color, radius, border);
        return image;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Color backgroundColor,
        Color textColor,
        Vector2 size)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image buttonImage = buttonObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(buttonImage, backgroundColor, 14, 14);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Button button = buttonObject.GetComponent<Button>();

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TmpRuntimeFontFallback.EnsureChineseFallback() ?? TMP_Settings.defaultFontAsset;
        text.text = label;
        text.fontSize = 28f;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = text.GetComponent<RectTransform>();
        StretchRect(textRect);

        return button;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TmpRuntimeFontFallback.EnsureChineseFallback() ?? TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        return text;
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}

public static class TmpRuntimeFontFallback
{
    private static readonly string[] PreferredFontAssetPaths =
    {
        "Assets/File/Fonts/NotoSansSC-Black SDF.asset",
        "Assets/File/Fonts/Fonts_1 SDF.asset"
    };

    private static readonly string[] PreferredSourceFontPaths =
    {
        "Assets/File/Fonts/NotoSansSC-Black.ttf",
        "Assets/File/Fonts/Fonts_1.ttf"
    };

    private static readonly string[] PreferredFontKeywords =
    {
        "NotoSansSC",
        "Noto Sans SC",
        "PingFang",
        "Hiragino Sans GB"
    };

    private const string RequiredCharacters =
        "按住或轻点查看大地图松开预览收起继续游戏设置返回基地关卡暂停分辨率显示模式窗口全屏比例当前地图交互攻击点击继续返回总音量音乐音量控制全部游戏声音背景音乐单独强度分辨率显示模式当前比例屏幕适配自动根据窗口大小匹配视野生命构筑建筑结构图鉴背包专用材料普通结构解锁消耗数量剩余详情说明近战远程耐久防御速度倍率0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-+/():.% x";

    private static readonly string[] RuntimeFontNames =
    {
        "Noto Sans SC",
        "PingFang SC",
        "Hiragino Sans GB",
        "Songti SC",
        "Arial Unicode MS"
    };

    private static TMP_FontAsset runtimeFallbackFont;
    private static bool ensured;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureChineseFallback();
    }

    public static TMP_FontAsset EnsureChineseFallback()
    {
        if (ensured)
        {
            return runtimeFallbackFont;
        }

        ensured = true;

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null)
        {
            return null;
        }

        TryWarmupFontCharacters(defaultFont);
        if (IsUsablePreferredFont(defaultFont))
        {
            runtimeFallbackFont = defaultFont;
            return runtimeFallbackFont;
        }

        runtimeFallbackFont = ResolveProjectFallback();
        if (runtimeFallbackFont == null)
        {
            runtimeFallbackFont = ResolveLoadedFallback();
        }

        if (runtimeFallbackFont == null)
        {
            runtimeFallbackFont = ResolveSystemFallback();
        }

        if (runtimeFallbackFont == null)
        {
            return defaultFont;
        }

        if (defaultFont.fallbackFontAssetTable == null)
        {
            defaultFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
        }

        if (!defaultFont.fallbackFontAssetTable.Contains(runtimeFallbackFont))
        {
            defaultFont.fallbackFontAssetTable.Add(runtimeFallbackFont);
        }

        return runtimeFallbackFont;
    }

    public static TMP_FontAsset WarmupCharacters(string text)
    {
        TMP_FontAsset primaryFont = EnsureChineseFallback();
        if (string.IsNullOrEmpty(text))
        {
            return primaryFont;
        }

        WarmupCharactersInternal(primaryFont, text);

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null && defaultFont != primaryFont)
        {
            WarmupCharactersInternal(defaultFont, text);
        }

        if (defaultFont != null && defaultFont.fallbackFontAssetTable != null)
        {
            for (int i = 0; i < defaultFont.fallbackFontAssetTable.Count; i++)
            {
                WarmupCharactersInternal(defaultFont.fallbackFontAssetTable[i], text);
            }
        }

        return primaryFont;
    }

    private static TMP_FontAsset ResolveLoadedFallback()
    {
        TMP_FontAsset[] loadedFontAssets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < loadedFontAssets.Length; i++)
        {
            TMP_FontAsset fontAsset = loadedFontAssets[i];
            if (!IsPreferredFont(fontAsset))
            {
                continue;
            }

            TryWarmupFontCharacters(fontAsset);
            if (IsUsablePreferredFont(fontAsset))
            {
                return fontAsset;
            }
        }

        return null;
    }

    private static TMP_FontAsset ResolveProjectFallback()
    {
#if UNITY_EDITOR
        for (int i = 0; i < PreferredSourceFontPaths.Length; i++)
        {
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(PreferredSourceFontPaths[i]);
            TMP_FontAsset fontAsset = CreateDynamicFontAsset(sourceFont);
            if (!IsPreferredFont(fontAsset))
            {
                continue;
            }

            TryWarmupFontCharacters(fontAsset);
            if (IsUsablePreferredFont(fontAsset))
            {
                return fontAsset;
            }
        }

        for (int i = 0; i < PreferredFontAssetPaths.Length; i++)
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PreferredFontAssetPaths[i]);
            if (!IsPreferredFont(fontAsset))
            {
                continue;
            }

            TryWarmupFontCharacters(fontAsset);
            if (IsUsablePreferredFont(fontAsset))
            {
                return fontAsset;
            }
        }
#endif

        return null;
    }

    private static TMP_FontAsset ResolveSystemFallback()
    {
        for (int i = 0; i < RuntimeFontNames.Length; i++)
        {
            Font font;
            try
            {
                font = Font.CreateDynamicFontFromOSFont(RuntimeFontNames[i], 90);
            }
            catch (Exception)
            {
                continue;
            }

            TMP_FontAsset fontAsset = CreateDynamicFontAsset(font);
            if (!IsPreferredFont(fontAsset))
            {
                continue;
            }

            TryWarmupFontCharacters(fontAsset);
            if (IsUsablePreferredFont(fontAsset))
            {
                return fontAsset;
            }
        }

        return null;
    }

    private static TMP_FontAsset CreateDynamicFontAsset(Font font)
    {
        if (font == null)
        {
            return null;
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            font,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true);
        if (fontAsset == null)
        {
            return null;
        }

        fontAsset.hideFlags = HideFlags.DontUnloadUnusedAsset;
        fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
        return fontAsset;
    }

    private static bool IsPreferredFont(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
        {
            return false;
        }

        if (ContainsPreferredKeyword(fontAsset.name))
        {
            return true;
        }

        if (ContainsPreferredKeyword(fontAsset.faceInfo.familyName))
        {
            return true;
        }

        return ContainsPreferredKeyword(fontAsset.sourceFontFile != null ? fontAsset.sourceFontFile.name : string.Empty);
    }

    private static bool IsUsablePreferredFont(TMP_FontAsset fontAsset)
    {
        if (!IsPreferredFont(fontAsset))
        {
            return false;
        }

        if (fontAsset.HasCharacters(RequiredCharacters))
        {
            return true;
        }

        return fontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic;
    }

    private static bool ContainsPreferredKeyword(string fontName)
    {
        if (string.IsNullOrEmpty(fontName))
        {
            return false;
        }

        for (int i = 0; i < PreferredFontKeywords.Length; i++)
        {
            if (fontName.IndexOf(PreferredFontKeywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static void TryWarmupFontCharacters(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null || fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic)
        {
            return;
        }

        fontAsset.TryAddCharacters(RequiredCharacters);
    }

    private static void WarmupCharactersInternal(TMP_FontAsset fontAsset, string text)
    {
        if (fontAsset == null || string.IsNullOrEmpty(text))
        {
            return;
        }

        if (fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic)
        {
            return;
        }

        fontAsset.TryAddCharacters(text);
    }
}
