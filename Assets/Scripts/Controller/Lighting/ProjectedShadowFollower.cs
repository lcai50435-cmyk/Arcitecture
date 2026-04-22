using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProjectedShadowFollower : MonoBehaviour
{
    private const string ShadowObjectName = "ProjectedShadow";
    private const float GroundContactLift = 0.02f;
    private const float HorizontalOffsetFactor = 0.65f;
    private const float VerticalOffsetFactor = 0.12f;

    [SerializeField] private Vector3 localOffset = new Vector3(0.18f, -0.28f, 0f);
    [SerializeField] private Vector3 scaleMultiplier = new Vector3(1.1f, 0.42f, 1f);
    [SerializeField] private Color shadowColor = new Color(0.04f, 0.05f, 0.08f, 0.42f);
    [SerializeField] private int sortingOrderOffset = -1;

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

        shadowRenderer.sprite = sourceRenderer.sprite;
        shadowRenderer.flipX = sourceRenderer.flipX;
        shadowRenderer.flipY = false;
        shadowRenderer.color = shadowColor;
        shadowRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        shadowRenderer.sortingOrder = sourceRenderer.sortingOrder + sortingOrderOffset;

        shadowRenderer.transform.localPosition = ResolveShadowLocalPosition();
        shadowRenderer.transform.localRotation = Quaternion.identity;
        shadowRenderer.transform.localScale = scaleMultiplier;
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
}

public sealed class ProjectedShadowFollowerShadowMarker : MonoBehaviour
{
}
