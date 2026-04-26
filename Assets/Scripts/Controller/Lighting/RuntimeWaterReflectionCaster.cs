using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class RuntimeWaterReflectionCaster : MonoBehaviour
{
    private const string ReflectionObjectName = "RuntimeWaterReflection";
    private const float ReflectionAlpha = 0.22f;
    private static readonly Color ReflectionTint = new Color(0.58f, 0.78f, 0.86f, 1f);
    private static readonly Vector3 ReflectionLocalScale = new Vector3(1f, -0.58f, 1f);

    [SerializeField] private SpriteRenderer sourceRenderer;
    [SerializeField] private SpriteRenderer reflectionRenderer;

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
        EnsureReflectionRenderer();
        SyncReflection();
    }

    private void OnEnable()
    {
        sourceRenderer ??= GetComponent<SpriteRenderer>();
        EnsureReflectionRenderer();
        SyncReflection();
    }

    private void LateUpdate()
    {
        SyncReflection();
    }

    private void EnsureReflectionRenderer()
    {
        if (reflectionRenderer != null)
        {
            return;
        }

        Transform existing = transform.Find(ReflectionObjectName);
        GameObject reflectionObject = existing != null
            ? existing.gameObject
            : new GameObject(ReflectionObjectName);

        reflectionObject.transform.SetParent(transform, false);
        reflectionObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        reflectionRenderer = reflectionObject.GetComponent<SpriteRenderer>();
        if (reflectionRenderer == null)
        {
            reflectionRenderer = reflectionObject.AddComponent<SpriteRenderer>();
        }

        reflectionRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        reflectionRenderer.receiveShadows = false;
    }

    private void SyncReflection()
    {
        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponent<SpriteRenderer>();
        }

        if (sourceRenderer == null)
        {
            return;
        }

        EnsureReflectionRenderer();

        bool shouldShow = sourceRenderer.enabled &&
                          sourceRenderer.sprite != null &&
                          RuntimeWaterReflectionSceneController.IsNearWater(sourceRenderer.bounds.center);
        reflectionRenderer.enabled = shouldShow;
        if (!shouldShow)
        {
            return;
        }

        Sprite sourceSprite = sourceRenderer.sprite;
        float localHeight = sourceSprite != null ? Mathf.Max(0.18f, sourceSprite.bounds.size.y) : 0.22f;
        Transform reflectionTransform = reflectionRenderer.transform;
        reflectionTransform.localPosition = new Vector3(0f, -localHeight * 0.62f, 0f);
        reflectionTransform.localRotation = Quaternion.identity;
        reflectionTransform.localScale = ReflectionLocalScale;

        reflectionRenderer.sprite = sourceSprite;
        reflectionRenderer.flipX = sourceRenderer.flipX;
        reflectionRenderer.flipY = sourceRenderer.flipY;
        reflectionRenderer.color = new Color(
            ReflectionTint.r,
            ReflectionTint.g,
            ReflectionTint.b,
            ReflectionAlpha * sourceRenderer.color.a);
        reflectionRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        reflectionRenderer.sortingOrder = sourceRenderer.sortingOrder - 2;
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
        if (owner == null || string.Equals(owner.name, ShimmerObjectName, StringComparison.Ordinal))
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
