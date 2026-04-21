using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RuntimeMiniMapHud : MonoBehaviour
{
    private const string BaseSceneName = "BaseScene";
    private const string GameSceneName = "GameScene";
    private const float SmallMapSize = 220f;
    private const float LargeMapSize = 620f;
    private const float MarkerRefreshInterval = 0.35f;
    private const float BoundsRefreshInterval = 1f;

    private static Sprite runtimeSprite;
    private static Font runtimeFont;

    private readonly List<MiniMapMarkerData> trackedMarkers = new List<MiniMapMarkerData>();
    private readonly List<MiniMapMarkerView> markerViews = new List<MiniMapMarkerView>();

    private Canvas canvas;
    private RectTransform rootRect;
    private RectTransform mapRect;
    private Image overlayImage;
    private Text titleText;
    private Text hintText;
    private bool expanded;
    private float markerRefreshTimer;
    private float boundsRefreshTimer;
    private Bounds worldBounds;
    private bool hasWorldBounds;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryCreate(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene);
    }

    private static void TryCreate(Scene scene)
    {
        if (scene.name != BaseSceneName && scene.name != GameSceneName)
        {
            return;
        }

        if (FindObjectOfType<RuntimeMiniMapHud>() != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("RuntimeMiniMapHud");
        hudObject.AddComponent<RuntimeMiniMapHud>();
    }

    private void Awake()
    {
        BuildUi();
        RebuildWorldBounds();
        RefreshTrackedTargets();
        ApplyLayout(false);
    }

    private void Update()
    {
        bool shouldExpand = Input.GetKey(KeyCode.M);
        if (shouldExpand != expanded)
        {
            ApplyLayout(shouldExpand);
        }

        markerRefreshTimer -= Time.unscaledDeltaTime;
        if (markerRefreshTimer <= 0f)
        {
            markerRefreshTimer = MarkerRefreshInterval;
            RefreshTrackedTargets();
        }

        boundsRefreshTimer -= Time.unscaledDeltaTime;
        if (boundsRefreshTimer <= 0f)
        {
            boundsRefreshTimer = BoundsRefreshInterval;
            RebuildWorldBounds();
        }

        UpdateMarkerPositions();
    }

    private void BuildUi()
    {
        GameObject canvasObject = new GameObject(
            "MiniMapCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 260;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject overlayObject = CreateUiObject("Overlay", canvasObject.transform);
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        StretchRect(overlayRect);
        overlayImage = overlayObject.AddComponent<Image>();
        overlayImage.color = new Color(0.03f, 0.04f, 0.05f, 0f);
        overlayImage.raycastTarget = false;

        GameObject rootObject = CreateUiObject("Panel", canvasObject.transform);
        rootRect = rootObject.GetComponent<RectTransform>();
        Image rootImage = rootObject.AddComponent<Image>();
        rootImage.color = new Color(0.07f, 0.09f, 0.11f, 0.82f);

        CreateOutline(rootObject, new Color(0.19f, 0.28f, 0.33f, 0.95f));

        titleText = CreateText(rootObject.transform, "Title", 20, TextAnchor.MiddleLeft);
        SetRect(titleText.rectTransform, new Vector2(14f, -12f), new Vector2(240f, 24f), new Vector2(0f, 1f));

        hintText = CreateText(rootObject.transform, "Hint", 16, TextAnchor.MiddleRight);
        SetRect(hintText.rectTransform, new Vector2(-14f, -12f), new Vector2(260f, 22f), new Vector2(1f, 1f));
        hintText.color = new Color(0.75f, 0.82f, 0.86f, 0.95f);

        GameObject mapObject = CreateUiObject("MapArea", rootObject.transform);
        mapRect = mapObject.GetComponent<RectTransform>();
        Image mapImage = mapObject.AddComponent<Image>();
        mapImage.color = new Color(0.10f, 0.13f, 0.16f, 0.92f);
        CreateOutline(mapObject, new Color(0.24f, 0.38f, 0.42f, 1f));

        GameObject horizontalAxis = CreateUiObject("HorizontalAxis", mapObject.transform);
        RectTransform horizontalRect = horizontalAxis.GetComponent<RectTransform>();
        horizontalRect.anchorMin = new Vector2(0f, 0.5f);
        horizontalRect.anchorMax = new Vector2(1f, 0.5f);
        horizontalRect.sizeDelta = new Vector2(0f, 2f);
        horizontalAxis.AddComponent<Image>().color = new Color(0.20f, 0.28f, 0.32f, 0.75f);

        GameObject verticalAxis = CreateUiObject("VerticalAxis", mapObject.transform);
        RectTransform verticalRect = verticalAxis.GetComponent<RectTransform>();
        verticalRect.anchorMin = new Vector2(0.5f, 0f);
        verticalRect.anchorMax = new Vector2(0.5f, 1f);
        verticalRect.sizeDelta = new Vector2(2f, 0f);
        verticalAxis.AddComponent<Image>().color = new Color(0.20f, 0.28f, 0.32f, 0.75f);
    }

    private void ApplyLayout(bool shouldExpand)
    {
        expanded = shouldExpand;

        if (rootRect == null || mapRect == null)
        {
            return;
        }

        if (expanded)
        {
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(720f, 760f);

            mapRect.anchorMin = new Vector2(0.5f, 0f);
            mapRect.anchorMax = new Vector2(0.5f, 0f);
            mapRect.pivot = new Vector2(0.5f, 0f);
            mapRect.anchoredPosition = new Vector2(0f, 24f);
            mapRect.sizeDelta = new Vector2(LargeMapSize, LargeMapSize);

            if (overlayImage != null)
            {
                overlayImage.color = new Color(0.03f, 0.04f, 0.05f, 0.62f);
            }
        }
        else
        {
            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.anchoredPosition = new Vector2(-22f, -22f);
            rootRect.sizeDelta = new Vector2(272f, 308f);

            mapRect.anchorMin = new Vector2(0.5f, 0f);
            mapRect.anchorMax = new Vector2(0.5f, 0f);
            mapRect.pivot = new Vector2(0.5f, 0f);
            mapRect.anchoredPosition = new Vector2(0f, 18f);
            mapRect.sizeDelta = new Vector2(SmallMapSize, SmallMapSize);

            if (overlayImage != null)
            {
                overlayImage.color = new Color(0.03f, 0.04f, 0.05f, 0f);
            }
        }

        if (titleText != null)
        {
            titleText.text = expanded ? $"{GetSceneDisplayName()} 大地图" : $"{GetSceneDisplayName()} 小地图";
        }

        if (hintText != null)
        {
            hintText.text = expanded ? "松开 M 返回小地图" : "按住 M 查看大地图";
        }
    }

    private void RefreshTrackedTargets()
    {
        trackedMarkers.Clear();

        AddPlayerMarker();
        AddEnemyMarkers();
        AddCrystalMarkers();
        AddSceneMarkers();

        EnsureMarkerPool(trackedMarkers.Count);
        for (int i = 0; i < markerViews.Count; i++)
        {
            bool active = i < trackedMarkers.Count;
            markerViews[i].SetActive(active);
            if (active)
            {
                markerViews[i].Bind(trackedMarkers[i]);
            }
        }
    }

    private void UpdateMarkerPositions()
    {
        if (mapRect == null || !hasWorldBounds)
        {
            return;
        }

        Rect mapArea = mapRect.rect;
        for (int i = 0; i < markerViews.Count; i++)
        {
            MiniMapMarkerView view = markerViews[i];
            if (!view.IsActive)
            {
                continue;
            }

            Transform target = view.Target;
            if (target == null)
            {
                view.SetActive(false);
                continue;
            }

            bool visible = expanded || !view.ExpandedOnly;
            view.SetVisible(visible);
            if (!visible)
            {
                continue;
            }

            Vector3 position = target.position;
            float x = Mathf.InverseLerp(worldBounds.min.x, worldBounds.max.x, position.x);
            float y = Mathf.InverseLerp(worldBounds.min.y, worldBounds.max.y, position.y);
            view.SetPosition(new Vector2(
                (x - 0.5f) * mapArea.width,
                (y - 0.5f) * mapArea.height));
        }
    }

    private void RebuildWorldBounds()
    {
        hasWorldBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, new Vector3(10f, 10f, 1f));

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            bounds = new Bounds(player.transform.position, new Vector3(8f, 8f, 1f));
            hasWorldBounds = true;
        }

        Renderer[] renderers = FindObjectsOfType<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (renderer.GetComponentInParent<Canvas>() != null)
            {
                continue;
            }

            if (!hasWorldBounds)
            {
                bounds = renderer.bounds;
                hasWorldBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        Collider2D[] colliders = FindObjectsOfType<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null || !collider.gameObject.activeInHierarchy || collider.isTrigger)
            {
                continue;
            }

            if (!hasWorldBounds)
            {
                bounds = collider.bounds;
                hasWorldBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        if (!hasWorldBounds)
        {
            bounds = new Bounds(Vector3.zero, new Vector3(20f, 20f, 1f));
            hasWorldBounds = true;
        }

        Vector3 size = bounds.size;
        size.x = Mathf.Max(size.x, 12f);
        size.y = Mathf.Max(size.y, 12f);
        size.z = 1f;
        bounds.size = size;

        worldBounds = bounds;
    }

    private void AddPlayerMarker()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        trackedMarkers.Add(new MiniMapMarkerData(player.transform, new Color(0.24f, 0.78f, 0.96f, 1f), 16f, false));
    }

    private void AddEnemyMarkers()
    {
        EnemyStatsManager[] enemies = FindObjectsOfType<EnemyStatsManager>();
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null)
            {
                continue;
            }

            trackedMarkers.Add(new MiniMapMarkerData(enemies[i].transform, new Color(0.96f, 0.28f, 0.22f, 1f), 12f, false));
        }
    }

    private void AddCrystalMarkers()
    {
        CrystalInteractHandler[] crystals = FindObjectsOfType<CrystalInteractHandler>();
        for (int i = 0; i < crystals.Length; i++)
        {
            CrystalInteractHandler crystal = crystals[i];
            if (crystal == null)
            {
                continue;
            }

            Color color = crystal.resourceCategory == ArchitecturalResourceCategory.SpecialStructure
                ? new Color(0.98f, 0.82f, 0.26f, 1f)
                : crystal.resourceCategory == ArchitecturalResourceCategory.InkSupply
                    ? new Color(0.24f, 0.78f, 0.56f, 1f)
                    : new Color(0.92f, 0.92f, 0.92f, 1f);

            trackedMarkers.Add(new MiniMapMarkerData(crystal.transform, color, 8f, true));
        }
    }

    private void AddSceneMarkers()
    {
        AddMarkerByType<BaseHubGameSceneInteract>(new Color(0.98f, 0.71f, 0.24f, 1f), 14f);
        AddMarkerByType<BaseHubBookInteract>(new Color(0.75f, 0.88f, 0.42f, 1f), 14f);
        AddMarkerByType<SpiritInteract>(new Color(0.64f, 0.86f, 1f, 1f), 14f);
        AddMarkerByType<BookInteract>(new Color(0.75f, 0.88f, 0.42f, 1f), 14f);
        AddMarkerByType<CatagloueInteractHandler>(new Color(0.98f, 0.71f, 0.24f, 1f), 14f);
        AddMarkerByType<CatalogueSubmitBridgeInteractHandler>(new Color(0.98f, 0.71f, 0.24f, 1f), 14f);
    }

    private void AddMarkerByType<T>(Color color, float size) where T : MonoBehaviour
    {
        T[] markers = FindObjectsOfType<T>();
        for (int i = 0; i < markers.Length; i++)
        {
            if (markers[i] != null)
            {
                trackedMarkers.Add(new MiniMapMarkerData(markers[i].transform, color, size, false));
            }
        }
    }

    private void EnsureMarkerPool(int count)
    {
        while (markerViews.Count < count)
        {
            GameObject markerObject = CreateUiObject("Marker", mapRect);
            RectTransform markerRect = markerObject.GetComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);

            Image image = markerObject.AddComponent<Image>();
            image.sprite = GetRuntimeSprite();
            image.raycastTarget = false;

            markerViews.Add(new MiniMapMarkerView(markerRect, image));
        }
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Text CreateText(Transform parent, string name, int fontSize, TextAnchor anchor)
    {
        GameObject textObject = CreateUiObject(name, parent);
        Text text = textObject.AddComponent<Text>();
        text.font = GetRuntimeFont();
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void CreateOutline(GameObject target, Color color)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private static Sprite GetRuntimeSprite()
    {
        if (runtimeSprite != null)
        {
            return runtimeSprite;
        }

        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();
        runtimeSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        return runtimeSprite;
    }

    private static Font GetRuntimeFont()
    {
        if (runtimeFont != null)
        {
            return runtimeFont;
        }

        runtimeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return runtimeFont;
    }

    private static string GetSceneDisplayName()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name == BaseSceneName)
        {
            return "基地";
        }

        if (scene.name == GameSceneName)
        {
            return "关卡";
        }

        return scene.name;
    }
}

public readonly struct MiniMapMarkerData
{
    public readonly Transform target;
    public readonly Color color;
    public readonly float size;
    public readonly bool expandedOnly;

    public MiniMapMarkerData(Transform target, Color color, float size, bool expandedOnly)
    {
        this.target = target;
        this.color = color;
        this.size = size;
        this.expandedOnly = expandedOnly;
    }
}

public sealed class MiniMapMarkerView
{
    private readonly RectTransform rectTransform;
    private readonly Image image;

    public Transform Target { get; private set; }
    public bool ExpandedOnly { get; private set; }
    public bool IsActive => image != null && image.gameObject.activeSelf;

    public MiniMapMarkerView(RectTransform rectTransform, Image image)
    {
        this.rectTransform = rectTransform;
        this.image = image;
    }

    public void Bind(MiniMapMarkerData data)
    {
        Target = data.target;
        ExpandedOnly = data.expandedOnly;

        if (image != null)
        {
            image.color = data.color;
        }

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(data.size, data.size);
        }
    }

    public void SetPosition(Vector2 anchoredPosition)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = anchoredPosition;
        }
    }

    public void SetActive(bool active)
    {
        if (image != null)
        {
            image.gameObject.SetActive(active);
        }
    }

    public void SetVisible(bool visible)
    {
        if (image != null)
        {
            image.enabled = visible;
        }
    }
}
