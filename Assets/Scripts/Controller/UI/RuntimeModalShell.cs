using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class RuntimeModalStyle
{
    public const float TransitionDuration = 0.24f;
    public const float PanelStartScale = 0.965f;
    public const float PanelStartYOffset = -18f;
    public const int BackdropSortingOrder = 900;
    public const int ModalSortingOrder = 920;

    public static readonly Color BlurTintColor = new Color(0.04f, 0.05f, 0.08f, 0.62f);
    public static readonly Color OverlayColor = new Color(0.02f, 0.03f, 0.05f, 0.36f);

    public static void ApplyBackdropState(RawImage blurBackdropImage, Image blurTintImage, Image overlayImage, float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        float blurProgress = Mathf.SmoothStep(0f, 1f, clampedProgress);

        if (blurBackdropImage != null)
        {
            blurBackdropImage.color = WithAlpha(Color.white, blurProgress);
            blurBackdropImage.rectTransform.localScale = Vector3.one;
        }

        if (blurTintImage != null)
        {
            blurTintImage.color = WithAlpha(BlurTintColor, BlurTintColor.a * blurProgress);
        }

        if (overlayImage != null)
        {
            overlayImage.color = WithAlpha(OverlayColor, OverlayColor.a * blurProgress);
        }
    }

    public static void ApplyPanelState(
        CanvasGroup panelCanvasGroup,
        RectTransform panelRectTransform,
        Vector2 visibleAnchoredPosition,
        Vector3 visibleScale,
        float progress)
    {
        float easedProgress = EaseOutCubic(progress);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = easedProgress;
        }

        if (panelRectTransform != null)
        {
            panelRectTransform.localScale = Vector3.Lerp(
                visibleScale * PanelStartScale,
                visibleScale,
                easedProgress);
            panelRectTransform.anchoredPosition = visibleAnchoredPosition + new Vector2(
                0f,
                Mathf.Lerp(PanelStartYOffset, 0f, easedProgress));
        }
    }

    public static Texture2D CaptureBackdropTexture()
    {
        Camera captureCamera = ResolveBackdropCamera();
        if (captureCamera == null)
        {
            return ScreenCapture.CaptureScreenshotAsTexture();
        }

        int captureWidth = Mathf.Max(Screen.width, 1);
        int captureHeight = Mathf.Max(Screen.height, 1);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = captureCamera.targetTexture;
        RenderTexture captureTexture = RenderTexture.GetTemporary(captureWidth, captureHeight, 24, RenderTextureFormat.ARGB32);
        captureTexture.filterMode = FilterMode.Bilinear;
        captureTexture.wrapMode = TextureWrapMode.Clamp;

        try
        {
            captureCamera.targetTexture = captureTexture;
            captureCamera.Render();
            RenderTexture.active = captureTexture;

            Texture2D result = new Texture2D(captureWidth, captureHeight, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0f, 0f, captureWidth, captureHeight), 0, 0, false);
            result.Apply(false, false);
            result.filterMode = FilterMode.Bilinear;
            result.wrapMode = TextureWrapMode.Clamp;
            return result;
        }
        finally
        {
            captureCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(captureTexture);
        }
    }

    public static RenderTexture BuildBlurBackdrop(Texture2D capturedBackdropTexture)
    {
        if (capturedBackdropTexture == null)
        {
            return null;
        }

        int halfWidth = Mathf.Max(640, Screen.width / 2);
        int halfHeight = Mathf.Max(360, Screen.height / 2);
        int quarterWidth = Mathf.Max(480, Screen.width / 4);
        int quarterHeight = Mathf.Max(270, Screen.height / 4);
        int finalWidth = Mathf.Max(320, Screen.width / 8);
        int finalHeight = Mathf.Max(180, Screen.height / 8);

        RenderTexture halfTexture = RenderTexture.GetTemporary(halfWidth, halfHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture quarterTexture = RenderTexture.GetTemporary(quarterWidth, quarterHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture tempTexture = RenderTexture.GetTemporary(finalWidth, finalHeight, 0, RenderTextureFormat.ARGB32);
        halfTexture.filterMode = FilterMode.Bilinear;
        quarterTexture.filterMode = FilterMode.Bilinear;
        tempTexture.filterMode = FilterMode.Bilinear;

        RenderTexture blurredBackdropTexture = new RenderTexture(finalWidth, finalHeight, 0, RenderTextureFormat.ARGB32)
        {
            name = "RuntimeModalBlurBackdrop",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        blurredBackdropTexture.Create();

        Graphics.Blit(capturedBackdropTexture, halfTexture);
        Graphics.Blit(halfTexture, quarterTexture);
        Graphics.Blit(quarterTexture, tempTexture);
        Graphics.Blit(tempTexture, quarterTexture);
        Graphics.Blit(quarterTexture, blurredBackdropTexture);

        RenderTexture.ReleaseTemporary(halfTexture);
        RenderTexture.ReleaseTemporary(quarterTexture);
        RenderTexture.ReleaseTemporary(tempTexture);
        return blurredBackdropTexture;
    }

    public static RenderTexture RefreshRealtimeBlurBackdrop(RenderTexture blurredBackdropTexture)
    {
        ResolveBlurDimensions(out int halfWidth, out int halfHeight, out int quarterWidth, out int quarterHeight, out int finalWidth, out int finalHeight);
        if (!IsBlurBackdropValid(blurredBackdropTexture, finalWidth, finalHeight))
        {
            ReleaseRenderTexture(blurredBackdropTexture);
            blurredBackdropTexture = CreateBlurBackdropTexture(finalWidth, finalHeight);
        }

        Camera captureCamera = ResolveBackdropCamera();
        if (captureCamera == null)
        {
            return RefreshFallbackScreenshotBlur(blurredBackdropTexture);
        }

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = captureCamera.targetTexture;
        RenderTexture halfTexture = RenderTexture.GetTemporary(halfWidth, halfHeight, 24, RenderTextureFormat.ARGB32);
        RenderTexture quarterTexture = RenderTexture.GetTemporary(quarterWidth, quarterHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture tempTexture = RenderTexture.GetTemporary(finalWidth, finalHeight, 0, RenderTextureFormat.ARGB32);
        halfTexture.filterMode = FilterMode.Bilinear;
        quarterTexture.filterMode = FilterMode.Bilinear;
        tempTexture.filterMode = FilterMode.Bilinear;

        try
        {
            captureCamera.targetTexture = halfTexture;
            captureCamera.Render();
            Graphics.Blit(halfTexture, quarterTexture);
            Graphics.Blit(quarterTexture, tempTexture);
            Graphics.Blit(tempTexture, quarterTexture);
            Graphics.Blit(quarterTexture, blurredBackdropTexture);
            return blurredBackdropTexture;
        }
        finally
        {
            captureCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(halfTexture);
            RenderTexture.ReleaseTemporary(quarterTexture);
            RenderTexture.ReleaseTemporary(tempTexture);
        }
    }

    public static void ReleaseBlurBackdrop(RawImage blurBackdropImage, ref Texture2D capturedBackdropTexture, ref RenderTexture blurredBackdropTexture)
    {
        if (blurBackdropImage != null)
        {
            blurBackdropImage.texture = null;
        }

        ReleaseRenderTexture(blurredBackdropTexture);
        blurredBackdropTexture = null;

        if (capturedBackdropTexture != null)
        {
            UnityEngine.Object.Destroy(capturedBackdropTexture);
            capturedBackdropTexture = null;
        }
    }

    public static float EaseOutCubic(float progress)
    {
        float inverse = 1f - Mathf.Clamp01(progress);
        return 1f - inverse * inverse * inverse;
    }

    public static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static Camera ResolveBackdropCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.isActiveAndEnabled)
        {
            return mainCamera;
        }

        Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>(true);
        Camera fallback = null;
        float highestDepth = float.MinValue;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];
            if (candidate == null || !candidate.isActiveAndEnabled || !candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (candidate.depth < highestDepth)
            {
                continue;
            }

            highestDepth = candidate.depth;
            fallback = candidate;
        }

        return fallback;
    }

    private static void ResolveBlurDimensions(
        out int halfWidth,
        out int halfHeight,
        out int quarterWidth,
        out int quarterHeight,
        out int finalWidth,
        out int finalHeight)
    {
        halfWidth = Mathf.Max(640, Screen.width / 2);
        halfHeight = Mathf.Max(360, Screen.height / 2);
        quarterWidth = Mathf.Max(480, Screen.width / 4);
        quarterHeight = Mathf.Max(270, Screen.height / 4);
        finalWidth = Mathf.Max(320, Screen.width / 8);
        finalHeight = Mathf.Max(180, Screen.height / 8);
    }

    private static RenderTexture CreateBlurBackdropTexture(int width, int height)
    {
        RenderTexture texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = "RuntimeModalBlurBackdrop",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.Create();
        return texture;
    }

    private static bool IsBlurBackdropValid(RenderTexture texture, int width, int height)
    {
        return texture != null &&
               texture.width == width &&
               texture.height == height &&
               texture.IsCreated();
    }

    private static RenderTexture RefreshFallbackScreenshotBlur(RenderTexture blurredBackdropTexture)
    {
        Texture2D screenshot = CaptureBackdropTexture();
        if (screenshot == null)
        {
            return blurredBackdropTexture;
        }

        RenderTexture rebuiltTexture = BuildBlurBackdrop(screenshot);
        UnityEngine.Object.Destroy(screenshot);
        if (rebuiltTexture == null)
        {
            return blurredBackdropTexture;
        }

        ReleaseRenderTexture(blurredBackdropTexture);
        return rebuiltTexture;
    }

    private static void ReleaseRenderTexture(RenderTexture texture)
    {
        if (texture == null)
        {
            return;
        }

        if (texture.IsCreated())
        {
            texture.Release();
        }

        UnityEngine.Object.Destroy(texture);
    }
}

public sealed class RuntimeModalShell : MonoBehaviour
{
    private const string CanvasName = "RuntimeModalShellCanvas";

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RawImage blurBackdropImage;
    private Image blurTintImage;
    private Image overlayImage;
    private Coroutine transitionCoroutine;
    private Coroutine backdropRefreshCoroutine;
    private Texture2D capturedBackdropTexture;
    private RenderTexture blurredBackdropTexture;
    private CanvasGroup activeCanvasGroup;
    private RectTransform activeRectTransform;
    private Vector2 activeVisibleAnchoredPosition;
    private Vector3 activeVisibleScale = Vector3.one;
    private Action backdropClickHandler;
    private BackdropClickTarget backdropClickTarget;

    public bool IsVisible { get; private set; }

    public void SetBackdropClickHandler(Action clickHandler)
    {
        backdropClickHandler = clickHandler;
        if (backdropClickTarget != null)
        {
            backdropClickTarget.Clicked = backdropClickHandler;
        }
    }

    public void Show(CanvasGroup targetCanvasGroup, Action onShown = null)
    {
        EnsureUi();
        SetTarget(targetCanvasGroup);
        ApplyState(0f);

        canvas.gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;
        IsVisible = true;

        StopTransition();
        StartRealtimeBackdropRefresh();
        transitionCoroutine = StartCoroutine(ShowRoutine(onShown));
    }

    public void Retarget(CanvasGroup targetCanvasGroup)
    {
        EnsureUi();
        SetTarget(targetCanvasGroup);
        ApplyState(1f);

        if (canvas != null && !canvas.gameObject.activeSelf)
        {
            canvas.gameObject.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        IsVisible = true;
        StartRealtimeBackdropRefresh();
    }

    public void Hide(bool immediate, Action onHidden = null)
    {
        if (canvas == null)
        {
            onHidden?.Invoke();
            return;
        }

        StopTransition();
        if (immediate || !IsVisible)
        {
            ApplyState(0f);
            Cleanup();
            onHidden?.Invoke();
            return;
        }

        transitionCoroutine = StartCoroutine(HideRoutine(onHidden));
    }

    private IEnumerator ShowRoutine(Action onShown)
    {
        yield return new WaitForEndOfFrame();

        RefreshRealtimeBackdrop();

        float elapsed = 0f;
        while (elapsed < RuntimeModalStyle.TransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            ApplyState(elapsed / RuntimeModalStyle.TransitionDuration);
            yield return null;
        }

        ApplyState(1f);
        transitionCoroutine = null;
        onShown?.Invoke();
    }

    private IEnumerator HideRoutine(Action onHidden)
    {
        float elapsed = 0f;
        while (elapsed < RuntimeModalStyle.TransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = 1f - Mathf.Clamp01(elapsed / RuntimeModalStyle.TransitionDuration);
            ApplyState(progress);
            yield return null;
        }

        ApplyState(0f);
        Cleanup();
        transitionCoroutine = null;
        onHidden?.Invoke();
    }

    private void Cleanup()
    {
        StopRealtimeBackdropRefresh();
        RuntimeModalStyle.ReleaseBlurBackdrop(blurBackdropImage, ref capturedBackdropTexture, ref blurredBackdropTexture);
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
        }

        IsVisible = false;
    }

    private void StopTransition()
    {
        if (transitionCoroutine == null)
        {
            return;
        }

        StopCoroutine(transitionCoroutine);
        transitionCoroutine = null;
    }

    private void StartRealtimeBackdropRefresh()
    {
        StopRealtimeBackdropRefresh();
        backdropRefreshCoroutine = StartCoroutine(RefreshBackdropRoutine());
    }

    private void StopRealtimeBackdropRefresh()
    {
        if (backdropRefreshCoroutine == null)
        {
            return;
        }

        StopCoroutine(backdropRefreshCoroutine);
        backdropRefreshCoroutine = null;
    }

    private IEnumerator RefreshBackdropRoutine()
    {
        while (IsVisible)
        {
            yield return new WaitForEndOfFrame();
            RefreshRealtimeBackdrop();
        }

        backdropRefreshCoroutine = null;
    }

    private void RefreshRealtimeBackdrop()
    {
        if (blurBackdropImage == null)
        {
            return;
        }

        blurredBackdropTexture = RuntimeModalStyle.RefreshRealtimeBlurBackdrop(blurredBackdropTexture);
        if (blurBackdropImage.texture != blurredBackdropTexture)
        {
            blurBackdropImage.texture = blurredBackdropTexture;
        }
    }

    private void ApplyState(float progress)
    {
        RuntimeModalStyle.ApplyBackdropState(blurBackdropImage, blurTintImage, overlayImage, progress);
        RuntimeModalStyle.ApplyPanelState(
            activeCanvasGroup,
            activeRectTransform,
            activeVisibleAnchoredPosition,
            activeVisibleScale,
            progress);
    }

    private void SetTarget(CanvasGroup targetCanvasGroup)
    {
        activeCanvasGroup = targetCanvasGroup;
        activeRectTransform = targetCanvasGroup != null ? targetCanvasGroup.transform as RectTransform : null;
        activeVisibleAnchoredPosition = activeRectTransform != null ? activeRectTransform.anchoredPosition : Vector2.zero;
        activeVisibleScale = activeRectTransform != null ? activeRectTransform.localScale : Vector3.one;
    }

    private void EnsureUi()
    {
        if (canvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(
            CanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = RuntimeModalStyle.BackdropSortingOrder;
        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = true;
        }

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        StretchRect(canvasRect);

        GameObject blurObject = new GameObject("BlurBackdrop", typeof(RectTransform), typeof(RawImage));
        blurObject.transform.SetParent(canvasObject.transform, false);
        blurBackdropImage = blurObject.GetComponent<RawImage>();
        blurBackdropImage.color = Color.clear;
        blurBackdropImage.raycastTarget = false;
        StretchRect(blurBackdropImage.rectTransform);

        blurTintImage = CreateImage("BlurTint", canvasObject.transform, RuntimeModalStyle.BlurTintColor);
        blurTintImage.color = Color.clear;
        StretchRect(blurTintImage.rectTransform);
        blurTintImage.raycastTarget = false;

        overlayImage = CreateImage("Overlay", canvasObject.transform, RuntimeModalStyle.OverlayColor);
        overlayImage.color = Color.clear;
        StretchRect(overlayImage.rectTransform);
        overlayImage.raycastTarget = true;
        backdropClickTarget = overlayImage.gameObject.AddComponent<BackdropClickTarget>();
        backdropClickTarget.Clicked = backdropClickHandler;

        canvas.gameObject.SetActive(false);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private sealed class BackdropClickTarget : MonoBehaviour, IPointerClickHandler
    {
        public Action Clicked { private get; set; }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke();
        }
    }
}
