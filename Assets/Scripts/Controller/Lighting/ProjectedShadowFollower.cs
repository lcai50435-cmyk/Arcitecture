using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProjectedShadowFollower : MonoBehaviour
{
    private const string ShadowObjectName = "ProjectedShadow";
    private const int ShadowTextureWidth = 64;
    private const int ShadowTextureHeight = 32;
    private const float ShadowPixelsPerUnit = 64f;
    private const float GroundContactLift = 0.012f;
    private const float HorizontalOffsetFactor = 0.30f;
    private const float VerticalOffsetFactor = 0.10f;
    private const float EllipseWidthFactor = 0.96f;
    private const float EllipseHeightFactor = 0.68f;
    private const float MinimumShadowWidth = 0.20f;
    private const float MinimumShadowHeight = 0.08f;

    [SerializeField] private Vector3 localOffset = new Vector3(0.18f, -0.28f, 0f);
    [SerializeField] private Vector3 scaleMultiplier = new Vector3(1.1f, 0.42f, 1f);
    [SerializeField] private Color shadowColor = new Color(0.04f, 0.05f, 0.08f, 0.42f);
    [SerializeField] private int sortingOrderOffset = -1;

    private static Sprite sharedEllipseShadowSprite;

    private SpriteRenderer sourceRenderer;
    private SpriteRenderer shadowRenderer;

    public void Configure(
        SpriteRenderer sourceRenderer,
        Vector3 localOffset,
        Vector3 scaleMultiplier,
        Color shadowColor,
        int sortingOrderOffset)
    {
        this.sourceRenderer = sourceRenderer;
        this.localOffset = localOffset;
        this.scaleMultiplier = scaleMultiplier;
        this.shadowColor = shadowColor;
        this.sortingOrderOffset = sortingOrderOffset;

        EnsureShadowRenderer();
        UpdateShadow();
    }

    private void OnEnable()
    {
        if (sourceRenderer == null)
        {
            sourceRenderer = ResolveSourceRenderer();
        }

        EnsureShadowRenderer();
        UpdateShadow();
    }

    private void LateUpdate()
    {
        if (sourceRenderer == null)
        {
            sourceRenderer = ResolveSourceRenderer();
        }

        UpdateShadow();
    }

    private SpriteRenderer ResolveSourceRenderer()
    {
        SpriteRenderer directRenderer = GetComponent<SpriteRenderer>();
        if (directRenderer != null)
        {
            return directRenderer;
        }

        SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < childRenderers.Length; i++)
        {
            SpriteRenderer candidate = childRenderers[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.GetComponent<ProjectedShadowFollowerShadowMarker>() != null)
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private void EnsureShadowRenderer()
    {
        if (shadowRenderer != null)
        {
            if (sourceRenderer != null && shadowRenderer.transform.parent != sourceRenderer.transform)
            {
                shadowRenderer.transform.SetParent(sourceRenderer.transform, false);
            }

            return;
        }

        ProjectedShadowFollowerShadowMarker existingMarker = GetComponentInChildren<ProjectedShadowFollowerShadowMarker>(true);
        GameObject shadowObject = existingMarker != null ? existingMarker.gameObject : new GameObject(ShadowObjectName);
        shadowObject.layer = NightLightingLayers.VisualLayer;

        if (shadowObject.GetComponent<ProjectedShadowFollowerShadowMarker>() == null)
        {
            shadowObject.AddComponent<ProjectedShadowFollowerShadowMarker>();
        }

        shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
        if (shadowRenderer == null)
        {
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        }

        if (sourceRenderer != null)
        {
            shadowObject.transform.SetParent(sourceRenderer.transform, false);
        }
        else
        {
            shadowObject.transform.SetParent(transform, false);
        }
    }

    private void UpdateShadow()
    {
        if (shadowRenderer == null || sourceRenderer == null)
        {
            if (shadowRenderer != null)
            {
                shadowRenderer.enabled = false;
            }

            return;
        }

        bool visible = NightLightingController.HasActiveProfile && sourceRenderer.enabled && sourceRenderer.sprite != null;
        shadowRenderer.enabled = visible;
        if (!visible)
        {
            return;
        }

        shadowRenderer.sprite = GetOrCreateEllipseShadowSprite();
        shadowRenderer.flipX = false;
        shadowRenderer.flipY = false;
        shadowRenderer.color = shadowColor;
        shadowRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        shadowRenderer.sortingOrder = sourceRenderer.sortingOrder + sortingOrderOffset;

        shadowRenderer.transform.localPosition = ResolveShadowLocalPosition();
        shadowRenderer.transform.localRotation = Quaternion.identity;
        shadowRenderer.transform.localScale = ResolveShadowLocalScale();
    }

    private Vector3 ResolveShadowLocalPosition()
    {
        Sprite sprite = sourceRenderer != null ? sourceRenderer.sprite : null;
        if (sprite == null)
        {
            return localOffset;
        }

        Bounds spriteBounds = sprite.bounds;
        return new Vector3(
            spriteBounds.center.x + localOffset.x * HorizontalOffsetFactor,
            spriteBounds.min.y + GroundContactLift + localOffset.y * VerticalOffsetFactor,
            0f);
    }

    private Vector3 ResolveShadowLocalScale()
    {
        Sprite sprite = sourceRenderer != null ? sourceRenderer.sprite : null;
        if (sprite == null)
        {
            return scaleMultiplier;
        }

        Bounds spriteBounds = sprite.bounds;
        float width = Mathf.Max(MinimumShadowWidth, spriteBounds.size.x * scaleMultiplier.x * EllipseWidthFactor);
        float height = Mathf.Max(MinimumShadowHeight, spriteBounds.size.y * scaleMultiplier.y * EllipseHeightFactor);
        return new Vector3(width, height, 1f);
    }

    private static Sprite GetOrCreateEllipseShadowSprite()
    {
        if (sharedEllipseShadowSprite != null)
        {
            return sharedEllipseShadowSprite;
        }

        Texture2D texture = new Texture2D(ShadowTextureWidth, ShadowTextureHeight, TextureFormat.ARGB32, false)
        {
            name = "RuntimeProjectedEllipseShadow",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color32[] pixels = new Color32[ShadowTextureWidth * ShadowTextureHeight];
        for (int y = 0; y < ShadowTextureHeight; y++)
        {
            float normalizedY = (y + 0.5f) / ShadowTextureHeight * 2f - 1f;
            for (int x = 0; x < ShadowTextureWidth; x++)
            {
                float normalizedX = (x + 0.5f) / ShadowTextureWidth * 2f - 1f;
                float distance = normalizedX * normalizedX + normalizedY * normalizedY;
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 0.75f) * 0.96f;
                pixels[y * ShadowTextureWidth + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        sharedEllipseShadowSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, ShadowTextureWidth, ShadowTextureHeight),
            new Vector2(0.5f, 0.5f),
            ShadowPixelsPerUnit);
        sharedEllipseShadowSprite.name = "RuntimeProjectedEllipseShadow";
        sharedEllipseShadowSprite.hideFlags = HideFlags.HideAndDontSave;
        return sharedEllipseShadowSprite;
    }
}

public sealed class ProjectedShadowFollowerShadowMarker : MonoBehaviour
{
}
