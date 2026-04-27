using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class RuntimeWaterReflectionCaster : MonoBehaviour
{
    internal const string ShadowObjectName = "RuntimeWaterShadow";
    internal const string LegacyReflectionObjectName = "RuntimeWaterReflection";
    private const float ShadowBaseAlpha = 0.26f;
    private const float ShadowNightAlphaBoost = 0.10f;
    private const float ShadowWidthFactor = 0.82f;
    private const float ShadowHeightFactor = 0.16f;
    private const float MinimumShadowWidth = 0.24f;
    private const float MinimumShadowHeight = 0.06f;
    private static readonly Color ShadowTint = new Color(0.012f, 0.015f, 0.022f, 1f);

    [SerializeField] private SpriteRenderer sourceRenderer;
    [SerializeField] private SpriteRenderer shadowRenderer;

    public static RuntimeWaterReflectionCaster EnsureForRenderer(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return null;
        }

        RuntimeWaterReflectionCaster caster = renderer.GetComponent<RuntimeWaterReflectionCaster>();
        if (caster == null)
        {
            caster = renderer.gameObject.AddComponent<RuntimeWaterReflectionCaster>();
        }

        caster.Configure(renderer);
        return caster;
    }

    private void Configure(SpriteRenderer renderer)
    {
        sourceRenderer = renderer;
        EnsureShadowRenderer();
        SyncShadow();
    }

    private void OnEnable()
    {
        sourceRenderer ??= GetComponent<SpriteRenderer>();
        EnsureShadowRenderer();
        SyncShadow();
    }

    private void LateUpdate()
    {
        SyncShadow();
    }

    private void EnsureShadowRenderer()
    {
        if (shadowRenderer != null)
        {
            return;
        }

        Transform existing = transform.Find(ShadowObjectName) ?? transform.Find(LegacyReflectionObjectName);
        GameObject shadowObject = existing != null
            ? existing.gameObject
            : new GameObject(ShadowObjectName);

        shadowObject.name = ShadowObjectName;
        shadowObject.transform.SetParent(transform, false);
        shadowObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
        if (shadowRenderer == null)
        {
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        }

        shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        shadowRenderer.receiveShadows = false;
    }

    private void SyncShadow()
    {
        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponent<SpriteRenderer>();
        }

        if (sourceRenderer == null)
        {
            return;
        }

        EnsureShadowRenderer();

        bool shouldShow = sourceRenderer.enabled &&
                          sourceRenderer.sprite != null &&
                          RuntimeWaterReflectionSceneController.IsNearWater(sourceRenderer.bounds.center);
        shadowRenderer.enabled = shouldShow;
        if (!shouldShow)
        {
            return;
        }

        Sprite sourceSprite = sourceRenderer.sprite;
        Vector3 shadowScale = ResolveShadowScale(sourceSprite);
        float localHeight = sourceSprite != null ? Mathf.Max(0.18f, sourceSprite.bounds.size.y) : 0.22f;
        Transform shadowTransform = shadowRenderer.transform;
        shadowTransform.localPosition = new Vector3(0f, -localHeight * 0.42f, 0f);
        shadowTransform.localRotation = Quaternion.identity;
        shadowTransform.localScale = shadowScale;

        shadowRenderer.sprite = ProjectedShadowFollower.GetOrCreateEllipseShadowSprite();
        shadowRenderer.sharedMaterial = null;
        shadowRenderer.flipX = false;
        shadowRenderer.flipY = false;
        shadowRenderer.color = ResolveShadowColor();
        shadowRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        shadowRenderer.sortingOrder = sourceRenderer.sortingOrder - 2;
    }

    private static Vector3 ResolveShadowScale(Sprite sourceSprite)
    {
        if (sourceSprite == null)
        {
            return new Vector3(MinimumShadowWidth, MinimumShadowHeight, 1f);
        }

        Bounds bounds = sourceSprite.bounds;
        float width = Mathf.Max(MinimumShadowWidth, bounds.size.x * ShadowWidthFactor);
        float height = Mathf.Max(MinimumShadowHeight, bounds.size.y * ShadowHeightFactor);
        return new Vector3(width, height, 1f);
    }

    private Color ResolveShadowColor()
    {
        float alpha = ShadowBaseAlpha + NightLightingController.ActiveNightProgress * ShadowNightAlphaBoost;
        return new Color(
            ShadowTint.r,
            ShadowTint.g,
            ShadowTint.b,
            Mathf.Clamp01(alpha * sourceRenderer.color.a));
    }
}

public sealed class RuntimeWaterReflectionSceneController : MonoBehaviour
{
    private const string ControllerName = "RuntimeWaterReflectionSceneController";
    private const string ShimmerObjectName = "RuntimeWaterShimmer";
    private const float RescanInterval = 0.8f;
    private const float ShimmerPixelsPerUnit = 32f;
    private const int ShimmerTextureWidth = 64;
    private const int ShimmerTextureHeight = 16;
    private const int MaxShimmerTileQuads = 12000;
    private static readonly List<Bounds> WaterBounds = new List<Bounds>();
    private static RuntimeWaterReflectionSceneController instance;
    private static Sprite sharedShimmerSprite;
    private static bool isRefreshing;

    private float rescanTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureForScene(SceneManager.GetActiveScene());
    }

    public static bool IsNearWater(Vector3 worldPosition)
    {
        if (instance == null)
        {
            EnsureForScene(SceneManager.GetActiveScene());
        }

        if (WaterBounds.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < WaterBounds.Count; i++)
        {
            Bounds expanded = WaterBounds[i];
            expanded.Expand(new Vector3(2.2f, 2.6f, 0f));
            if (expanded.Contains(new Vector3(worldPosition.x, worldPosition.y, expanded.center.z)))
            {
                return true;
            }
        }

        return false;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    private static void EnsureForScene(Scene scene)
    {
        if (instance == null)
        {
            GameObject existing = GameObject.Find(ControllerName);
            instance = existing != null
                ? existing.GetComponent<RuntimeWaterReflectionSceneController>()
                : null;

            if (instance == null)
            {
                GameObject controllerObject = new GameObject(ControllerName);
                if (scene.IsValid() && scene.isLoaded)
                {
                    SceneManager.MoveGameObjectToScene(controllerObject, scene);
                }

                instance = controllerObject.AddComponent<RuntimeWaterReflectionSceneController>();
            }
        }

        if (!isRefreshing)
        {
            instance.RefreshSceneBindings();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        RefreshSceneBindings();
    }

    private void Update()
    {
        rescanTimer -= Time.unscaledDeltaTime;
        if (rescanTimer > 0f)
        {
            return;
        }

        RefreshSceneBindings();
    }

    private void RefreshSceneBindings()
    {
        if (isRefreshing)
        {
            return;
        }

        isRefreshing = true;
        rescanTimer = RescanInterval;
        try
        {
            ScanWaterSurfaces();
            AttachReflectiveRuntimeActors();
        }
        finally
        {
            isRefreshing = false;
        }
    }

    private static void ScanWaterSurfaces()
    {
        WaterBounds.Clear();
        Renderer[] renderers = FindObjectsOfType<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !IsWaterObject(renderer.gameObject))
            {
                continue;
            }

            Bounds bounds = renderer.bounds;
            if (bounds.size.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            WaterBounds.Add(bounds);
            if (ShouldCreateWaterShimmer(renderer, bounds))
            {
                EnsureWaterShimmer(renderer, bounds);
            }
            else
            {
                RemoveWaterShimmer(renderer);
            }
        }

        Collider2D[] colliders = FindObjectsOfType<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider != null && IsWaterObject(collider.gameObject))
            {
                WaterBounds.Add(collider.bounds);
            }
        }
    }

    private static void AttachReflectiveRuntimeActors()
    {
        SpriteRenderer[] renderers = FindObjectsOfType<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer != null && ShouldAttachReflection(renderer))
            {
                RuntimeWaterReflectionCaster.EnsureForRenderer(renderer);
            }
        }
    }

    private static bool ShouldAttachReflection(SpriteRenderer renderer)
    {
        GameObject owner = renderer.gameObject;
        if (owner == null || IsGeneratedRuntimeVisual(owner))
        {
            return false;
        }

        if (owner.GetComponentInParent<RuntimeWaterReflectionCaster>() != null &&
            owner.GetComponent<RuntimeWaterReflectionCaster>() == null)
        {
            return false;
        }

        if (owner.CompareTag("Player") || owner.GetComponentInParent<PlayerMove>() != null)
        {
            return true;
        }

        if (owner.GetComponentInParent<SpriteCompanionFollowController>() != null ||
            owner.name.IndexOf("SpriteCompanion", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        RepairableBuildingVisual visual = owner.GetComponentInParent<RepairableBuildingVisual>();
        RepairableBuildingGroup group = owner.GetComponentInParent<RepairableBuildingGroup>();
        return visual != null &&
               group != null &&
               RuntimeProgressState.EnsureInstance().IsBuildingRepaired(group.BuildingId);
    }

    private static bool IsGeneratedRuntimeVisual(GameObject owner)
    {
        string name = owner.name;
        return string.Equals(name, ShimmerObjectName, StringComparison.Ordinal) ||
               string.Equals(name, RuntimeWaterReflectionCaster.ShadowObjectName, StringComparison.Ordinal) ||
               string.Equals(name, RuntimeWaterReflectionCaster.LegacyReflectionObjectName, StringComparison.Ordinal) ||
               string.Equals(name, ProjectedShadowFollower.ShadowObjectName, StringComparison.Ordinal) ||
               string.Equals(name, NightLocalLightSource.VisualObjectName, StringComparison.Ordinal);
    }

    private static bool IsWaterObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        if (string.Equals(gameObject.tag, "Water", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        for (Transform current = gameObject.transform; current != null; current = current.parent)
        {
            string name = current.gameObject.name;
            if (name.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("水", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldCreateWaterShimmer(Renderer waterRenderer, Bounds bounds)
    {
        if (waterRenderer == null || IsCollisionOnlyWaterRenderer(waterRenderer))
        {
            return false;
        }

        return EstimateShimmerTileQuads(bounds.size) <= MaxShimmerTileQuads;
    }

    private static bool IsCollisionOnlyWaterRenderer(Renderer renderer)
    {
        if (renderer.GetComponent<TilemapRenderer>() == null ||
            renderer.GetComponent<TilemapCollider2D>() == null)
        {
            return false;
        }

        string name = renderer.gameObject.name;
        return name.IndexOf("Collision", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Collider", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("碰撞", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static int EstimateShimmerTileQuads(Vector3 boundsSize)
    {
        float tileWidth = ShimmerTextureWidth / ShimmerPixelsPerUnit;
        float tileHeight = ShimmerTextureHeight / ShimmerPixelsPerUnit;
        int columns = Mathf.CeilToInt(Mathf.Max(0.4f, boundsSize.x) / tileWidth);
        int rows = Mathf.CeilToInt(Mathf.Max(0.25f, boundsSize.y) / tileHeight);
        return columns * rows;
    }

    private static void RemoveWaterShimmer(Renderer waterRenderer)
    {
        if (waterRenderer == null)
        {
            return;
        }

        Transform existing = waterRenderer.transform.Find(ShimmerObjectName);
        if (existing == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(existing.gameObject);
        }
        else
        {
            DestroyImmediate(existing.gameObject);
        }
    }

    private static void EnsureWaterShimmer(Renderer waterRenderer, Bounds bounds)
    {
        Transform existing = waterRenderer.transform.Find(ShimmerObjectName);
        GameObject shimmerObject = existing != null
            ? existing.gameObject
            : new GameObject(ShimmerObjectName);
        shimmerObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        shimmerObject.transform.SetParent(waterRenderer.transform, true);
        shimmerObject.transform.position = bounds.center + new Vector3(0f, 0f, -0.02f);
        shimmerObject.transform.rotation = Quaternion.identity;
        shimmerObject.transform.localScale = Vector3.one;

        SpriteRenderer shimmerRenderer = shimmerObject.GetComponent<SpriteRenderer>();
        if (shimmerRenderer == null)
        {
            shimmerRenderer = shimmerObject.AddComponent<SpriteRenderer>();
        }

        shimmerRenderer.sprite = GetOrCreateShimmerSprite();
        shimmerRenderer.drawMode = SpriteDrawMode.Tiled;
        shimmerRenderer.size = new Vector2(Mathf.Max(0.4f, bounds.size.x), Mathf.Max(0.25f, bounds.size.y));
        shimmerRenderer.sortingLayerID = waterRenderer.sortingLayerID;
        shimmerRenderer.sortingOrder = waterRenderer.sortingOrder + 1;
        shimmerRenderer.color = new Color(0.66f, 0.88f, 1f, 0.12f);

        RuntimeWaterShimmerOverlay overlay = shimmerObject.GetComponent<RuntimeWaterShimmerOverlay>();
        if (overlay == null)
        {
            overlay = shimmerObject.AddComponent<RuntimeWaterShimmerOverlay>();
        }

        overlay.Configure(shimmerRenderer, bounds.center);
    }

    private static Sprite GetOrCreateShimmerSprite()
    {
        if (sharedShimmerSprite != null)
        {
            return sharedShimmerSprite;
        }

        Texture2D texture = new Texture2D(ShimmerTextureWidth, ShimmerTextureHeight, TextureFormat.RGBA32, false)
        {
            name = "RuntimeWaterShimmerTexture",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < ShimmerTextureHeight; y++)
        {
            for (int x = 0; x < ShimmerTextureWidth; x++)
            {
                bool stripe = (x + y * 3) % 19 < 4;
                byte alpha = stripe ? (byte)58 : (byte)0;
                texture.SetPixel(x, y, new Color32(210, 242, 255, alpha));
            }
        }

        texture.Apply(false, false);
        sharedShimmerSprite = Sprite.Create(texture, new Rect(0f, 0f, ShimmerTextureWidth, ShimmerTextureHeight), new Vector2(0.5f, 0.5f), ShimmerPixelsPerUnit);
        sharedShimmerSprite.name = "RuntimeWaterShimmerSprite";
        return sharedShimmerSprite;
    }
}

public sealed class RuntimeWaterShimmerOverlay : MonoBehaviour
{
    private SpriteRenderer shimmerRenderer;
    private Vector3 basePosition;

    public void Configure(SpriteRenderer renderer, Vector3 worldCenter)
    {
        shimmerRenderer = renderer;
        basePosition = worldCenter + new Vector3(0f, 0f, -0.02f);
    }

    private void LateUpdate()
    {
        if (shimmerRenderer == null)
        {
            shimmerRenderer = GetComponent<SpriteRenderer>();
        }

        if (shimmerRenderer == null)
        {
            return;
        }

        float wave = Mathf.Sin(Time.unscaledTime * 1.7f + transform.position.x * 0.23f);
        transform.position = basePosition + new Vector3(wave * 0.035f, Mathf.Sin(Time.unscaledTime * 1.2f) * 0.018f, 0f);
        shimmerRenderer.color = new Color(0.66f, 0.88f, 1f, 0.10f + Mathf.Abs(wave) * 0.06f);
    }
}
