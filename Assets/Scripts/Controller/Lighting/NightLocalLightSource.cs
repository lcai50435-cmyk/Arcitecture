using UnityEngine;

[DisallowMultipleComponent]
public sealed class NightLocalLightSource : MonoBehaviour
{
    private const string VisualObjectName = "NightLocalLightVisual";
    private const int BaseSortingOrder = 28200;

    [SerializeField] private Color lightColor = Color.white;
    [SerializeField] private float radius = 2.4f;
    [SerializeField] private float baseIntensity = 0.2f;
    [SerializeField] private float nightBoost = 0.14f;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.18f, 0f);

    private SpriteRenderer lightRenderer;

    public void Configure(
        Color color,
        float radius,
        float baseIntensity,
        float nightBoost,
        Vector3 localOffset)
    {
        this.lightColor = color;
        this.radius = Mathf.Max(0.1f, radius);
        this.baseIntensity = Mathf.Max(0f, baseIntensity);
        this.nightBoost = Mathf.Max(0f, nightBoost);
        this.localOffset = localOffset;

        EnsureRenderer();
        UpdateVisual();
    }

    private void OnEnable()
    {
        EnsureRenderer();
        UpdateVisual();
    }

    private void LateUpdate()
    {
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

        lightRenderer = visualObject.GetComponent<SpriteRenderer>();
        if (lightRenderer == null)
        {
            lightRenderer = visualObject.AddComponent<SpriteRenderer>();
        }

        lightRenderer.sprite = NightLightingVisualFactory.GetRadialGlowSprite();
        lightRenderer.sortingLayerID = NightLightingVisualFactory.GetTopSortingLayerId();
        lightRenderer.sortingOrder = BaseSortingOrder;
    }

    private void UpdateVisual()
    {
        if (lightRenderer == null)
        {
            return;
        }

        float controllerMultiplier = NightLightingController.ActiveLightMultiplier;
        float intensity = (baseIntensity + nightBoost * NightLightingController.ActiveNightProgress) * controllerMultiplier;
        bool visible = NightLightingController.HasActiveProfile && intensity > 0.001f;

        lightRenderer.enabled = visible;
        if (!visible)
        {
            return;
        }

        lightRenderer.transform.localPosition = localOffset;
        lightRenderer.transform.localScale = new Vector3(radius, radius, 1f);
        lightRenderer.sortingLayerID = NightLightingVisualFactory.GetTopSortingLayerId();
        lightRenderer.sortingOrder = BaseSortingOrder;

        Color color = lightColor;
        color.a = Mathf.Clamp01(intensity);
        lightRenderer.color = color;
    }
}
