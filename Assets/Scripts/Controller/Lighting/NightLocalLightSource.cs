using UnityEngine;

public enum NightLightSortingMode
{
    RelativeToSource,
    AccentOverlay
}

[DisallowMultipleComponent]
public sealed class NightLocalLightSource : MonoBehaviour
{
    public const string VisualObjectName = "NightLocalLightVisual";

    private const int DefaultSourceSortingOffset = -1;
    private const float VisualRefreshInterval = 0.1f;

    private static int mActiveTransientLightCount;

    [SerializeField] private Color lightColor = Color.white;
    [SerializeField] private float radius = 2.4f;
    [SerializeField] private float baseIntensity = 0.2f;
    [SerializeField] private float nightBoost = 0.14f;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.18f, 0f);
    [SerializeField] private bool scaleWithSceneLightMultiplier = true;
    [SerializeField] private NightLightSortingMode sortingMode = NightLightSortingMode.RelativeToSource;
    [SerializeField] private int sourceSortingOrderOffset = DefaultSourceSortingOffset;

    private SpriteRenderer lightRenderer;
    private SpriteRenderer sourceRenderer;
    private float mNextVisualRefreshTime;
    private bool mbTransient;
    private bool mbHasTransientBudget;

    public void Configure(
        Color color,
        float radius,
        float baseIntensity,
        float nightBoost,
        Vector3 localOffset,
        bool scaleWithSceneLightMultiplier,
        NightLightSortingMode sortingMode = NightLightSortingMode.RelativeToSource,
        int sourceSortingOrderOffset = DefaultSourceSortingOffset)
    {
        this.lightColor = color;
        this.radius = Mathf.Max(0.1f, radius);
        this.baseIntensity = Mathf.Max(0f, baseIntensity);
        this.nightBoost = Mathf.Max(0f, nightBoost);
        this.localOffset = localOffset;
        this.scaleWithSceneLightMultiplier = scaleWithSceneLightMultiplier;
        this.sortingMode = sortingMode;
        this.sourceSortingOrderOffset = sourceSortingOrderOffset;

        EnsureRenderer();
        UpdateVisual();
    }

    private void OnEnable()
    {
        acquireTransientBudget();
        EnsureRenderer();
        UpdateVisual();
    }

    private void OnDisable()
    {
        releaseTransientBudget();
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime < mNextVisualRefreshTime)
        {
            return;
        }

        mNextVisualRefreshTime = Time.unscaledTime + VisualRefreshInterval;
        UpdateVisual();
    }

    public void SetTransientBudgeted(bool transient)
    {
        if (mbTransient == transient)
        {
            return;
        }

        releaseTransientBudget();
        mbTransient = transient;
        acquireTransientBudget();
        UpdateVisual();
    }

    private void EnsureRenderer()
    {
        if (lightRenderer != null)
        {
            return;
        }

        Transform visualTransform = transform.Find(VisualObjectName);
        GameObject visualObject = visualTransform != null ? visualTransform.gameObject : new GameObject(VisualObjectName);
        visualObject.transform.SetParent(transform, false);
        visualObject.layer = NightLightingLayers.VisualLayer;
        visualObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        lightRenderer = visualObject.GetComponent<SpriteRenderer>();
        if (lightRenderer == null)
        {
            lightRenderer = visualObject.AddComponent<SpriteRenderer>();
        }

        lightRenderer.sprite = NightLightingVisualFactory.GetRadialGlowSprite();
        lightRenderer.sharedMaterial = NightLightingVisualFactory.GetAdditiveGlowMaterial();
        lightRenderer.sortingLayerID = NightLightingVisualFactory.GetTopSortingLayerId();
        lightRenderer.sortingOrder = NightLightingController.LocalLightSortingOrder;
    }

    private void UpdateVisual()
    {
        if (lightRenderer == null)
        {
            return;
        }

        float controllerMultiplier = scaleWithSceneLightMultiplier ? NightLightingController.ActiveLightMultiplier : 1f;
        float intensity = (baseIntensity + nightBoost * NightLightingController.ActiveNightProgress) * controllerMultiplier;
        bool visible = NightLightingController.HasActiveProfile &&
                       intensity > 0.001f &&
                       (!mbTransient || mbHasTransientBudget);

        lightRenderer.enabled = visible;
        if (!visible)
        {
            return;
        }

        lightRenderer.transform.localPosition = localOffset;
        lightRenderer.transform.localScale = new Vector3(radius, radius, 1f);
        lightRenderer.sharedMaterial = NightLightingVisualFactory.GetAdditiveGlowMaterial();
        ApplySorting();

        Color color = lightColor;
        color.a = Mathf.Clamp01(intensity);
        lightRenderer.color = color;
    }

    private void ApplySorting()
    {
        if (sortingMode == NightLightSortingMode.AccentOverlay)
        {
            lightRenderer.sortingLayerID = NightLightingVisualFactory.GetTopSortingLayerId();
            lightRenderer.sortingOrder = NightLightingController.LocalLightSortingOrder;
            return;
        }

        SpriteRenderer source = ResolveSourceRenderer();
        if (source == null)
        {
            lightRenderer.sortingLayerID = NightLightingVisualFactory.GetTopSortingLayerId();
            lightRenderer.sortingOrder = NightLightingController.LocalLightSortingOrder;
            return;
        }

        lightRenderer.sortingLayerID = NightLightingVisualFactory.GetTopSortingLayerId();
        lightRenderer.sortingOrder = NightLightingController.LocalLightSortingOrder + sourceSortingOrderOffset;
    }

    private SpriteRenderer ResolveSourceRenderer()
    {
        if (sourceRenderer != null && sourceRenderer != lightRenderer)
        {
            return sourceRenderer;
        }

        sourceRenderer = NightLightingController.ResolvePrimarySpriteRenderer(gameObject);
        return sourceRenderer;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void resetTransientBudget()
    {
        mActiveTransientLightCount = 0;
    }

    private void acquireTransientBudget()
    {
        if (!mbTransient || mbHasTransientBudget || !isActiveAndEnabled)
        {
            return;
        }

        int limit = GameplayPerformanceSettings.Profile.MaxTransientLights;
        if (mActiveTransientLightCount >= limit)
        {
            return;
        }

        mActiveTransientLightCount++;
        mbHasTransientBudget = true;
    }

    private void releaseTransientBudget()
    {
        if (!mbHasTransientBudget)
        {
            return;
        }

        mActiveTransientLightCount = Mathf.Max(0, mActiveTransientLightCount - 1);
        mbHasTransientBudget = false;
    }
}
