using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class RuntimeBackpackPickupAnimator : MonoBehaviour
{
    private const string CanvasName = "RuntimeBackpackPickupAnimatorCanvas";
    private const int SortingOrder = 292;
    private const float FlightDuration = 0.56f;
    private const float FlashDuration = 0.24f;
    private const float ArcHeight = 112f;
    private const float MinIconSize = 56f;
    private const float MaxIconSize = 88f;

    public static RuntimeBackpackPickupAnimator Instance { get; private set; }

    private Canvas canvas;
    private RectTransform canvasRect;
    private Sprite whiteSprite;
    private readonly Dictionary<int, List<Image>> overlayImagesByAnimation = new Dictionary<int, List<Image>>();
    private int nextAnimationId = 1;
    private int activeAnimationCount;

    public static bool TryAnimateLootBagPickup(
        ArchitecturalCrystal crystal,
        Vector3 worldPosition,
        Sprite travelSprite)
    {
        if (!crystal.IsCommonStructure)
        {
            return false;
        }

        BackpackMananger backpack = BackpackMananger.Instance;
        if (backpack == null)
        {
            return false;
        }

        if (!backpack.TryReserveSlotForPickup(crystal, out int slotIndex))
        {
            return false;
        }

        BackpackUI backpackUi = FindObjectOfType<BackpackUI>(true);
        if (backpackUi == null)
        {
            backpack.CancelReservedSlot(slotIndex);
            return false;
        }

        backpackUi.EnsureVisibleForIncomingPickup();
        if (!backpackUi.TryGetSlotScreenPosition(slotIndex, out _, out _))
        {
            backpackUi.EndIncomingPickupPresentation();
            backpack.CancelReservedSlot(slotIndex);
            return false;
        }

        EnsureInstance().PlayAnimation(backpackUi, backpack, crystal, worldPosition, travelSprite, slotIndex);
        return true;
    }

    public static RuntimeBackpackPickupAnimator EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        RuntimeBackpackPickupAnimator existing = FindObjectOfType<RuntimeBackpackPickupAnimator>();
        if (existing != null)
        {
            Instance = existing;
            existing.EnsureCanvas();
            return existing;
        }

        GameObject runtimeObject = new GameObject(CanvasName);
        DontDestroyOnLoad(runtimeObject);
        Instance = runtimeObject.AddComponent<RuntimeBackpackPickupAnimator>();
        Instance.EnsureCanvas();
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
        EnsureCanvas();
    }

    private void OnDisable()
    {
        CleanupAllAnimationOverlays();
    }

    private void OnDestroy()
    {
        CleanupAllAnimationOverlays();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void PlayAnimation(
        BackpackUI backpackUi,
        BackpackMananger backpack,
        ArchitecturalCrystal crystal,
        Vector3 worldPosition,
        Sprite travelSprite,
        int slotIndex)
    {
        EnsureCanvas();
        if (activeAnimationCount == 0)
        {
            CleanupStaleOverlayChildren();
        }

        int animationId = nextAnimationId++;
        activeAnimationCount++;
        StartCoroutine(AnimatePickupRoutine(animationId, backpackUi, backpack, crystal, worldPosition, travelSprite, slotIndex));
    }

    private IEnumerator AnimatePickupRoutine(
        int animationId,
        BackpackUI backpackUi,
        BackpackMananger backpack,
        ArchitecturalCrystal crystal,
        Vector3 worldPosition,
        Sprite travelSprite,
        int slotIndex)
    {
        bool pickupResolved = false;

        try
        {
            if (!TryResolveCanvasPointFromWorld(worldPosition, out Vector2 startPoint) ||
                !TryResolveSlotCanvasPoint(backpackUi, slotIndex, out Vector2 endPoint, out Vector2 slotSize))
            {
                ResolvePickup(backpack, crystal, slotIndex);
                pickupResolved = true;
                yield break;
            }

            Sprite flightSprite = travelSprite != null
                ? travelSprite
                : (crystal.backIcon != null ? crystal.backIcon : RuntimeCrystalDropFactory.ResolveSprite(crystal));
            flightSprite = RuntimeSpriteDisplaySanitizer.GetDisplaySprite(flightSprite);

            Image flyingIcon = CreateOverlayImage(animationId, "FlyingLootBag", flightSprite);
            RectTransform flyingRect = flyingIcon.rectTransform;
            float baseIconSize = Mathf.Clamp(Mathf.Max(slotSize.x, slotSize.y) * 1.08f, MinIconSize, MaxIconSize);
            flyingRect.sizeDelta = new Vector2(baseIconSize, baseIconSize);
            flyingRect.anchoredPosition = startPoint;

            Vector2 controlPoint = (startPoint + endPoint) * 0.5f + Vector2.up * Mathf.Max(ArcHeight, Vector2.Distance(startPoint, endPoint) * 0.12f);
            float elapsed = 0f;
            while (elapsed < FlightDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / FlightDuration);
                float eased = EaseOutCubic(t);
                flyingRect.anchoredPosition = EvaluateQuadraticBezier(startPoint, controlPoint, endPoint, eased);
                flyingRect.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.82f, eased);
                flyingIcon.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.97f, eased));
                yield return null;
            }

            flyingRect.anchoredPosition = endPoint;
            DestroyOverlayImage(animationId, flyingIcon);

            yield return PlaySlotFlash(animationId, endPoint, slotSize, () =>
            {
                ResolvePickup(backpack, crystal, slotIndex);
                pickupResolved = true;
            });
        }
        finally
        {
            CleanupAnimationOverlays(animationId);
            if (activeAnimationCount > 0)
            {
                activeAnimationCount--;
            }

            if (activeAnimationCount == 0)
            {
                CleanupStaleOverlayChildren();
            }

            if (!pickupResolved && backpack != null)
            {
                backpack.CancelReservedSlot(slotIndex);
            }

            if (backpackUi != null)
            {
                backpackUi.EndIncomingPickupPresentation();
            }
        }
    }

    private IEnumerator PlaySlotFlash(int animationId, Vector2 slotPoint, Vector2 slotSize, System.Action onReveal)
    {
        Image flashImage = CreateOverlayImage(animationId, "SlotRevealFlash", GetWhiteSprite());
        RectTransform flashRect = flashImage.rectTransform;
        float flashSize = Mathf.Max(Mathf.Max(slotSize.x, slotSize.y) * 1.22f, 64f);
        flashRect.sizeDelta = new Vector2(flashSize, flashSize);
        flashRect.anchoredPosition = slotPoint;
        flashRect.localScale = Vector3.one * 0.72f;

        bool revealed = false;
        float elapsed = 0f;
        while (elapsed < FlashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / FlashDuration);

            if (!revealed && t >= 0.5f)
            {
                revealed = true;
                onReveal?.Invoke();
            }

            float alpha = t < 0.5f
                ? Mathf.Lerp(0f, 0.92f, t / 0.5f)
                : Mathf.Lerp(0.92f, 0f, (t - 0.5f) / 0.5f);
            float scale = t < 0.5f
                ? Mathf.Lerp(0.72f, 1.16f, t / 0.5f)
                : Mathf.Lerp(1.16f, 1.34f, (t - 0.5f) / 0.5f);

            flashRect.localScale = Vector3.one * scale;
            flashImage.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        if (!revealed)
        {
            onReveal?.Invoke();
        }

        DestroyOverlayImage(animationId, flashImage);
    }

    private void ResolvePickup(BackpackMananger backpack, ArchitecturalCrystal crystal, int slotIndex)
    {
        if (backpack == null)
        {
            return;
        }

        if (backpack.CommitReservedPickup(crystal, slotIndex))
        {
            return;
        }

        backpack.CancelReservedSlot(slotIndex);
        backpack.PickItem(crystal);
    }

    private void EnsureCanvas()
    {
        if (canvas != null && canvasRect != null)
        {
            return;
        }

        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        canvasRect = canvas.transform as RectTransform;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = gameObject.AddComponent<GraphicRaycaster>();
        }

        raycaster.enabled = false;
    }

    private bool TryResolveCanvasPointFromWorld(Vector3 worldPosition, out Vector2 canvasPoint)
    {
        canvasPoint = default;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return false;
        }

        Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);
        if (screenPoint.z < 0f)
        {
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            null,
            out canvasPoint);
    }

    private bool TryResolveSlotCanvasPoint(
        BackpackUI backpackUi,
        int slotIndex,
        out Vector2 canvasPoint,
        out Vector2 slotSize)
    {
        canvasPoint = default;
        slotSize = default;

        if (backpackUi == null ||
            !backpackUi.TryGetSlotScreenPosition(slotIndex, out Vector2 screenPoint, out slotSize))
        {
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            null,
            out canvasPoint);
    }

    private Image CreateOverlayImage(int animationId, string objectName, Sprite sprite)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.SetParent(canvasRect, false);
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.maskable = false;
        image.raycastTarget = false;
        image.color = Color.white;
        RegisterOverlayImage(animationId, image);
        return image;
    }

    private void RegisterOverlayImage(int animationId, Image image)
    {
        if (image == null)
        {
            return;
        }

        if (!overlayImagesByAnimation.TryGetValue(animationId, out List<Image> overlayImages))
        {
            overlayImages = new List<Image>();
            overlayImagesByAnimation[animationId] = overlayImages;
        }

        overlayImages.Add(image);
    }

    private void DestroyOverlayImage(int animationId, Image image)
    {
        if (image == null)
        {
            return;
        }

        if (overlayImagesByAnimation.TryGetValue(animationId, out List<Image> overlayImages))
        {
            overlayImages.Remove(image);
        }

        if (image.gameObject.activeSelf)
        {
            image.gameObject.SetActive(false);
        }

        Destroy(image.gameObject);
    }

    private void CleanupAnimationOverlays(int animationId)
    {
        if (!overlayImagesByAnimation.TryGetValue(animationId, out List<Image> overlayImages))
        {
            return;
        }

        for (int i = overlayImages.Count - 1; i >= 0; i--)
        {
            Image image = overlayImages[i];
            if (image == null)
            {
                continue;
            }

            if (image.gameObject.activeSelf)
            {
                image.gameObject.SetActive(false);
            }

            Destroy(image.gameObject);
        }

        overlayImagesByAnimation.Remove(animationId);
    }

    private void CleanupAllAnimationOverlays()
    {
        foreach (int animationId in new List<int>(overlayImagesByAnimation.Keys))
        {
            CleanupAnimationOverlays(animationId);
        }

        activeAnimationCount = 0;
        CleanupStaleOverlayChildren();
    }

    private void CleanupStaleOverlayChildren()
    {
        if (canvasRect == null)
        {
            return;
        }

        for (int i = canvasRect.childCount - 1; i >= 0; i--)
        {
            Transform child = canvasRect.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (child.name != "FlyingLootBag" && child.name != "SlotRevealFlash")
            {
                continue;
            }

            if (child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(false);
            }

            Destroy(child.gameObject);
        }
    }

    private Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
        {
            return whiteSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        whiteSprite.name = "RuntimeBackpackPickupWhiteSprite";
        return whiteSprite;
    }

    private static float EaseOutCubic(float value)
    {
        float remaining = 1f - value;
        return 1f - remaining * remaining * remaining;
    }

    private static Vector2 EvaluateQuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
    }
}
