using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Sprites;
using UnityEngine.UI;

public class RuntimeMiniMapHud : MonoBehaviour
{
    private const string BaseSceneName = "BaseScene";
    private const string GameSceneName = "GameScene";
    private const string CameraName = "RuntimeMiniMapCamera";
    private const string OverlayCanvasName = "RuntimeMiniMapOverlayCanvas";
    private const string SmallMapRootName = "RuntimeMiniMapRoot";
    private const int OverlayCanvasSortingOrder = 228;
    private const float SmallMapSize = 172f;
    private const float MapAspect = 4f / 3f;
    private const float LargeMapWidth = 720f;
    private const float LargeMapHeight = 720f;
    private const float LargePanelWidth = 796f;
    private const float LargePanelHeight = 820f;
    private const float Margin = 22f;
    private const float SmallMarkerMaxWidth = 16f;
    private const float SmallMarkerMaxHeight = 22f;
    private const float ExpandedMarkerMaxWidth = 28f;
    private const float ExpandedMarkerMaxHeight = 38f;
    private const float MiniMapMarkerMaxWidth = 18f;
    private const float MiniMapMarkerMaxHeight = 24f;
    private const int TextureWidth = 1024;
    private const int TextureHeight = 768;
    private const float ExpandSmoothTime = 0.14f;
    private const float SmallFramePaddingX = 12f;
    private const float SmallFramePaddingY = 12f;
    private const float LargeFramePaddingX = 20f;
    private const float LargeFramePaddingY = 18f;

    private static readonly Color PanelColor = new Color(0.07f, 0.10f, 0.14f, 0.86f);
    private static readonly Color ExpandedPanelColor = new Color(0.07f, 0.10f, 0.14f, 0.96f);
    private static readonly Color BorderColor = new Color(0.29f, 0.43f, 0.52f, 1f);
    private static readonly Color OverlayColor = new Color(0.03f, 0.05f, 0.08f, 0.84f);
    private static readonly Color TitleColor = new Color(0.94f, 0.97f, 1f, 1f);
    private static readonly Color HintColor = new Color(0.76f, 0.84f, 0.90f, 1f);
    private static readonly Color MarkerOutlineColor = new Color(0.07f, 0.12f, 0.19f, 0.98f);
    private static readonly Color MarkerGlowColor = new Color(0.58f, 0.83f, 1f, 0.38f);

    public static RuntimeMiniMapHud Instance { get; private set; }
    public bool IsExpandedViewVisible => visible && expandProgress > 0.05f;

    private Camera miniMapCamera;
    private Canvas overlayCanvas;
    private RenderTexture renderTexture;
    private Camera cachedMainCamera;
    private Transform cachedPlayer;
    private SpriteRenderer cachedPlayerSpriteRenderer;
    private RectTransform smallMapRoot;
    private RectTransform smallMapViewportRect;
    private RectTransform smallMapImageRect;
    private RawImage smallMapImage;
    private Image smallMapFrameImage;
    private RectTransform smallMarkerRoot;
    private RawImage smallMarkerGlowImage;
    private RawImage[] smallMarkerOutlineImages;
    private RawImage smallMarkerFigureImage;
    private GUIStyle titleStyle;
    private GUIStyle hintStyle;
    private Texture2D markerFigureTexture;
    private bool expanded;
    private bool visible = true;
    private bool pinnedExpanded;
    private float mKeyPressedAt = -1f;
    private float expandProgress;
    private float expandVelocity;

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
    }

    public static RuntimeMiniMapHud EnsureInstance()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        bool supportedScene = IsSupportedScene(activeScene.name);

        if (!supportedScene)
        {
            if (Instance != null)
            {
                Instance.SetVisible(false);
            }

            return Instance;
        }

        if (Instance != null)
        {
            Instance.SetVisible(true);
            Instance.RefreshSceneBindings();
            return Instance;
        }

        RuntimeMiniMapHud existing = FindObjectOfType<RuntimeMiniMapHud>(true);
        if (existing != null)
        {
            Instance = existing;
            Instance.SetVisible(true);
            Instance.RefreshSceneBindings();
            return existing;
        }

        GameObject hudObject = new GameObject("RuntimeMiniMapHud");
        Instance = hudObject.AddComponent<RuntimeMiniMapHud>();
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
        EnsureInfrastructure();
        RefreshSceneBindings();
    }

    private void Start()
    {
        EnsureInfrastructure();
        RefreshSceneBindings();
        expandProgress = expanded ? 1f : 0f;
    }

    private void LateUpdate()
    {
        bool supportedScene = IsSupportedScene(SceneManager.GetActiveScene().name);
        SetVisible(supportedScene);
        if (!supportedScene)
        {
            return;
        }

        EnsureInfrastructure();

        if (cachedMainCamera == null || cachedPlayer == null)
        {
            RefreshSceneBindings();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            mKeyPressedAt = Time.unscaledTime;
        }

        if (Input.GetKeyUp(KeyCode.M))
        {
            float pressedDuration = mKeyPressedAt < 0f ? 0f : Time.unscaledTime - mKeyPressedAt;
            if (pressedDuration <= 0.22f)
            {
                pinnedExpanded = !pinnedExpanded;
            }

            mKeyPressedAt = -1f;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pinnedExpanded = false;
        }

        if (ShouldHideForGameplayOverlay())
        {
            pinnedExpanded = false;
        }

        expanded = !ShouldHideForGameplayOverlay() && (Input.GetKey(KeyCode.M) || pinnedExpanded);
        UpdateExpansionState();
        UpdateCameraPose();
        UpdateSmallMapOverlay();

        if (miniMapCamera != null && renderTexture != null)
        {
            miniMapCamera.targetTexture = renderTexture;
            miniMapCamera.Render();
        }
    }

    private void OnGUI()
    {
        if (!visible ||
            renderTexture == null ||
            !IsSupportedScene(SceneManager.GetActiveScene().name) ||
            ShouldHideForGameplayOverlay())
        {
            return;
        }

        EnsureStyles();

        float easedExpandProgress = GetEasedExpandProgress();
        bool showExpandedState = expanded || easedExpandProgress > 0.5f;
        string title = $"{GetSceneDisplayName()} {(showExpandedState ? "大地图" : "小地图")}";
        string hint = showExpandedState ? "松开 M 预览 / Esc 收起" : "按住或轻点 M 查看大地图";
        float chromeAlpha = Mathf.Clamp01(Mathf.InverseLerp(0.04f, 0.22f, easedExpandProgress));

        Rect smallMapSlotRect = new Rect(
            Screen.width - SmallMapSize - Margin,
            Margin,
            SmallMapSize,
            SmallMapSize);
        Rect smallMapRect = GetAspectFitRect(smallMapSlotRect, MapAspect);
        Rect smallMapFrameRect = ExpandRect(smallMapRect, SmallFramePaddingX, SmallFramePaddingY);
        Rect smallPanelRect = smallMapSlotRect;
        Rect largePanelRect = new Rect(
            (Screen.width - LargePanelWidth) * 0.5f,
            (Screen.height - LargePanelHeight) * 0.5f,
            LargePanelWidth,
            LargePanelHeight);
        Rect panelRect = LerpRect(smallPanelRect, largePanelRect, easedExpandProgress);

        Rect largeMapSlotRect = new Rect(
            largePanelRect.x + (largePanelRect.width - LargeMapWidth) * 0.5f,
            largePanelRect.y + 64f,
            LargeMapWidth,
            LargeMapHeight);
        Rect largeMapRect = GetAspectFitRect(largeMapSlotRect, MapAspect);
        Rect largeMapFrameRect = ExpandRect(largeMapRect, LargeFramePaddingX, LargeFramePaddingY);
        Rect mapRect = LerpRect(smallMapRect, largeMapRect, easedExpandProgress);
        Rect mapFrameRect = LerpRect(smallMapFrameRect, largeMapFrameRect, easedExpandProgress);

        if (easedExpandProgress <= 0.001f)
        {
            DrawMapFrame(smallMapFrameRect, SmallFramePaddingX, SmallFramePaddingY, 1f);
            return;
        }

        if (easedExpandProgress > 0.001f)
        {
            Color overlayColor = OverlayColor;
            overlayColor.a *= easedExpandProgress;
            GUI.color = overlayColor;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (chromeAlpha > 0.001f)
        {
            Color panelFillColor = Color.Lerp(PanelColor, ExpandedPanelColor, easedExpandProgress);
            panelFillColor.a *= chromeAlpha;

            Color borderColor = BorderColor;
            borderColor.a *= chromeAlpha;

            DrawPanel(panelRect, panelFillColor, borderColor);
        }

        if (chromeAlpha > 0.001f)
        {
            float currentFramePaddingX = Mathf.Lerp(SmallFramePaddingX, LargeFramePaddingX, easedExpandProgress);
            float currentFramePaddingY = Mathf.Lerp(SmallFramePaddingY, LargeFramePaddingY, easedExpandProgress);
            DrawMapFrame(mapFrameRect, currentFramePaddingX, currentFramePaddingY, chromeAlpha);
        }

        GUI.DrawTexture(mapRect, renderTexture, ScaleMode.StretchToFill, false);

        if (chromeAlpha > 0.001f)
        {
            Color previousGuiColor = GUI.color;
            Color labelTint = Color.white;
            labelTint.a = chromeAlpha;
            GUI.color = labelTint;
            GUI.Label(new Rect(panelRect.x + 16f, panelRect.y + 12f, panelRect.width * 0.55f, 26f), title, titleStyle);
            GUI.Label(new Rect(panelRect.x + panelRect.width - 236f, panelRect.y + 14f, 220f, 22f), hint, hintStyle);
            GUI.color = previousGuiColor;
        }

        DrawPlayerMarker(mapRect);

        GUI.color = Color.white;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }

        if (markerFigureTexture != null)
        {
            Destroy(markerFigureTexture);
            markerFigureTexture = null;
        }

        if (overlayCanvas != null)
        {
            Destroy(overlayCanvas.gameObject);
            overlayCanvas = null;
        }
    }

    private void EnsureInfrastructure()
    {
        EnsureRenderTexture();
        EnsureMiniMapCamera();
        EnsureSmallMapOverlay();
    }

    private void EnsureMiniMapCamera()
    {
        if (miniMapCamera == null)
        {
            Transform cameraTransform = transform.Find(CameraName);
            GameObject cameraObject = cameraTransform != null ? cameraTransform.gameObject : new GameObject(CameraName);
            cameraObject.transform.SetParent(transform, false);

            miniMapCamera = cameraObject.GetComponent<Camera>();
            if (miniMapCamera == null)
            {
                miniMapCamera = cameraObject.AddComponent<Camera>();
            }
        }

        miniMapCamera.enabled = false;
        miniMapCamera.orthographic = true;
        miniMapCamera.clearFlags = CameraClearFlags.SolidColor;
        miniMapCamera.backgroundColor = new Color(0.05f, 0.08f, 0.10f, 1f);
        miniMapCamera.useOcclusionCulling = false;
        miniMapCamera.allowHDR = false;
        miniMapCamera.allowMSAA = false;
        miniMapCamera.depth = -100f;
        miniMapCamera.targetTexture = renderTexture;
    }

    private void EnsureRenderTexture()
    {
        if (renderTexture != null)
        {
            return;
        }

        renderTexture = new RenderTexture(TextureWidth, TextureHeight, 16, RenderTextureFormat.ARGB32);
        renderTexture.name = "RuntimeMiniMapTexture";
        renderTexture.filterMode = FilterMode.Bilinear;
        renderTexture.useMipMap = false;
        renderTexture.autoGenerateMips = false;
        renderTexture.Create();
    }

    private void RefreshSceneBindings()
    {
        cachedMainCamera = Camera.main;
        cachedPlayer = null;
        cachedPlayerSpriteRenderer = null;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            cachedPlayer = playerObject.transform;
            cachedPlayerSpriteRenderer = ResolvePlayerSpriteRenderer(playerObject.transform);
        }

        if (miniMapCamera == null)
        {
            EnsureMiniMapCamera();
        }

        if (miniMapCamera == null)
        {
            return;
        }

        if (cachedMainCamera != null)
        {
            miniMapCamera.cullingMask = cachedMainCamera.cullingMask;
            miniMapCamera.backgroundColor = cachedMainCamera.backgroundColor;
            miniMapCamera.nearClipPlane = cachedMainCamera.nearClipPlane;
            miniMapCamera.farClipPlane = cachedMainCamera.farClipPlane;
        }
        else
        {
            miniMapCamera.cullingMask = ~0;
            miniMapCamera.nearClipPlane = -50f;
            miniMapCamera.farClipPlane = 50f;
        }

        UpdateCameraPose();
    }

    private void EnsureSmallMapOverlay()
    {
        if (overlayCanvas == null)
        {
            Transform canvasTransform = transform.Find(OverlayCanvasName);
            GameObject canvasObject = canvasTransform != null
                ? canvasTransform.gameObject
                : new GameObject(
                    OverlayCanvasName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster),
                    typeof(CanvasGroup));

            canvasObject.transform.SetParent(transform, false);

            overlayCanvas = canvasObject.GetComponent<Canvas>();
            if (overlayCanvas == null)
            {
                overlayCanvas = canvasObject.AddComponent<Canvas>();
            }

            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = OverlayCanvasSortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = false;
            }

            CanvasGroup canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = canvasObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (smallMapRoot != null)
        {
            if (smallMapImage != null)
            {
                smallMapImage.texture = renderTexture;
            }

            return;
        }

        GameObject rootObject = CreateOverlayUIObject(SmallMapRootName, overlayCanvas.transform);
        smallMapRoot = rootObject.GetComponent<RectTransform>();

        GameObject frameObject = CreateOverlayUIObject("Frame", smallMapRoot);
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        StretchRect(frameRect);
        smallMapFrameImage = frameObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplyMapFrameSprite(smallMapFrameImage, Color.white);
        smallMapFrameImage.enabled = false;
        smallMapFrameImage.raycastTarget = false;

        GameObject viewportObject = CreateOverlayUIObject("Viewport", smallMapRoot);
        smallMapViewportRect = viewportObject.GetComponent<RectTransform>();
        viewportObject.AddComponent<RectMask2D>();

        GameObject mapObject = CreateOverlayUIObject("Map", smallMapViewportRect);
        smallMapImageRect = mapObject.GetComponent<RectTransform>();
        smallMapImage = mapObject.AddComponent<RawImage>();
        smallMapImage.texture = renderTexture;
        smallMapImage.raycastTarget = false;

        GameObject markerObject = CreateOverlayUIObject("PlayerMarker", smallMapViewportRect);
        smallMarkerRoot = markerObject.GetComponent<RectTransform>();
        smallMarkerRoot.anchorMin = new Vector2(0.5f, 0.5f);
        smallMarkerRoot.anchorMax = new Vector2(0.5f, 0.5f);
        smallMarkerRoot.pivot = new Vector2(0.5f, 0.5f);

        GameObject glowObject = CreateOverlayUIObject("Glow", smallMarkerRoot);
        RectTransform glowRect = glowObject.GetComponent<RectTransform>();
        StretchRect(glowRect);
        smallMarkerGlowImage = glowObject.AddComponent<RawImage>();
        smallMarkerGlowImage.color = MarkerGlowColor;
        smallMarkerGlowImage.raycastTarget = false;

        smallMarkerOutlineImages = new RawImage[8];
        for (int i = 0; i < smallMarkerOutlineImages.Length; i++)
        {
            GameObject outlineObject = CreateOverlayUIObject($"Outline{i}", smallMarkerRoot);
            smallMarkerOutlineImages[i] = outlineObject.AddComponent<RawImage>();
            smallMarkerOutlineImages[i].color = MarkerOutlineColor;
            smallMarkerOutlineImages[i].raycastTarget = false;
        }

        GameObject figureObject = CreateOverlayUIObject("Figure", smallMarkerRoot);
        RectTransform figureRect = figureObject.GetComponent<RectTransform>();
        figureRect.anchorMin = new Vector2(0.5f, 0.5f);
        figureRect.anchorMax = new Vector2(0.5f, 0.5f);
        figureRect.pivot = new Vector2(0.5f, 0.5f);
        smallMarkerFigureImage = figureObject.AddComponent<RawImage>();
        smallMarkerFigureImage.color = Color.white;
        smallMarkerFigureImage.raycastTarget = false;
    }

    private void UpdateSmallMapOverlay()
    {
        if (smallMapRoot == null)
        {
            return;
        }

        bool shouldShow = visible && !ShouldHideForGameplayOverlay() && expandProgress <= 0.001f;
        if (smallMapRoot.gameObject.activeSelf != shouldShow)
        {
            smallMapRoot.gameObject.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            return;
        }

        smallMapRoot.anchorMin = new Vector2(1f, 1f);
        smallMapRoot.anchorMax = new Vector2(1f, 1f);
        smallMapRoot.pivot = new Vector2(1f, 1f);
        smallMapRoot.anchoredPosition = new Vector2(-Margin, -Margin);

        Vector2 mapSize = BuildAspectFitSize(SmallMapSize, SmallMapSize, MapAspect);
        smallMapRoot.sizeDelta = new Vector2(
            mapSize.x + SmallFramePaddingX * 2f,
            mapSize.y + SmallFramePaddingY * 2f);
        smallMapRoot.SetAsLastSibling();

        if (smallMapViewportRect != null)
        {
            ConfigureMarkerLayerRect(
                smallMapViewportRect,
                mapSize,
                Vector2.zero);
        }

        if (smallMapImageRect != null)
        {
            ConfigureMarkerLayerRect(
                smallMapImageRect,
                mapSize,
                Vector2.zero);
        }

        if (smallMapImage != null && smallMapImage.texture != renderTexture)
        {
            smallMapImage.texture = renderTexture;
        }

        if (smallMarkerRoot != null)
        {
            smallMarkerRoot.sizeDelta = Vector2.zero;
            smallMarkerRoot.anchoredPosition = Vector2.zero;
            UpdateSmallMarkerVisual();
        }
    }

    private void UpdateCameraPose()
    {
        if (miniMapCamera == null)
        {
            return;
        }

        Camera referenceCamera = cachedMainCamera != null ? cachedMainCamera : Camera.main;
        if (referenceCamera == null)
        {
            return;
        }

        Vector3 position = referenceCamera.transform.position;
        if (cachedPlayer != null)
        {
            position.x = cachedPlayer.position.x;
            position.y = cachedPlayer.position.y;
        }

        position.z = referenceCamera.transform.position.z;
        miniMapCamera.transform.position = position;
        miniMapCamera.transform.rotation = referenceCamera.transform.rotation;
        miniMapCamera.orthographic = true;
        miniMapCamera.aspect = MapAspect;

        float baseSize = referenceCamera.orthographic
            ? Mathf.Max(referenceCamera.orthographicSize, 4f)
            : 8f;
        miniMapCamera.orthographicSize = Mathf.Lerp(baseSize * 1.65f, baseSize * 2.6f, GetEasedExpandProgress());
    }

    private void DrawPlayerMarker(Rect mapRect)
    {
        GetMarkerVisual(out Texture markerTexture, out Rect uvRect, out float aspect, out bool flipX, out bool flipY);

        Rect markerRect = BuildAspectFitRect(
            mapRect.center,
            expanded ? ExpandedMarkerMaxWidth : SmallMarkerMaxWidth,
            expanded ? ExpandedMarkerMaxHeight : SmallMarkerMaxHeight,
            aspect);

        DrawPlayerSpriteMarker(markerRect, markerTexture, uvRect, flipX, flipY);
    }

    private void DrawPlayerSpriteMarker(Rect rect, Texture markerTexture, Rect uvRect, bool flipX, bool flipY)
    {
        float outlineOffset = expanded ? 1.8f : 1.1f;
        float glowPadding = expanded ? 7f : 4f;
        Vector2[] offsets =
        {
            new Vector2(-outlineOffset, 0f),
            new Vector2(outlineOffset, 0f),
            new Vector2(0f, -outlineOffset),
            new Vector2(0f, outlineOffset),
            new Vector2(-outlineOffset, -outlineOffset),
            new Vector2(-outlineOffset, outlineOffset),
            new Vector2(outlineOffset, -outlineOffset),
            new Vector2(outlineOffset, outlineOffset)
        };

        Rect glowRect = ExpandRect(rect, glowPadding);
        DrawMarkerTexture(glowRect, markerTexture, uvRect, MarkerGlowColor, flipX, flipY);

        for (int i = 0; i < offsets.Length; i++)
        {
            Rect outlineRect = new Rect(rect.x + offsets[i].x, rect.y + offsets[i].y, rect.width, rect.height);
            DrawMarkerTexture(outlineRect, markerTexture, uvRect, MarkerOutlineColor, flipX, flipY);
        }

        DrawMarkerTexture(rect, markerTexture, uvRect, Color.white, flipX, flipY);
    }

    private Texture2D GetMarkerFigureTexture()
    {
        if (markerFigureTexture != null)
        {
            return markerFigureTexture;
        }

        const int size = 32;
        markerFigureTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        markerFigureTexture.filterMode = FilterMode.Bilinear;

        Vector2 headCenter = new Vector2(15.5f, 7.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x, y);
                bool isHead = Vector2.Distance(point, headCenter) <= 4.8f;
                bool isTorso = x >= 11 && x <= 20 && y >= 12 && y <= 22;
                bool isShoulders = x >= 8 && x <= 23 && y >= 14 && y <= 17;
                bool isLeftLeg = x >= 11 && x <= 14 && y >= 22 && y <= 29;
                bool isRightLeg = x >= 17 && x <= 20 && y >= 22 && y <= 29;
                bool isFeet = x >= 9 && x <= 22 && y >= 28 && y <= 30;

                bool filled = isHead || isTorso || isShoulders || isLeftLeg || isRightLeg || isFeet;
                markerFigureTexture.SetPixel(x, y, filled ? Color.white : Color.clear);
            }
        }

        markerFigureTexture.Apply();
        return markerFigureTexture;
    }

    private void EnsureStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = TitleColor;
        }

        if (hintStyle == null)
        {
            hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.fontSize = 14;
            hintStyle.alignment = TextAnchor.MiddleRight;
            hintStyle.normal.textColor = HintColor;
        }
    }

    private void UpdateSmallMarkerVisual()
    {
        if (smallMarkerRoot == null || smallMarkerFigureImage == null)
        {
            return;
        }

        GetMarkerVisual(out Texture markerTexture, out Rect uvRect, out float aspect, out bool flipX, out bool flipY);
        Vector2 markerSize = BuildAspectFitSize(MiniMapMarkerMaxWidth, MiniMapMarkerMaxHeight, aspect);

        smallMarkerRoot.localScale = new Vector3(flipX ? -1f : 1f, flipY ? -1f : 1f, 1f);

        ConfigureMarkerLayerRect(smallMarkerGlowImage.rectTransform, markerSize + new Vector2(8f, 8f), Vector2.zero);
        ApplyMarkerToRawImage(smallMarkerGlowImage, markerTexture, uvRect, MarkerGlowColor);

        Vector2[] offsets =
        {
            new Vector2(-1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, -1f),
            new Vector2(0f, 1f),
            new Vector2(-0.85f, -0.85f),
            new Vector2(-0.85f, 0.85f),
            new Vector2(0.85f, -0.85f),
            new Vector2(0.85f, 0.85f)
        };

        for (int i = 0; i < smallMarkerOutlineImages.Length; i++)
        {
            ConfigureMarkerLayerRect(smallMarkerOutlineImages[i].rectTransform, markerSize, offsets[i]);
            ApplyMarkerToRawImage(smallMarkerOutlineImages[i], markerTexture, uvRect, MarkerOutlineColor);
        }

        ConfigureMarkerLayerRect(smallMarkerFigureImage.rectTransform, markerSize, Vector2.zero);
        ApplyMarkerToRawImage(smallMarkerFigureImage, markerTexture, uvRect, Color.white);
    }

    private void GetMarkerVisual(out Texture markerTexture, out Rect uvRect, out float aspect, out bool flipX, out bool flipY)
    {
        SpriteRenderer spriteRenderer = ResolvePlayerSpriteRenderer();
        if (spriteRenderer != null && spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null)
        {
            Sprite sprite = spriteRenderer.sprite;
            Vector4 outerUv = DataUtility.GetOuterUV(sprite);

            markerTexture = sprite.texture;
            uvRect = new Rect(outerUv.x, outerUv.y, outerUv.z - outerUv.x, outerUv.w - outerUv.y);
            aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            flipX = spriteRenderer.flipX;
            flipY = spriteRenderer.flipY;
            return;
        }

        markerTexture = GetMarkerFigureTexture();
        uvRect = new Rect(0f, 0f, 1f, 1f);
        aspect = 20f / 26f;
        flipX = false;
        flipY = false;
    }

    private SpriteRenderer ResolvePlayerSpriteRenderer()
    {
        if (cachedPlayerSpriteRenderer != null)
        {
            return cachedPlayerSpriteRenderer;
        }

        if (cachedPlayer != null)
        {
            cachedPlayerSpriteRenderer = ResolvePlayerSpriteRenderer(cachedPlayer);
            if (cachedPlayerSpriteRenderer != null)
            {
                return cachedPlayerSpriteRenderer;
            }
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return null;
        }

        cachedPlayer = playerObject.transform;
        cachedPlayerSpriteRenderer = ResolvePlayerSpriteRenderer(playerObject.transform);
        return cachedPlayerSpriteRenderer;
    }

    private static SpriteRenderer ResolvePlayerSpriteRenderer(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            return null;
        }

        SpriteRenderer directRenderer = playerTransform.GetComponent<SpriteRenderer>();
        if (directRenderer != null)
        {
            return directRenderer;
        }

        SpriteRenderer[] childRenderers = playerTransform.GetComponentsInChildren<SpriteRenderer>(true);
        return childRenderers.Length > 0 ? childRenderers[0] : null;
    }

    private void SetVisible(bool shouldShow)
    {
        visible = shouldShow;

        if (miniMapCamera != null)
        {
            miniMapCamera.gameObject.SetActive(shouldShow);
        }

        if (overlayCanvas != null)
        {
            overlayCanvas.gameObject.SetActive(shouldShow);
        }
    }

    private void UpdateExpansionState()
    {
        float target = expanded ? 1f : 0f;
        expandProgress = Mathf.SmoothDamp(
            expandProgress,
            target,
            ref expandVelocity,
            ExpandSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);

        if (Mathf.Abs(expandProgress - target) <= 0.001f)
        {
            expandProgress = target;
            expandVelocity = 0f;
        }
    }

    private float GetEasedExpandProgress()
    {
        float progress = Mathf.Clamp01(expandProgress);
        return progress * progress * (3f - 2f * progress);
    }

    private static void DrawPanel(Rect panelRect, Color fillColor, Color borderColor)
    {
        DrawFilledRect(panelRect, fillColor);
        DrawBorder(panelRect, 2f, borderColor);
    }

    private static void DrawMapFrame(Rect rect, float borderWidth, float borderHeight, float alpha)
    {
        Texture2D frameTexture = RuntimeUiSpriteFactory.GetMapFrameTexture();
        if (frameTexture == null)
        {
            Color fallbackFill = Color.Lerp(PanelColor, ExpandedPanelColor, alpha);
            fallbackFill.a = alpha;
            Color fallbackBorder = BorderColor;
            fallbackBorder.a = alpha;
            DrawPanel(rect, fallbackFill, fallbackBorder);
            return;
        }

        Rect sourceRect = RuntimeUiSpriteFactory.GetMapFramePixelRect();
        Vector4 sourceBorder = RuntimeUiSpriteFactory.GetMapFrameBorder();
        Color tint = new Color(1f, 1f, 1f, alpha);

        float left = Mathf.Min(borderWidth, rect.width * 0.5f);
        float right = Mathf.Min(borderWidth, rect.width - left);
        float bottom = Mathf.Min(borderHeight, rect.height * 0.5f);
        float top = Mathf.Min(borderHeight, rect.height - bottom);

        float srcLeft = Mathf.Min(sourceBorder.x, sourceRect.width * 0.5f);
        float srcRight = Mathf.Min(sourceBorder.z, sourceRect.width - srcLeft);
        float srcBottom = Mathf.Min(sourceBorder.y, sourceRect.height * 0.5f);
        float srcTop = Mathf.Min(sourceBorder.w, sourceRect.height - srcBottom);

        float centerWidth = Mathf.Max(0f, rect.width - left - right);
        float centerHeight = Mathf.Max(0f, rect.height - top - bottom);
        float srcCenterWidth = Mathf.Max(0f, sourceRect.width - srcLeft - srcRight);
        float srcCenterHeight = Mathf.Max(0f, sourceRect.height - srcTop - srcBottom);

        DrawMapFrameSlice(frameTexture, tint,
            new Rect(rect.x, rect.y, left, bottom),
            new Rect(sourceRect.x, sourceRect.y, srcLeft, srcBottom));
        DrawMapFrameSlice(frameTexture, tint,
            new Rect(rect.x + left, rect.y, centerWidth, bottom),
            new Rect(sourceRect.x + srcLeft, sourceRect.y, srcCenterWidth, srcBottom));
        DrawMapFrameSlice(frameTexture, tint,
            new Rect(rect.xMax - right, rect.y, right, bottom),
            new Rect(sourceRect.xMax - srcRight, sourceRect.y, srcRight, srcBottom));

        DrawMapFrameSlice(frameTexture, tint,
            new Rect(rect.x, rect.y + bottom, left, centerHeight),
            new Rect(sourceRect.x, sourceRect.y + srcBottom, srcLeft, srcCenterHeight));
        DrawMapFrameSlice(frameTexture, tint,
            new Rect(rect.x + left, rect.y + bottom, centerWidth, centerHeight),
            new Rect(sourceRect.x + srcLeft, sourceRect.y + srcBottom, srcCenterWidth, srcCenterHeight));
        DrawMapFrameSlice(frameTexture, tint,
            new Rect(rect.xMax - right, rect.y + bottom, right, centerHeight),
            new Rect(sourceRect.xMax - srcRight, sourceRect.y + srcBottom, srcRight, srcCenterHeight));

        DrawMapFrameSlice(frameTexture, tint,
            new Rect(rect.x, rect.yMax - top, left, top),
            new Rect(sourceRect.x, sourceRect.yMax - srcTop, srcLeft, srcTop));
        DrawMapFrameSlice(frameTexture, tint,
            new Rect(rect.x + left, rect.yMax - top, centerWidth, top),
            new Rect(sourceRect.x + srcLeft, sourceRect.yMax - srcTop, srcCenterWidth, srcTop));
        DrawMapFrameSlice(frameTexture, tint,
            new Rect(rect.xMax - right, rect.yMax - top, right, top),
            new Rect(sourceRect.xMax - srcRight, sourceRect.yMax - srcTop, srcRight, srcTop));
    }

    private static void DrawMapFrameSlice(Texture texture, Color tint, Rect destinationRect, Rect sourceRect)
    {
        if (destinationRect.width <= 0.001f || destinationRect.height <= 0.001f ||
            sourceRect.width <= 0.001f || sourceRect.height <= 0.001f)
        {
            return;
        }

        DrawTintedTexture(destinationRect, texture, tint, BuildUvRect(texture, sourceRect));
    }

    private static Rect LerpRect(Rect from, Rect to, float t)
    {
        return new Rect(
            Mathf.Lerp(from.x, to.x, t),
            Mathf.Lerp(from.y, to.y, t),
            Mathf.Lerp(from.width, to.width, t),
            Mathf.Lerp(from.height, to.height, t));
    }

    private static void DrawFilledRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    private static void DrawTintedTexture(Rect rect, Texture texture, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
        GUI.color = previousColor;
    }

    private static void DrawTintedTexture(Rect rect, Texture texture, Color color, Rect uv)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
        GUI.color = previousColor;
    }

    private static void DrawMarkerTexture(Rect rect, Texture texture, Rect uvRect, Color color, bool flipX, bool flipY)
    {
        Rect actualUvRect = uvRect;
        if (flipX)
        {
            actualUvRect.x += actualUvRect.width;
            actualUvRect.width *= -1f;
        }

        if (flipY)
        {
            actualUvRect.y += actualUvRect.height;
            actualUvRect.height *= -1f;
        }

        DrawTintedTexture(rect, texture, color, actualUvRect);
    }

    private static void DrawBorder(Rect rect, float thickness, Color color)
    {
        DrawFilledRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        DrawFilledRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        DrawFilledRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        DrawFilledRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private static Rect ExpandRect(Rect rect, float padding)
    {
        return new Rect(
            rect.x - padding,
            rect.y - padding,
            rect.width + padding * 2f,
            rect.height + padding * 2f);
    }

    private static Rect ExpandRect(Rect rect, float paddingX, float paddingY)
    {
        return new Rect(
            rect.x - paddingX,
            rect.y - paddingY,
            rect.width + paddingX * 2f,
            rect.height + paddingY * 2f);
    }

    private static Rect BuildUvRect(Texture texture, Rect sourceRect)
    {
        float width = Mathf.Max(1f, texture.width);
        float height = Mathf.Max(1f, texture.height);
        return new Rect(
            sourceRect.x / width,
            sourceRect.y / height,
            sourceRect.width / width,
            sourceRect.height / height);
    }

    private static Rect GetAspectFitRect(Rect containerRect, float aspect)
    {
        Vector2 fittedSize = BuildAspectFitSize(containerRect.width, containerRect.height, aspect);
        return new Rect(
            containerRect.center.x - fittedSize.x * 0.5f,
            containerRect.center.y - fittedSize.y * 0.5f,
            fittedSize.x,
            fittedSize.y);
    }

    private static Rect BuildAspectFitRect(Vector2 center, float maxWidth, float maxHeight, float aspect)
    {
        Vector2 size = BuildAspectFitSize(maxWidth, maxHeight, aspect);
        return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
    }

    private static Vector2 BuildAspectFitSize(float maxWidth, float maxHeight, float aspect)
    {
        float safeAspect = Mathf.Max(0.2f, aspect);
        float width = maxHeight * safeAspect;
        float height = maxHeight;

        if (width > maxWidth)
        {
            float scale = maxWidth / width;
            width *= scale;
            height *= scale;
        }

        return new Vector2(width, height);
    }

    private static void ConfigureMarkerLayerRect(RectTransform rectTransform, Vector2 size, Vector2 anchoredPosition)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;
    }

    private static void ApplyMarkerToRawImage(RawImage image, Texture texture, Rect uvRect, Color color)
    {
        if (image == null)
        {
            return;
        }

        image.texture = texture;
        image.uvRect = uvRect;
        image.color = color;
    }

    private bool ShouldHideForGameplayOverlay()
    {
        if (UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen())
        {
            return true;
        }

        return UIManager.Instance != null && UIManager.Instance.IsHandbookOpen;
    }

    private static GameObject CreateOverlayUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static bool IsSupportedScene(string sceneName)
    {
        return sceneName == BaseSceneName || sceneName == GameSceneName;
    }

    private static string GetSceneDisplayName()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == BaseSceneName)
        {
            return "基地";
        }

        if (sceneName == GameSceneName)
        {
            return "关卡";
        }

        return sceneName;
    }
}
