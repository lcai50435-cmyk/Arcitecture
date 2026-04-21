using UnityEngine;
using UnityEngine.SceneManagement;

public class RuntimeMiniMapHud : MonoBehaviour
{
    private const string BaseSceneName = "BaseScene";
    private const string GameSceneName = "GameScene";
    private const string CameraName = "RuntimeMiniMapCamera";
    private const float SmallMapSize = 220f;
    private const float SmallPanelWidth = 256f;
    private const float SmallPanelHeight = 286f;
    private const float LargeMapWidth = 720f;
    private const float LargeMapHeight = 720f;
    private const float LargePanelWidth = 796f;
    private const float LargePanelHeight = 820f;
    private const float Margin = 22f;
    private const int TextureSize = 1024;

    private static readonly Color PanelColor = new Color(0.07f, 0.10f, 0.14f, 0.86f);
    private static readonly Color ExpandedPanelColor = new Color(0.07f, 0.10f, 0.14f, 0.96f);
    private static readonly Color BorderColor = new Color(0.29f, 0.43f, 0.52f, 1f);
    private static readonly Color OverlayColor = new Color(0.03f, 0.05f, 0.08f, 0.84f);
    private static readonly Color MapBackdropColor = new Color(0.02f, 0.03f, 0.05f, 1f);
    private static readonly Color TitleColor = new Color(0.94f, 0.97f, 1f, 1f);
    private static readonly Color HintColor = new Color(0.76f, 0.84f, 0.90f, 1f);
    private static readonly Color MarkerOutlineColor = new Color(0.07f, 0.12f, 0.19f, 0.98f);
    private static readonly Color MarkerGlowColor = new Color(0.58f, 0.83f, 1f, 0.62f);
    private static readonly Color MarkerFillColor = new Color(0.98f, 0.99f, 1f, 1f);
    private static readonly Color MarkerAccentColor = new Color(0.52f, 0.74f, 1f, 0.95f);

    public static RuntimeMiniMapHud Instance { get; private set; }

    private Camera miniMapCamera;
    private RenderTexture renderTexture;
    private Camera cachedMainCamera;
    private Transform cachedPlayer;
    private GUIStyle titleStyle;
    private GUIStyle hintStyle;
    private Texture2D markerGlowTexture;
    private Texture2D markerFigureTexture;
    private bool expanded;
    private bool visible = true;
    private bool pinnedExpanded;
    private float mKeyPressedAt = -1f;

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

        expanded = Input.GetKey(KeyCode.M) || pinnedExpanded;
        UpdateCameraPose();

        if (miniMapCamera != null && renderTexture != null)
        {
            miniMapCamera.targetTexture = renderTexture;
            miniMapCamera.Render();
        }
    }

    private void OnGUI()
    {
        if (!visible || renderTexture == null || !IsSupportedScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        EnsureStyles();

        Rect mapRect;
        Rect panelRect;
        string title = $"{GetSceneDisplayName()} {(expanded ? "大地图" : "小地图")}";
        string hint = expanded ? "松开 M 预览 / Esc 收起" : "按住或轻点 M 查看大地图";

        if (expanded)
        {
            GUI.color = OverlayColor;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            panelRect = new Rect(
                (Screen.width - LargePanelWidth) * 0.5f,
                (Screen.height - LargePanelHeight) * 0.5f,
                LargePanelWidth,
                LargePanelHeight);
            mapRect = new Rect(
                panelRect.x + (panelRect.width - LargeMapWidth) * 0.5f,
                panelRect.y + 64f,
                LargeMapWidth,
                LargeMapHeight);
        }
        else
        {
            panelRect = new Rect(
                Screen.width - SmallPanelWidth - Margin,
                Margin,
                SmallPanelWidth,
                SmallPanelHeight);
            mapRect = new Rect(
                panelRect.x + (panelRect.width - SmallMapSize) * 0.5f,
                panelRect.y + 48f,
                SmallMapSize,
                SmallMapSize);
        }

        DrawPanel(panelRect, expanded ? ExpandedPanelColor : PanelColor);
        GUI.Label(new Rect(panelRect.x + 16f, panelRect.y + 12f, panelRect.width * 0.55f, 26f), title, titleStyle);
        GUI.Label(new Rect(panelRect.x + panelRect.width - 236f, panelRect.y + 14f, 220f, 22f), hint, hintStyle);
        DrawFilledRect(mapRect, MapBackdropColor);
        GUI.DrawTexture(mapRect, renderTexture, ScaleMode.StretchToFill, false);
        DrawBorder(mapRect, 2f);
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

        if (markerGlowTexture != null)
        {
            Destroy(markerGlowTexture);
            markerGlowTexture = null;
        }

        if (markerFigureTexture != null)
        {
            Destroy(markerFigureTexture);
            markerFigureTexture = null;
        }
    }

    private void EnsureInfrastructure()
    {
        EnsureRenderTexture();
        EnsureMiniMapCamera();
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

        renderTexture = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32);
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

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            cachedPlayer = playerObject.transform;
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

        float baseSize = referenceCamera.orthographic
            ? Mathf.Max(referenceCamera.orthographicSize, 4f)
            : 8f;
        miniMapCamera.orthographicSize = expanded ? baseSize * 2.6f : baseSize * 1.65f;
    }

    private void DrawPlayerMarker(Rect mapRect)
    {
        float markerHeight = expanded ? 40f : 26f;
        float markerWidth = expanded ? 30f : 20f;
        Rect markerRect = new Rect(
            mapRect.center.x - markerWidth * 0.5f,
            mapRect.center.y - markerHeight * 0.5f,
            markerWidth,
            markerHeight);

        float glowPadding = expanded ? 16f : 10f;
        Rect glowRect = ExpandRect(markerRect, glowPadding);
        DrawTintedTexture(glowRect, GetMarkerGlowTexture(), MarkerGlowColor);

        DrawMarkerWithOutline(markerRect);
    }

    private void DrawMarkerWithOutline(Rect rect)
    {
        float outlineOffset = expanded ? 2.2f : 1.4f;
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

        for (int i = 0; i < offsets.Length; i++)
        {
            Rect outlineRect = new Rect(rect.x + offsets[i].x, rect.y + offsets[i].y, rect.width, rect.height);
            DrawTintedTexture(outlineRect, GetMarkerFigureTexture(), MarkerOutlineColor);
        }

        DrawTintedTexture(rect, GetMarkerFigureTexture(), MarkerFillColor);

        float accentWidth = rect.width * 0.34f;
        float accentHeight = rect.height * 0.18f;
        Rect accentRect = new Rect(
            rect.center.x - accentWidth * 0.5f,
            rect.y + rect.height * 0.48f,
            accentWidth,
            accentHeight);
        DrawTintedTexture(accentRect, Texture2D.whiteTexture, MarkerAccentColor);
    }

    private Texture2D GetMarkerGlowTexture()
    {
        if (markerGlowTexture != null)
        {
            return markerGlowTexture;
        }

        markerGlowTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        markerGlowTexture.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(15.5f, 15.5f);

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / 15.5f;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha *= alpha;
                markerGlowTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        markerGlowTexture.Apply();
        return markerGlowTexture;
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

    private void SetVisible(bool shouldShow)
    {
        visible = shouldShow;

        if (miniMapCamera != null)
        {
            miniMapCamera.gameObject.SetActive(shouldShow);
        }
    }

    private static void DrawPanel(Rect panelRect, Color fillColor)
    {
        DrawFilledRect(panelRect, fillColor);
        DrawBorder(panelRect, 2f);
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

    private static void DrawBorder(Rect rect, float thickness)
    {
        DrawFilledRect(new Rect(rect.x, rect.y, rect.width, thickness), BorderColor);
        DrawFilledRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), BorderColor);
        DrawFilledRect(new Rect(rect.x, rect.y, thickness, rect.height), BorderColor);
        DrawFilledRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), BorderColor);
    }

    private static Rect ExpandRect(Rect rect, float padding)
    {
        return new Rect(
            rect.x - padding,
            rect.y - padding,
            rect.width + padding * 2f,
            rect.height + padding * 2f);
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
