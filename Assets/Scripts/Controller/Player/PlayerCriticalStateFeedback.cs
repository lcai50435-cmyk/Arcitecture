using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家濒死时的整屏红色危险反馈。
/// </summary>
public class PlayerCriticalStateFeedback : MonoBehaviour
{
    private const float CriticalThreshold = 0.25f;
    private const float PresenceFadeInSpeed = 12f;
    private const float PresenceFadeOutSpeed = 5.5f;
    private const float DangerResponseSpeed = 10f;
    private const float BurstDecaySpeed = 3.2f;
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;
    private const int OverlaySortingOrder = 11000;
    private const int EdgeParticleCount = 20;
    private const float Tau = Mathf.PI * 2f;

    private static readonly Color VignetteColor = new Color(0.85f, 0.08f, 0.03f, 0f);
    private static readonly Color PrimaryFlowColor = new Color(1f, 0.18f, 0.08f, 0f);
    private static readonly Color SecondaryFlowColor = new Color(1f, 0.48f, 0.18f, 0f);
    private static readonly Color EmberLowColor = new Color(0.72f, 0.08f, 0.04f, 0f);
    private static readonly Color EmberHighColor = new Color(1f, 0.48f, 0.18f, 0f);

    private static readonly Texture2D[] PrimaryFlowTextures = new Texture2D[4];
    private static readonly Texture2D[] SecondaryFlowTextures = new Texture2D[4];

    private static Sprite vignetteSprite;
    private static Sprite emberSprite;

    private CharacterCore characterCore;
    private Canvas overlayCanvas;
    private RectTransform overlayRect;
    private Image vignetteImage;
    private EdgeFlowBand[] edgeBands;
    private ScreenParticle[] screenParticles;
    private bool isSubscribed;
    private float displayPresence;
    private float displayDanger;
    private float damageBurst;

    private enum EdgeSide
    {
        Top = 0,
        Bottom = 1,
        Left = 2,
        Right = 3
    }

    private sealed class EdgeFlowBand
    {
        public EdgeSide Side;
        public RectTransform Root;
        public RawImage Primary;
        public RawImage Secondary;
    }

    private sealed class ScreenParticle
    {
        public EdgeSide Side;
        public RectTransform Rect;
        public Image Image;
        public float Along;
        public float AlongSpeed;
        public float DriftSpeed;
        public float BaseSize;
        public float Phase;
        public float ColorT;
        public float AlphaScale;
        public float JitterScale;
    }

    public static PlayerCriticalStateFeedback Ensure(GameObject playerObject)
    {
        if (playerObject == null)
        {
            return null;
        }

        PlayerCriticalStateFeedback feedback = playerObject.GetComponent<PlayerCriticalStateFeedback>();
        if (feedback == null)
        {
            feedback = playerObject.AddComponent<PlayerCriticalStateFeedback>();
        }

        return feedback;
    }

    private void Awake()
    {
        characterCore = GetComponent<CharacterCore>();
        if (characterCore == null)
        {
            enabled = false;
            return;
        }

        SubscribeEvents();
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void Update()
    {
        if (characterCore == null)
        {
            return;
        }

        if (ShouldForceImmediateHide())
        {
            HideImmediately();
            return;
        }

        UpdateEffectState();

        bool shouldKeepOverlay = displayPresence > 0.001f || damageBurst > 0.001f;
        if (!shouldKeepOverlay)
        {
            DestroyScreenOverlayImmediate();
            return;
        }

        EnsureScreenOverlay();
        UpdateOverlayPresentation(Time.unscaledTime, Time.unscaledDeltaTime);
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        HideImmediately();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        HideImmediately();
    }

    private void SubscribeEvents()
    {
        if (isSubscribed || characterCore == null)
        {
            return;
        }

        characterCore.OnTakeDamage += HandleTakeDamage;
        characterCore.OnTakeDamageWithValue += HandleTakeDamageWithValue;
        characterCore.OnDeath += HandleDeath;
        isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed || characterCore == null)
        {
            return;
        }

        characterCore.OnTakeDamage -= HandleTakeDamage;
        characterCore.OnTakeDamageWithValue -= HandleTakeDamageWithValue;
        characterCore.OnDeath -= HandleDeath;
        isSubscribed = false;
    }

    private void HandleTakeDamage()
    {
        ApplyDamageBurst(0.1f);
    }

    private void HandleTakeDamageWithValue(float damage)
    {
        float maxHp = ResolveMaxHp();
        float normalizedDamage = maxHp > Mathf.Epsilon ? Mathf.Clamp01(damage / maxHp) : 0f;
        ApplyDamageBurst(0.18f + normalizedDamage * 0.45f);
    }

    private void HandleDeath()
    {
        HideImmediately();
    }

    private void UpdateEffectState()
    {
        float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        float targetDanger = ResolveDangerValue();
        bool targetVisible = targetDanger > 0f;

        displayDanger = Mathf.MoveTowards(displayDanger, targetDanger, DangerResponseSpeed * deltaTime);
        displayPresence = Mathf.MoveTowards(
            displayPresence,
            targetVisible ? 1f : 0f,
            (targetVisible ? PresenceFadeInSpeed : PresenceFadeOutSpeed) * deltaTime);
        damageBurst = Mathf.MoveTowards(damageBurst, 0f, BurstDecaySpeed * deltaTime);
    }

    private void ApplyDamageBurst(float additionalStrength)
    {
        float danger = ResolveDangerValue();
        if (danger <= 0f)
        {
            return;
        }

        damageBurst = Mathf.Clamp01(Mathf.Max(damageBurst, 0.2f + danger * 0.45f + additionalStrength));
    }

    private float ResolveDangerValue()
    {
        float hpRatio = ResolveHpRatio();
        return Mathf.Clamp01((CriticalThreshold - hpRatio) / CriticalThreshold);
    }

    private float ResolveHpRatio()
    {
        float maxHp = ResolveMaxHp();
        if (maxHp <= Mathf.Epsilon)
        {
            return 1f;
        }

        return Mathf.Clamp01(characterCore.currentHp / maxHp);
    }

    private float ResolveMaxHp()
    {
        if (characterCore == null || characterCore.stats == null)
        {
            return 0f;
        }

        return Mathf.Max(0f, characterCore.stats.maxHp);
    }

    private bool ShouldForceImmediateHide()
    {
        return characterCore == null ||
               characterCore.IsDead ||
               GameplayFailureController.IsFailureActive;
    }

    private void HideImmediately()
    {
        displayPresence = 0f;
        displayDanger = 0f;
        damageBurst = 0f;
        DestroyScreenOverlayImmediate();
    }

    private void EnsureScreenOverlay()
    {
        if (overlayCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(
            "PlayerCriticalStateFeedbackCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = OverlaySortingOrder;
        overlayCanvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        overlayRect = canvasObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        vignetteImage = CreateFullscreenImage("CriticalVignette", overlayRect, GetOrCreateVignetteSprite());
        edgeBands = new[]
        {
            CreateEdgeFlowBand(EdgeSide.Top),
            CreateEdgeFlowBand(EdgeSide.Bottom),
            CreateEdgeFlowBand(EdgeSide.Left),
            CreateEdgeFlowBand(EdgeSide.Right)
        };
        screenParticles = CreateScreenParticles();
    }

    private void DestroyScreenOverlayImmediate()
    {
        if (overlayCanvas == null)
        {
            return;
        }

        Destroy(overlayCanvas.gameObject);
        overlayCanvas = null;
        overlayRect = null;
        vignetteImage = null;
        edgeBands = null;
        screenParticles = null;
    }

    private Image CreateFullscreenImage(string objectName, Transform parent, Sprite sprite)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        return image;
    }

    private EdgeFlowBand CreateEdgeFlowBand(EdgeSide side)
    {
        GameObject rootObject = new GameObject(side + "FlowBand", typeof(RectTransform));
        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.SetParent(overlayRect, false);
        ConfigureBandAnchors(root, side);

        EdgeFlowBand band = new EdgeFlowBand
        {
            Side = side,
            Root = root,
            Primary = CreateBandLayer("Primary", root, GetFlowTexture(side, false)),
            Secondary = CreateBandLayer("Secondary", root, GetFlowTexture(side, true))
        };

        return band;
    }

    private void ConfigureBandAnchors(RectTransform rect, EdgeSide side)
    {
        switch (side)
        {
            case EdgeSide.Top:
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0f, 120f);
                break;
            case EdgeSide.Bottom:
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0f, 120f);
                break;
            case EdgeSide.Left:
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(120f, 0f);
                break;
            case EdgeSide.Right:
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(120f, 0f);
                break;
        }
    }

    private RawImage CreateBandLayer(string objectName, Transform parent, Texture texture)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(RawImage));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        RawImage image = imageObject.GetComponent<RawImage>();
        image.raycastTarget = false;
        image.texture = texture;
        image.uvRect = new Rect(0f, 0f, 1f, 1f);
        return image;
    }

    private ScreenParticle[] CreateScreenParticles()
    {
        ScreenParticle[] particles = new ScreenParticle[EdgeParticleCount];
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i] = CreateParticle(i);
        }

        return particles;
    }

    private ScreenParticle CreateParticle(int index)
    {
        GameObject particleObject = new GameObject("EdgeParticle_" + index, typeof(RectTransform), typeof(Image));
        RectTransform rect = particleObject.GetComponent<RectTransform>();
        rect.SetParent(overlayRect, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image image = particleObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.sprite = GetOrCreateEmberSprite();
        image.type = Image.Type.Simple;

        EdgeSide side = (EdgeSide)(index % 4);
        float direction = GetParticleDirection(side);
        ScreenParticle particle = new ScreenParticle
        {
            Side = side,
            Rect = rect,
            Image = image,
            Along = Random.value,
            AlongSpeed = direction * Random.Range(0.18f, 0.42f),
            DriftSpeed = Random.Range(0.8f, 1.8f),
            BaseSize = Random.Range(12f, 24f),
            Phase = Random.Range(0f, Tau),
            ColorT = Random.Range(0f, 1f),
            AlphaScale = Random.Range(0.65f, 1f),
            JitterScale = Random.Range(0.55f, 1.15f)
        };

        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, particle.BaseSize);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, particle.BaseSize);
        return particle;
    }

    private float GetParticleDirection(EdgeSide side)
    {
        switch (side)
        {
            case EdgeSide.Top:
            case EdgeSide.Right:
                return 1f;
            default:
                return -1f;
        }
    }

    private void UpdateOverlayPresentation(float currentTime, float deltaTime)
    {
        if (overlayRect == null || vignetteImage == null || edgeBands == null)
        {
            return;
        }

        float pulse = 0.5f + 0.5f * Mathf.Sin(currentTime * Mathf.Lerp(1.9f, 2.8f, displayDanger));
        float pulseGain = 0.92f + pulse * (0.1f + displayDanger * 0.1f) + damageBurst * 0.08f;
        float effectStrength = displayPresence * Mathf.Lerp(0.62f, 1f, displayDanger);
        float thickness = Mathf.Lerp(112f, 180f, displayDanger) * pulseGain;

        UpdateVignette(effectStrength, pulse);
        UpdateFlowBands(currentTime, thickness, effectStrength, pulseGain);
        UpdateParticles(currentTime, deltaTime, thickness, effectStrength);
    }

    private void UpdateVignette(float effectStrength, float pulse)
    {
        float alpha = effectStrength * (0.06f + displayDanger * 0.1f + pulse * 0.04f) + damageBurst * 0.035f;
        Color color = VignetteColor;
        color.a = alpha;
        vignetteImage.color = color;
    }

    private void UpdateFlowBands(float currentTime, float thickness, float effectStrength, float pulseGain)
    {
        float primaryAlpha = effectStrength * (0.13f + displayDanger * 0.12f) * pulseGain + damageBurst * 0.04f;
        float secondaryAlpha = effectStrength * (0.08f + displayDanger * 0.11f) * pulseGain + damageBurst * 0.035f;
        float primaryTiles = Mathf.Lerp(1.05f, 1.38f, displayDanger);
        float secondaryTiles = Mathf.Lerp(1.3f, 1.76f, displayDanger);

        for (int i = 0; i < edgeBands.Length; i++)
        {
            EdgeFlowBand band = edgeBands[i];
            if (band == null || band.Root == null)
            {
                continue;
            }

            bool horizontal = band.Side == EdgeSide.Top || band.Side == EdgeSide.Bottom;
            if (horizontal)
            {
                band.Root.sizeDelta = new Vector2(0f, thickness);
            }
            else
            {
                band.Root.sizeDelta = new Vector2(thickness, 0f);
            }

            float primaryDirection = GetPrimaryDirection(band.Side);
            float secondaryDirection = -primaryDirection;
            float primarySpeed = Mathf.Lerp(0.026f, 0.05f, displayDanger);
            float secondarySpeed = Mathf.Lerp(0.04f, 0.072f, displayDanger);

            band.Primary.uvRect = horizontal
                ? new Rect(currentTime * primarySpeed * primaryDirection, 0f, primaryTiles, 1f)
                : new Rect(0f, currentTime * primarySpeed * primaryDirection, 1f, primaryTiles);
            band.Secondary.uvRect = horizontal
                ? new Rect(currentTime * secondarySpeed * secondaryDirection, 0f, secondaryTiles, 1f)
                : new Rect(0f, currentTime * secondarySpeed * secondaryDirection, 1f, secondaryTiles);

            Color primaryColor = PrimaryFlowColor;
            primaryColor.a = primaryAlpha;
            band.Primary.color = primaryColor;

            Color secondaryColor = Color.Lerp(SecondaryFlowColor, PrimaryFlowColor, damageBurst * 0.35f);
            secondaryColor.a = secondaryAlpha;
            band.Secondary.color = secondaryColor;
        }
    }

    private float GetPrimaryDirection(EdgeSide side)
    {
        switch (side)
        {
            case EdgeSide.Top:
            case EdgeSide.Left:
                return 1f;
            default:
                return -1f;
        }
    }

    private void UpdateParticles(float currentTime, float deltaTime, float thickness, float effectStrength)
    {
        if (screenParticles == null || overlayRect == null)
        {
            return;
        }

        Rect screenRect = overlayRect.rect;
        float width = screenRect.width > 1f ? screenRect.width : Screen.width;
        float height = screenRect.height > 1f ? screenRect.height : Screen.height;
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        float travelGain = 0.34f + displayDanger * 0.55f + damageBurst * 0.25f;

        for (int i = 0; i < screenParticles.Length; i++)
        {
            ScreenParticle particle = screenParticles[i];
            if (particle == null || particle.Rect == null || particle.Image == null)
            {
                continue;
            }

            particle.Along = Mathf.Repeat(particle.Along + particle.AlongSpeed * travelGain * deltaTime, 1f);

            float shimmer = 0.55f + 0.45f * Mathf.Sin(currentTime * (1.1f + particle.DriftSpeed) + particle.Phase);
            float depthWave = 0.5f + 0.5f * Mathf.Sin(currentTime * particle.DriftSpeed + particle.Phase * 0.7f);
            float depth = Mathf.Lerp(0.06f, 0.9f, depthWave);
            float edgeOffset = thickness * (0.08f + depth * 0.82f);
            float tangentJitter = Mathf.Sin(currentTime * (0.85f + particle.DriftSpeed * 0.4f) + particle.Phase) * thickness * 0.05f * particle.JitterScale;
            Vector2 anchoredPosition = ResolveParticlePosition(particle.Side, particle.Along, edgeOffset, tangentJitter, halfWidth, halfHeight);

            float alpha = effectStrength * (0.08f + displayDanger * 0.09f) * particle.AlphaScale * (0.72f + shimmer * 0.38f)
                          + damageBurst * 0.03f;
            Color color = Color.Lerp(EmberLowColor, EmberHighColor, particle.ColorT);
            color.a = alpha;
            particle.Image.color = color;
            particle.Image.enabled = alpha > 0.001f;

            float size = particle.BaseSize * (0.82f + shimmer * 0.35f) * (0.86f + displayDanger * 0.45f + damageBurst * 0.25f);
            particle.Rect.anchoredPosition = anchoredPosition;
            particle.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            particle.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
        }
    }

    private Vector2 ResolveParticlePosition(
        EdgeSide side,
        float along,
        float edgeOffset,
        float tangentJitter,
        float halfWidth,
        float halfHeight)
    {
        switch (side)
        {
            case EdgeSide.Top:
                return new Vector2(Mathf.Lerp(-halfWidth, halfWidth, along) + tangentJitter, halfHeight - edgeOffset);
            case EdgeSide.Bottom:
                return new Vector2(Mathf.Lerp(-halfWidth, halfWidth, along) + tangentJitter, -halfHeight + edgeOffset);
            case EdgeSide.Left:
                return new Vector2(-halfWidth + edgeOffset, Mathf.Lerp(-halfHeight, halfHeight, along) + tangentJitter);
            default:
                return new Vector2(halfWidth - edgeOffset, Mathf.Lerp(-halfHeight, halfHeight, along) + tangentJitter);
        }
    }

    private static Texture2D GetFlowTexture(EdgeSide side, bool secondary)
    {
        Texture2D[] cache = secondary ? SecondaryFlowTextures : PrimaryFlowTextures;
        int index = (int)side;
        if (cache[index] == null)
        {
            cache[index] = CreateEdgeFlowTexture(side, secondary);
        }

        return cache[index];
    }

    private static Texture2D CreateEdgeFlowTexture(EdgeSide side, bool secondary)
    {
        bool horizontal = side == EdgeSide.Top || side == EdgeSide.Bottom;
        int width = horizontal ? 384 : 128;
        int height = horizontal ? 128 : 384;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        float phase = secondary ? 0.37f : 0.11f;
        float sharpness = secondary ? 2.05f : 1.38f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (x + 0.5f) / width;
                float v = (y + 0.5f) / height;
                float along;
                float depth;

                switch (side)
                {
                    case EdgeSide.Top:
                        along = u;
                        depth = 1f - v;
                        break;
                    case EdgeSide.Bottom:
                        along = u;
                        depth = v;
                        break;
                    case EdgeSide.Left:
                        along = v;
                        depth = u;
                        break;
                    default:
                        along = v;
                        depth = 1f - u;
                        break;
                }

                float waveA = 0.5f + 0.5f * Mathf.Sin(Tau * ((secondary ? 3f : 2f) * along + (secondary ? 2f : 1f) * depth + phase));
                float waveB = 0.5f + 0.5f * Mathf.Sin(Tau * ((secondary ? 7f : 5f) * along - (secondary ? 3f : 2f) * depth + phase * 1.8f));
                float waveC = 0.5f + 0.5f * Mathf.Cos(Tau * ((secondary ? 11f : 8f) * along + (secondary ? 2f : 3f) * depth + phase * 2.3f));
                float softBreakup = 0.5f + 0.5f * Mathf.Sin(Tau * ((secondary ? 13f : 9f) * along + depth + phase * 0.9f));

                float flow = waveA * 0.42f + waveB * 0.33f + waveC * 0.25f;
                flow = Mathf.Lerp(flow, Mathf.Pow(flow, sharpness), secondary ? 0.76f : 0.52f);

                float fade = 1f - Mathf.SmoothStep(secondary ? 0.44f : 0.58f, 1f, depth);
                fade *= Mathf.Lerp(1f, 0.28f, depth * depth);

                float alpha = fade * (secondary ? (0.18f + flow * 0.82f) : (0.28f + flow * 0.64f));
                alpha *= Mathf.Lerp(0.74f, 1f, softBreakup);
                alpha = Mathf.Clamp01(alpha);

                float brightness = secondary
                    ? Mathf.Lerp(0.52f, 1f, flow)
                    : Mathf.Lerp(0.4f, 0.9f, flow);
                texture.SetPixel(x, y, new Color(brightness, brightness, brightness, alpha));
            }
        }

        texture.Apply(false, true);
        return texture;
    }

    private static Sprite GetOrCreateVignetteSprite()
    {
        if (vignetteSprite != null)
        {
            return vignetteSprite;
        }

        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size;
                float v = (y + 0.5f) / size;
                float edgeDistance = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * 2f;
                float alpha = 1f - Mathf.SmoothStep(0.18f, 0.74f, edgeDistance);
                alpha = Mathf.Pow(Mathf.Clamp01(alpha), 1.25f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, true);
        vignetteSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return vignetteSprite;
    }

    private static Sprite GetOrCreateEmberSprite()
    {
        if (emberSprite != null)
        {
            return emberSprite;
        }

        const int size = 20;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = ((x + 0.5f) / size) * 2f - 1f;
                float ny = ((y + 0.5f) / size) * 2f - 1f;
                float radial = Mathf.Sqrt(nx * nx + ny * ny);
                float square = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
                float alpha = Mathf.Lerp(
                    1f - Mathf.SmoothStep(0.2f, 1f, radial),
                    1f - Mathf.SmoothStep(0.1f, 1f, square),
                    0.35f);
                alpha = Mathf.Clamp01(alpha);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, true);
        emberSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return emberSprite;
    }
}
